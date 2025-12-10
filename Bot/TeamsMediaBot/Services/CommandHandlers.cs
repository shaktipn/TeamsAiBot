namespace TeamsMediaBot.Services
{
    /// <summary>
    /// Handles execution of parsed commands and generates appropriate responses.
    /// </summary>
    public class CommandHandlers
    {
        private readonly MeetingJoinService _meetingJoinService;
        private readonly BotMediaService _botMediaService;
        private readonly ILogger<CommandHandlers> _logger;

        /// <summary>
        /// Initializes a new instance of the CommandHandlers class.
        /// </summary>
        public CommandHandlers(
            MeetingJoinService meetingJoinService,
            BotMediaService botMediaService,
            ILogger<CommandHandlers> logger)
        {
            _meetingJoinService = meetingJoinService ?? throw new ArgumentNullException(nameof(meetingJoinService));
            _botMediaService = botMediaService ?? throw new ArgumentNullException(nameof(botMediaService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles a join command to join a Teams meeting.
        /// </summary>
        /// <param name="command">The join command with meeting URL</param>
        /// <param name="tenantId">The Azure AD tenant ID</param>
        /// <returns>Response message for the user</returns>
        public async Task<string> HandleJoinCommandAsync(JoinCommand command, string tenantId)
        {
            try
            {
                // Validate that URL was provided
                if (string.IsNullOrWhiteSpace(command.MeetingUrl))
                {
                    return "Please provide a meeting URL. Usage: `join <meeting_url>`";
                }

                _logger.LogInformation("Handling join command for URL: {Url}", command.MeetingUrl);

                // Attempt to join the meeting
                var result = await _meetingJoinService.JoinMeetingAsync(command.MeetingUrl, tenantId);

                if (result.Success)
                {
                    return $"✅ Successfully joined the meeting! Transcription will begin shortly.\n\nCall ID: `{result.CallId}`";
                }
                else
                {
                    _logger.LogError("Failed to join meeting: {Error}", result.Error);
                    return $"❌ Failed to join meeting: {result.Error}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling join command");
                return $"❌ An error occurred while trying to join the meeting: {ex.Message}";
            }
        }

        /// <summary>
        /// Handles a help command to show available commands.
        /// </summary>
        /// <returns>Help message with command list</returns>
        public string HandleHelpCommand()
        {
            return @"**Available Commands:**

**Slash commands:**
• `/join <meeting_url>` - Join a Teams meeting and start transcription
• `/status` - Show active meeting sessions
• `/help` - Show this message

**Or mention me:**
• `@Surya AI Bot join <url>` - Join a meeting
• `@Surya AI Bot status` - Show status
• `@Surya AI Bot help` - Show this help
• `@Surya AI Bot hi` - Say hello

**Example:**
```
/join https://teams.microsoft.com/l/meetup-join/...
```";
        }

        /// <summary>
        /// Handles a status command to show active sessions.
        /// </summary>
        /// <returns>Status message with session count</returns>
        public string HandleStatusCommand()
        {
            var count = _botMediaService.GetActiveSessionCount();

            if (count == 0)
            {
                return "📊 No active meetings. Use `/join <url>` to join a meeting.";
            }
            else if (count == 1)
            {
                return "📊 Currently active in **1 meeting**.";
            }
            else
            {
                return $"📊 Currently active in **{count} meetings**.";
            }
        }

        /// <summary>
        /// Handles a greeting command.
        /// </summary>
        /// <returns>Greeting response</returns>
        public string HandleGreetingCommand()
        {
            return @"👋 Hi! I'm **Surya AI Bot**.

I can join Teams meetings and provide live transcriptions with speaker identification.

Type `/help` to see what I can do!";
        }

        /// <summary>
        /// Handles an unknown command.
        /// </summary>
        /// <returns>Error message for unknown command</returns>
        public string HandleUnknownCommand()
        {
            return "❓ Unknown command. Type `/help` to see available commands.";
        }
    }
}
