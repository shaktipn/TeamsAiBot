using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Identity.Client;

namespace TeamsMediaBot.Services
{
    /// <summary>
    /// Service for making direct Microsoft Graph API calls.
    /// Handles operations not available in Graph Communications SDK.
    /// </summary>
    public class GraphCallService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfidentialClientApplication _msalClient;
        private readonly ILogger<GraphCallService> _logger;

        private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";
        private static readonly string[] Scopes = ["https://graph.microsoft.com/.default"];

        public GraphCallService(
            HttpClient httpClient,
            IConfidentialClientApplication msalClient,
            ILogger<GraphCallService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _msalClient = msalClient ?? throw new ArgumentNullException(nameof(msalClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Updates the recording status for a Teams call via Microsoft Graph API.
        /// Required for compliance when accessing real-time media.
        /// </summary>
        /// <param name="callId">The Teams call ID.</param>
        /// <param name="status">Status: "recording", "notRecording", or "failed".</param>
        /// <param name="clientContext">Optional unique context (auto-generated if null).</param>
        public async Task UpdateRecordingStatusAsync(
            string callId,
            string status,
            string? clientContext = null)
        {
            if (string.IsNullOrWhiteSpace(callId))
            {
                throw new ArgumentException("Call ID cannot be null or empty.", nameof(callId));
            }

            if (string.IsNullOrWhiteSpace(status))
            {
                throw new ArgumentException("Status cannot be null or empty.", nameof(status));
            }

            // Validate status value
            var validStatuses = new[] { "recording", "notRecording", "failed" };
            if (!validStatuses.Contains(status))
            {
                throw new ArgumentException(
                    $"Status must be one of: {string.Join(", ", validStatuses)}",
                    nameof(status));
            }

            // Generate client context if not provided
            clientContext ??= $"{callId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            // Ensure clientContext doesn't exceed 256 characters
            if (clientContext.Length > 256)
            {
                clientContext = clientContext.Substring(0, 256);
            }

            var endpoint = $"{GraphBaseUrl}/communications/calls/{callId}/updateRecordingStatus";

            _logger.LogInformation(
                "Updating recording status via Graph API. CallId: {CallId}, Status: {Status}, ClientContext: {ClientContext}",
                callId, status, clientContext);

            try
            {
                // Acquire access token
                var authResult = await _msalClient
                    .AcquireTokenForClient(Scopes)
                    .ExecuteAsync();

                // Prepare request payload
                var payload = new
                {
                    clientContext = clientContext,
                    status = status
                };

                var jsonContent = JsonSerializer.Serialize(payload);
                var httpContent = new StringContent(
                    jsonContent,
                    Encoding.UTF8,
                    "application/json");

                // Create HTTP request
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = httpContent
                };
                request.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    authResult.AccessToken);

                // Send request
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Recording status updated successfully. CallId: {CallId}, Status: {Status}",
                        callId, status);
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "Failed to update recording status. CallId: {CallId}, Status: {Status}, " +
                        "StatusCode: {StatusCode}, Error: {Error}",
                        callId, status, response.StatusCode, errorContent);

                    response.EnsureSuccessStatusCode(); // Throw exception
                }
            }
            catch (MsalException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to acquire access token for Graph API. CallId: {CallId}",
                    callId);
                throw;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    ex,
                    "HTTP request failed for recording status update. CallId: {CallId}",
                    callId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error updating recording status. CallId: {CallId}",
                    callId);
                throw;
            }
        }
    }
}
