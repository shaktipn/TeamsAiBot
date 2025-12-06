using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TeamsMediaBot.Configuration;
using TeamsMediaBot.Models;

namespace TeamsMediaBot.Services
{
    /// <summary>
    /// Service responsible for all communication with the Ktor server.
    /// Handles HTTP API calls and WebSocket connections.
    /// </summary>
    public class KtorService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly KtorConfiguration _config;
        private readonly ILogger<KtorService> _logger;
        private ClientWebSocket? _webSocket;
        private CancellationTokenSource? _wsCancellationTokenSource;
        private Task? _wsListenerTask;

        public KtorService(
            HttpClient httpClient,
            IOptions<KtorConfiguration> config,
            ILogger<KtorService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes a new bot session with the Ktor server.
        /// Sends meeting details and receives session ID and URL to share in chat.
        /// </summary>
        /// <param name="threadId">The Teams thread ID.</param>
        /// <param name="messageId">The Teams message ID.</param>
        /// <param name="tenantId">The Azure AD tenant ID.</param>
        /// <param name="meetingId">The Teams meeting ID.</param>
        /// <returns>Session initialization response containing session ID and URL.</returns>
        public async Task<KtorSessionInitResponse> InitializeSessionAsync(
            string threadId,
            string messageId,
            string tenantId,
            string meetingId)
        {
            if (string.IsNullOrWhiteSpace(threadId))
            {
                throw new ArgumentException(message: "Thread ID cannot be null or empty.", paramName: nameof(threadId));
            }

            if (string.IsNullOrWhiteSpace(messageId))
            {
                throw new ArgumentException(message: "Message ID cannot be null or empty.", paramName: nameof(messageId));
            }

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                throw new ArgumentException(message: "Tenant ID cannot be null or empty.", paramName: nameof(tenantId));
            }

            if (string.IsNullOrWhiteSpace(meetingId))
            {
                throw new ArgumentException(message: "Meeting ID cannot be null or empty.", paramName: nameof(meetingId));
            }

            var requestPayload = new KtorSessionInitRequest
            {
                ThreadId = threadId,
                MessageId = messageId,
                TenantId = tenantId,
                MeetingId = meetingId
            };

            var endpoint = $"{_config.ApiBaseUrl}{_config.SessionInitEndpoint}";
            _logger.LogInformation(
                message: "Initializing session with Ktor. Endpoint: {Endpoint}, MeetingId: {MeetingId}",
                endpoint,
                meetingId);

            try
            {
                var response = await _httpClient.PostAsJsonAsync(
                    requestUri: endpoint,
                    value: requestPayload);

                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<KtorSessionInitResponse>();

                if (result == null)
                {
                    throw new InvalidOperationException("Received null response from Ktor session initialization.");
                }

                _logger.LogInformation(
                    message: "Session initialized successfully. SessionId: {SessionId}",
                    result.SessionId);

                return result;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(
                    exception: ex,
                    message: "Failed to initialize session with Ktor. Endpoint: {Endpoint}",
                    endpoint);
                throw;
            }
        }

