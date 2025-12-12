using Microsoft.Bot.Builder.Integration.AspNet.Core;

namespace TeamsMediaBot.Bot
{
    /// <summary>
    /// Bot Framework HTTP adapter with centralized error handling.
    /// Uses modern CloudAdapter with ConfigurationBotFrameworkAuthentication (SDK 4.14+).
    /// </summary>
    public class AdapterWithErrorHandler : Microsoft.Bot.Builder.Integration.AspNet.Core.CloudAdapter
    {
        /// <summary>
        /// Initializes a new instance of the AdapterWithErrorHandler class.
        /// </summary>
        /// <param name="auth">Bot Framework authentication provider</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="logger">Logger instance</param>
        public AdapterWithErrorHandler(
            Microsoft.Bot.Connector.Authentication.BotFrameworkAuthentication auth,
            IConfiguration configuration,
            ILogger<AdapterWithErrorHandler> logger)
            : base(auth, logger)
        {
            var appId = configuration["MicrosoftAppId"];
            logger.LogWarning($"Bot AppId: {appId}");
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
