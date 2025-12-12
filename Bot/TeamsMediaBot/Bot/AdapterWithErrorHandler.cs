using Microsoft.Bot.Builder.Integration.AspNet.Core;

namespace TeamsMediaBot.Bot
{
    /// <summary>
    /// Bot Framework HTTP adapter with centralized error handling.
    /// </summary>
    public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
    {
        /// <summary>
        /// Initializes a new instance of the AdapterWithErrorHandler class.
        /// </summary>
        /// <param name="configuration">Application configuration</param>
        /// <param name="logger">Logger instance</param>
        public AdapterWithErrorHandler(
            IConfiguration configuration,
            ILogger<BotFrameworkHttpAdapter> logger)
            : base(configuration, logger)
        {
            var appId = configuration["MicrosoftAppId"];
            var appPass = configuration["MicrosoftAppPassword"];
            logger.LogWarning($"Bot AppId: {appId}");
            logger.LogWarning($"Bot Password exists: {!string.IsNullOrEmpty(appPass)}");
            logger.LogWarning($"Bot Password length: {appPass?.Length ?? 0}");
            OnTurnError = async (turnContext, exception) =>
            {
                // Log the exception with full details
                logger.LogError(exception, "Error processing turn: {ErrorMessage}", exception.Message);

                // Send a friendly error message to the user
                try
                {
                    await turnContext.SendActivityAsync(
                        "❌ Sorry, something went wrong. Please try again or type `/help` for assistance.");
                }
                catch (Exception sendEx)
                {
                    // If we can't even send an error message, log it
                    logger.LogError(sendEx, "Failed to send error message to user");
                }
            };
        }
    }
}