        /// <summary>
        /// Connects to the Ktor WebSocket for real-time communication.
        /// Starts listening for incoming messages (e.g., live summaries).
        /// </summary>
        /// <param name="sessionId">The session ID to include in the connection.</param>
        /// <param name="onMessageReceived">Callback invoked when a message is received from Ktor.</param>
        public async Task ConnectWebSocketAsync(
            string sessionId,
            Action<string> onMessageReceived)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new ArgumentException(message: "Session ID cannot be null or empty.", paramName: nameof(sessionId));
            }

            if (onMessageReceived == null)
            {
                throw new ArgumentNullException(nameof(onMessageReceived));
            }

            _webSocket = new ClientWebSocket();
            _wsCancellationTokenSource = new CancellationTokenSource();

            var wsUrl = $"{_config.WebSocketUrl}?sessionId={sessionId}";
            _logger.LogInformation(
                message: "Connecting to Ktor WebSocket. URL: {Url}",
                wsUrl);

            try
            {
                await _webSocket.ConnectAsync(
                    uri: new Uri(wsUrl),
                    cancellationToken: _wsCancellationTokenSource.Token);

                _logger.LogInformation(message: "WebSocket connected successfully. SessionId: {SessionId}", sessionId);

                // Start listening for incoming messages
                _wsListenerTask = Task.Run(async () => await ListenForMessagesAsync(onMessageReceived: onMessageReceived));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    exception: ex,
                    message: "Failed to connect to Ktor WebSocket. SessionId: {SessionId}",
                    sessionId);
                throw;
            }
        }

        /// <summary>
        /// Listens for incoming messages from the Ktor WebSocket.
        /// </summary>
        /// <param name="onMessageReceived">Callback to invoke with received messages.</param>
        private async Task ListenForMessagesAsync(Action<string> onMessageReceived)
        {
            var buffer = new byte[4096];

            try
            {
                while (_webSocket?.State == WebSocketState.Open &&
                       _wsCancellationTokenSource?.Token.IsCancellationRequested == false)
                {
                    var result = await _webSocket.ReceiveAsync(
                        buffer: new ArraySegment<byte>(buffer),
                        cancellationToken: _wsCancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, index: 0, count: result.Count);

                        _logger.LogDebug(message: "Received WebSocket message: {Message}", message);

                        onMessageReceived?.Invoke(message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogInformation(message: "WebSocket closed by server.");
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation(message: "WebSocket listener cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Error in WebSocket listener.");
            }
        }

        /// <summary>
        /// Sends a transcript message to Ktor via WebSocket.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        /// <param name="text">The transcribed text.</param>
        /// <param name="isFinal">Whether this is a final or interim transcript.</param>
        public async Task SendTranscriptAsync(string sessionId, string text, bool isFinal)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new ArgumentException(message: "Session ID cannot be null or empty.", paramName: nameof(sessionId));
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException(message: "Text cannot be null or empty.", paramName: nameof(text));
            }

            if (_webSocket?.State != WebSocketState.Open)
            {
                _logger.LogWarning(message: "Cannot send transcript - WebSocket is not open.");
                return;
            }

            var message = new TranscriptMessage
            {
                SessionId = sessionId,
                Text = text,
                IsFinal = isFinal
            };

            await SendWebSocketMessageAsync(message: message);
        }

        /// <summary>
        /// Sends a meeting end notification to Ktor via WebSocket.
        /// </summary>
        /// <param name="sessionId">The session ID.</param>
        /// <param name="reason">The reason for meeting end.</param>
        public async Task SendMeetingEndAsync(string sessionId, string reason)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new ArgumentException(message: "Session ID cannot be null or empty.", paramName: nameof(sessionId));
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException(message: "Reason cannot be null or empty.", paramName: nameof(reason));
            }

            if (_webSocket?.State != WebSocketState.Open)
            {
                _logger.LogWarning(message: "Cannot send meeting end - WebSocket is not open.");
                return;
            }

            var message = new MeetingEndMessage
            {
                SessionId = sessionId,
                Reason = reason
            };

            await SendWebSocketMessageAsync(message: message);
        }

        /// <summary>
        /// Helper method to send a message via WebSocket.
        /// </summary>
        /// <param name="message">The message object to serialize and send.</param>
        private async Task SendWebSocketMessageAsync(object message)
        {
            try
            {
                var json = JsonSerializer.Serialize(value: message);
                var bytes = Encoding.UTF8.GetBytes(json);

                await _webSocket!.SendAsync(
                    buffer: new ArraySegment<byte>(bytes),
                    messageType: WebSocketMessageType.Text,
                    endOfMessage: true,
                    cancellationToken: CancellationToken.None);

                _logger.LogDebug(message: "Sent WebSocket message: {Json}", json);
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Failed to send WebSocket message.");
            }
        }

        /// <summary>
        /// Closes the WebSocket connection gracefully.
        /// </summary>
        public async Task DisconnectWebSocketAsync()
        {
            try
            {
                if (_webSocket?.State == WebSocketState.Open)
                {
                    _logger.LogInformation(message: "Closing WebSocket connection.");

                    await _webSocket.CloseAsync(
                        closeStatus: WebSocketCloseStatus.NormalClosure,
                        statusDescription: "Meeting ended",
                        cancellationToken: CancellationToken.None);
                }

                _wsCancellationTokenSource?.Cancel();

                if (_wsListenerTask != null)
                {
                    await _wsListenerTask;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Error while closing WebSocket.");
            }
        }

        public void Dispose()
        {
            _wsCancellationTokenSource?.Cancel();
            _wsCancellationTokenSource?.Dispose();
            _webSocket?.Dispose();
        }
    }
}
