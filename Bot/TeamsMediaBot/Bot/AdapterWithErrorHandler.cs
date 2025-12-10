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
            OnTurnError = async (turnContext, exception) =>
            {
                // Log the exception
                logger.LogError(exception, "Error processing turn");

                // Send a friendly error message to the user
                await turnContext.SendActivityAsync(
                    "❌ Sorry, something went wrong. Please try again or type `/help` for assistance.");
            };
        }
    }
}
