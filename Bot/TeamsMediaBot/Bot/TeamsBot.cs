using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using TeamsMediaBot.Services;

namespace TeamsMediaBot.Bot
{
    /// <summary>
    /// Main bot activity handler for Teams messages and commands.
    /// Inherits from ActivityHandler to process Teams activities.
    /// </summary>
    public class TeamsBot : ActivityHandler
    {
        private readonly CommandParser _commandParser;
        private readonly CommandHandlers _commandHandlers;
        private readonly ILogger<TeamsBot> _logger;
        private readonly string _tenantId;

        /// <summary>
        /// Initializes a new instance of the TeamsBot class.
        /// </summary>
        public TeamsBot(
            CommandParser commandParser,
            CommandHandlers commandHandlers,
            IConfiguration configuration,
            ILogger<TeamsBot> logger)
        {
            _commandParser = commandParser ?? throw new ArgumentNullException(nameof(commandParser));
            _commandHandlers = commandHandlers ?? throw new ArgumentNullException(nameof(commandHandlers));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Get tenant ID from configuration
            _tenantId = configuration["AzureAd:TenantId"] ?? throw new InvalidOperationException("TenantId not configured");
        }

        /// <summary>
        /// Called when the bot receives a message activity.
        /// Handles both mention-based and slash command formats.
        /// </summary>
        protected override async Task OnMessageActivityAsync(
            ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Received command: {turnContext}", turnContext);
                // Extract the command text (removes mention tags if present)
                var commandText = GetCommandText(turnContext.Activity);

                _logger.LogInformation("Received command: {Command}", commandText);

                // Parse the command
                var command = _commandParser.Parse(commandText);

                // Route to appropriate handler
                var response = command switch
                {
                    JoinCommand joinCmd => await _commandHandlers.HandleJoinCommandAsync(joinCmd, _tenantId),
                    HelpCommand => _commandHandlers.HandleHelpCommand(),
                    StatusCommand => _commandHandlers.HandleStatusCommand(),
                    GreetingCommand => _commandHandlers.HandleGreetingCommand(),
                    UnknownCommand => _commandHandlers.HandleUnknownCommand(),
                    _ => _commandHandlers.HandleUnknownCommand()
                };

                // Send response back to Teams
                await turnContext.SendActivityAsync(
                    MessageFactory.Text(response),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message activity");
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("❌ An error occurred while processing your command."),
                    cancellationToken);
            }
        }

        /// <summary>
        /// Called when members (including the bot) are added to a conversation.
        /// Sends a welcome message when the bot is added.
        /// </summary>
        protected override async Task OnMembersAddedAsync(
            IList<ChannelAccount> membersAdded,
            ITurnContext<IConversationUpdateActivity> turnContext,
            CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                // Don't greet ourselves
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(
                        MessageFactory.Text(BotMessages.WelcomeMessage),
                        cancellationToken);
                }
            }
        }

        /// <summary>
        /// Extracts clean command text from the activity.
        /// Removes mention tags and leading/trailing whitespace.
        /// </summary>
        /// <param name="activity">The message activity</param>
        /// <returns>Clean command text</returns>
        private string GetCommandText(IMessageActivity activity)
        {
            var text = activity.Text ?? string.Empty;

            // If the message contains mentions, remove them
            if (activity.Entities?.Any(e => e.Type == "mention") == true)
            {
                // Remove mention HTML tags like <at>Bot Name</at>
                text = activity.RemoveRecipientMention();
            }

            return text.Trim();
        }
    }
}
