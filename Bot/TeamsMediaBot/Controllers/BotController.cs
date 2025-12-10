using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph.Communications.Client;
using TeamsMediaBot.Services;

namespace TeamsMediaBot.Controllers
{
    /// <summary>
    /// API controller that handles incoming webhooks from Microsoft Teams.
    /// This is the entry point for call notifications from the Teams platform.
    /// </summary>
    [ApiController]
    [Route("api")]
    public class BotController : ControllerBase
    {
        private readonly BotMediaService _botMediaService;
        private readonly Microsoft.Bot.Builder.Integration.AspNet.Core.IBotFrameworkHttpAdapter _adapter;
        private readonly Microsoft.Bot.Builder.IBot _bot;
        private readonly ILogger<BotController> _logger;

        public BotController(
            BotMediaService botMediaService,
            Microsoft.Bot.Builder.Integration.AspNet.Core.IBotFrameworkHttpAdapter adapter,
            Microsoft.Bot.Builder.IBot bot,
            ILogger<BotController> logger)
        {
            _botMediaService = botMediaService ?? throw new ArgumentNullException(nameof(botMediaService));
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _bot = bot ?? throw new ArgumentNullException(nameof(bot));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Webhook endpoint called by Microsoft Teams for call signaling.
        /// Handles all incoming call notifications, state changes, and events.
        /// </summary>
        /// <returns>HTTP 202 Accepted to acknowledge receipt.</returns>
        [HttpPost]
        [Route("calls")]
        public async Task<IActionResult> HandleIncomingCall()
        {
            _logger.LogInformation(message: "Received incoming call notification from Teams.");

            try
            {
                // Convert ASP.NET Core request to HttpRequestMessage for Graph SDK
                var httpRequestMessage = await ConvertToHttpRequestMessageAsync();

                // Process the notification through the Graph Communications Client
                await _botMediaService.Client.ProcessNotificationAsync(
                    request: httpRequestMessage);

                _logger.LogInformation(message: "Call notification processed successfully.");

                return Accepted();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    exception: ex,
                    message: "Error processing incoming call notification.");

                // Return 202 even on error to prevent Teams from retrying
                return Accepted();
            }
        }

        /// <summary>
        /// Bot Framework messaging endpoint.
        /// Handles Teams messages, mentions, and commands.
        /// </summary>
        /// <returns>HTTP 200 OK when message is processed.</returns>
        [HttpPost]
        [Route("messages")]
        public async Task<IActionResult> HandleMessagesAsync()
        {
            _logger.LogInformation("Received message activity from Teams");

            try
            {
                // Process the incoming activity using Bot Framework
                // The adapter handles JWT validation, deserializes Activity objects,
                // and routes to TeamsBot.OnMessageActivityAsync()
                await _adapter.ProcessAsync(Request, Response, _bot);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message activity");
                return StatusCode(500);
            }
        }

        /// <summary>
        /// Health check endpoint to verify the bot is running.
        /// </summary>
        /// <returns>HTTP 200 OK with status information.</returns>
        [HttpGet]
        [Route("health")]
        public IActionResult HealthCheck()
        {
            var activeSessionCount = _botMediaService.GetActiveSessionCount();

            var status = new
            {
                Status = "Healthy",
                ActiveSessions = activeSessionCount,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation(
                message: "Health check - Active sessions: {Count}",
                activeSessionCount);

            return Ok(value: status);
        }

        /// <summary>
        /// Converts an ASP.NET Core HttpRequest to HttpRequestMessage.
        /// Required by the Microsoft Graph Communications SDK.
        /// </summary>
        /// <returns>Converted HttpRequestMessage.</returns>
        private async Task<HttpRequestMessage> ConvertToHttpRequestMessageAsync()
        {
            var httpRequestMessage = new HttpRequestMessage(
                method: HttpMethod.Post,
                requestUri: $"{Request.Scheme}://{Request.Host}{Request.Path}");

            // Copy request body
            using (var memoryStream = new MemoryStream())
            {
                await Request.Body.CopyToAsync(destination: memoryStream);
                memoryStream.Position = 0;

                var bodyContent = Encoding.UTF8.GetString(bytes: memoryStream.ToArray());
                httpRequestMessage.Content = new StringContent(
                    content: bodyContent,
                    encoding: Encoding.UTF8,
                    mediaType: "application/json");
            }

            // Copy all headers (critical for authentication validation)
            foreach (var header in Request.Headers)
            {
                httpRequestMessage.Headers.TryAddWithoutValidation(
                    name: header.Key,
                    values: header.Value.ToArray());
            }

            return httpRequestMessage;
        }
    }
}
