using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Graph.Models;
using Microsoft.Skype.Bots.Media;
using Call = Microsoft.Graph.Models.Call;

namespace TeamsMediaBot.Services
{
    /// <summary>
    /// Manages the complete lifecycle of a single meeting session.
    /// Handles call answering, media streaming, transcription, and cleanup.
    /// </summary>
    public class MeetingSessionManager : IDisposable
    {
        private readonly ICommunicationsClient _communicationsClient;
        private readonly KtorService _ktorService;
        private readonly DeepgramService _deepgramService;
        private readonly GraphCallService _graphCallService;
        private readonly ILogger<MeetingSessionManager> _logger;

        private ICall? _call;
        private string? _sessionId;
        private bool _isDisposed;

        public MeetingSessionManager(
            ICommunicationsClient communicationsClient,
            KtorService ktorService,
            DeepgramService deepgramService,
            GraphCallService graphCallService,
            ILogger<MeetingSessionManager> logger)
        {
            _communicationsClient = communicationsClient ?? throw new ArgumentNullException(nameof(communicationsClient));
            _ktorService = ktorService ?? throw new ArgumentNullException(nameof(ktorService));
            _deepgramService = deepgramService ?? throw new ArgumentNullException(nameof(deepgramService));
            _graphCallService = graphCallService ?? throw new ArgumentNullException(nameof(graphCallService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Handles an incoming call from Microsoft Teams.
        /// Orchestrates the entire flow: registration, media setup, call answering, and monitoring.
        /// </summary>
        /// <param name="call">The incoming call object.</param>
        public async Task HandleIncomingCallAsync(ICall call)
        {
            if (call == null)
            {
                throw new ArgumentNullException(nameof(call));
            }

            _call = call;

            try
            {
                _logger.LogInformation(
                    message: "Handling incoming call. CallId: {CallId}",
                    call.Id);

                // Validate call type - reject if it's not a meeting call
                if (!IsGroupCall(call: call))
                {
                    _logger.LogWarning(
                        message: "Rejecting call - not a group/meeting call. CallId: {CallId}",
                        call.Id);

                    await call.RejectAsync(
                        rejectReason: RejectReason.None,
                        cancellationToken: CancellationToken.None);
                    return;
                }

                // Extract meeting information
                var threadId = call.Resource?.ChatInfo?.ThreadId;
                var messageId = call.Resource?.ChatInfo?.MessageId;
                var tenantId = call.Resource?.TenantId;
                var meetingId = call.Id; // Using call ID as meeting ID

                if (string.IsNullOrWhiteSpace(threadId) ||
                    string.IsNullOrWhiteSpace(messageId) ||
                    string.IsNullOrWhiteSpace(tenantId))
                {
                    _logger.LogError(
                        message: "Missing required meeting information. ThreadId: {ThreadId}, MessageId: {MessageId}, TenantId: {TenantId}",
                        threadId,
                        messageId,
                        tenantId);

                    await call.RejectAsync(
                        rejectReason: RejectReason.None,
                        cancellationToken: CancellationToken.None);
                    return;
                }

                // Step 1: Register session with Ktor
                _logger.LogInformation(message: "Registering session with Ktor.");
                var sessionInfo = await _ktorService.InitializeSessionAsync(
                    threadId: threadId,
                    messageId: messageId,
                    tenantId: tenantId,
                    meetingId: meetingId);

                _sessionId = sessionInfo.SessionId;

                // Step 2: Initialize Deepgram for transcription
                _logger.LogInformation(message: "Initializing Deepgram for session: {SessionId}", _sessionId);
                await _deepgramService.InitializeAsync(
                    onTranscriptReceived: OnTranscriptReceived);

                // Step 3: Connect to Ktor WebSocket for real-time updates
                _logger.LogInformation(message: "Connecting to Ktor WebSocket for session: {SessionId}", _sessionId);
                await _ktorService.ConnectWebSocketAsync(
                    sessionId: _sessionId,
                    onMessageReceived: OnKtorMessageReceived);

                // Step 4: Update recording status to "recording" (compliance requirement)
                _logger.LogInformation(message: "Updating recording status to 'recording' for call: {CallId}", call.Id);
                await _graphCallService.UpdateRecordingStatusAsync(
                    callId: call.Id,
                    status: "recording");

                // Step 5: Create media session and answer the call
                _logger.LogInformation(message: "Creating media session and answering call: {CallId}", call.Id);
                var mediaSession = CreateMediaSession();

                await call.AnswerAsync(
                    mediaSession: mediaSession,
                    cancellationToken: CancellationToken.None);

                _logger.LogInformation(message: "Call answered successfully. CallId: {CallId}", call.Id);

                // Step 6: Send the URL to the meeting chat
                await SendUrlToMeetingChatAsync(
                    url: sessionInfo.Url,
                    call: call);

                // Step 7: Subscribe to call state changes for cleanup
                call.OnUpdated += OnCallUpdated;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    exception: ex,
                    message: "Failed to handle incoming call. CallId: {CallId}",
                    call.Id);

                await RejectAndCleanupAsync(call: call);
                throw;
            }
        }

        /// <summary>
        /// Validates if the call is a group/meeting call (not 1-to-1).
        /// </summary>
        /// <param name="call">The call to validate.</param>
        /// <returns>True if it's a group call, false otherwise.</returns>
        private bool IsGroupCall(ICall call)
        {
            // Check if the call is a meeting scenario (not peer-to-peer)
            // A meeting call typically has a valid ScenarioId
            if (call.Resource?.MeetingInfo != null)
            {
                return true;
            }

            // Alternative check: peer-to-peer calls typically don't have MeetingInfo
            // You can also check if there are multiple participants
            return false;
        }

        /// <summary>
        /// Creates and configures the local media session for audio streaming.
        /// </summary>
        /// <returns>Configured local media session.</returns>
        private ILocalMediaSession CreateMediaSession()
        {
            var audioSettings = new AudioSocketSettings
            {
                StreamDirections = StreamDirection.Recvonly, // We only receive audio
                SupportedAudioFormat = AudioFormat.Pcm16K
            };

            var videoSettings = new VideoSocketSettings
            {
                StreamDirections = StreamDirection.Inactive // No video processing
            };

            var mediaSession = _communicationsClient.CreateMediaSession(
                audioSocketSettings: audioSettings,
                videoSocketSettings: videoSettings);

            // Subscribe to audio events
            mediaSession.AudioSocket.AudioMediaReceived += OnAudioMediaReceived;

            return mediaSession;
        }

        /// <summary>
        /// Handles received audio media from the call.
        /// Forwards audio data to Deepgram for transcription.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Audio media event arguments.</param>
        private void OnAudioMediaReceived(object? sender, AudioMediaReceivedEventArgs e)
        {
            try
            {
                if (e?.Buffer == null)
                {
                    return;
                }

                // Convert audio buffer to byte array
                long length = e.Buffer.Length;
                byte[] audioData = new byte[length];
                Marshal.Copy(
                    source: e.Buffer.Data,
                    destination: audioData,
                    startIndex: 0,
                    length: (int)length);

                // Send to Deepgram for transcription
                _deepgramService.SendAudio(audioData: audioData);
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Error processing audio media.");
            }
            finally
            {
                // Always dispose the buffer
                e?.Buffer?.Dispose();
            }
        }

        /// <summary>
        /// Handles transcripts received from Deepgram.
        /// Forwards them to Ktor via WebSocket.
        /// </summary>
        /// <param name="text">The transcribed text.</param>
        /// <param name="isFinal">Whether this is a final or interim transcript.</param>
        private void OnTranscriptReceived(string text, bool isFinal)
        {
            if (string.IsNullOrWhiteSpace(_sessionId))
            {
                _logger.LogWarning(message: "Cannot send transcript - session ID is null.");
                return;
            }

            try
            {
                _logger.LogDebug(
                    message: "Transcript received: {Text}, IsFinal: {IsFinal}",
                    text,
                    isFinal);

                // Send transcript to Ktor
                _ = _ktorService.SendTranscriptAsync(
                    sessionId: _sessionId,
                    text: text,
                    isFinal: isFinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Error sending transcript to Ktor.");
            }
        }

        /// <summary>
        /// Handles messages received from Ktor via WebSocket.
        /// Logs live summaries to the console.
        /// </summary>
        /// <param name="message">The JSON message from Ktor.</param>
        private void OnKtorMessageReceived(string message)
        {
            try
            {
                _logger.LogDebug(message: "Received message from Ktor: {Message}", message);

                // Parse the message to check if it's a live summary
                var jsonDoc = JsonDocument.Parse(json: message);
                if (jsonDoc.RootElement.TryGetProperty(propertyName: "type", out var typeElement))
                {
                    var messageType = typeElement.GetString();

                    if (messageType == "LIVE_SUMMARY")
                    {
                        var summary = jsonDoc.RootElement.GetProperty(propertyName: "summary").GetString();

                        // Console log as per requirements
                        Console.WriteLine("=================================================");
                        Console.WriteLine($"[LIVE SUMMARY] SessionId: {_sessionId}");
                        Console.WriteLine($"Summary: {summary}");
                        Console.WriteLine("=================================================");

                        _logger.LogInformation(
                            message: "Live summary received for session {SessionId}: {Summary}",
                            _sessionId,
                            summary);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Error processing Ktor message.");
            }
        }

        /// <summary>
        /// Sends the meeting URL to the Teams chat.
        /// </summary>
        /// <param name="url">The URL to send.</param>
        /// <param name="call">The call object.</param>
        private Task SendUrlToMeetingChatAsync(string url, ICall call)
        {
            try
            {
                _logger.LogInformation(
                    message: "Sending URL to meeting chat. URL: {Url}",
                    url);

                // Send a message to the chat with the URL
                var chatMessage = new ChatMessage
                {
                    Body = new ItemBody
                    {
                        Content = $"Meeting transcription is active. View live updates here: {url}"
                    }
                };

                // Note: You'll need to use Microsoft Graph API to send chat messages
                // This requires additional Graph SDK setup and permissions
                // For now, we'll log this as a placeholder
                _logger.LogInformation(
                    message: "URL to send to chat: {Url} (Implementation pending Graph API setup)",
                    url);

                // TODO: Implement actual chat message sending via Graph API
                // This requires:
                // 1. Microsoft.Graph SDK
                // 2. Chat.ReadWrite permission
                // 3. GraphServiceClient setup
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Failed to send URL to meeting chat.");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles call state updates (e.g., when the call ends).
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="args">Resource event arguments.</param>
        private async void OnCallUpdated(ICall sender, ResourceEventArgs<Call> args)
        {
            var callState = args.NewResource?.State;

            if (callState == CallState.Terminated)
            {
                _logger.LogInformation(
                    message: "Call ended. CallId: {CallId}, State: {State}",
                    sender.Id,
                    callState);

                await HandleCallEndAsync(reason: callState.ToString() ?? "Unknown");
            }
        }

        /// <summary>
        /// Handles cleanup when a call ends.
        /// Sends meeting end notification, closes connections, and disposes resources.
        /// </summary>
        /// <param name="reason">The reason for call end.</param>
        private async Task HandleCallEndAsync(string reason)
        {
            if (_isDisposed)
            {
                return;
            }

            try
            {
                _logger.LogInformation(
                    message: "Handling call end. SessionId: {SessionId}, Reason: {Reason}",
                    _sessionId,
                    reason);

                // Step 1: Update recording status to "notRecording" (compliance requirement)
                if (_call != null)
                {
                    _logger.LogInformation(
                        message: "Updating recording status to 'notRecording' for call: {CallId}",
                        _call.Id);

                    await _graphCallService.UpdateRecordingStatusAsync(
                        callId: _call.Id,
                        status: "notRecording");
                }

                // Step 2: Send meeting end notification to Ktor
                if (!string.IsNullOrWhiteSpace(_sessionId))
                {
                    _logger.LogInformation(message: "Sending meeting end notification to Ktor.");
                    await _ktorService.SendMeetingEndAsync(
                        sessionId: _sessionId,
                        reason: reason);
                }

                // Step 3: Disconnect from Ktor WebSocket
                _logger.LogInformation(message: "Disconnecting from Ktor WebSocket.");
                await _ktorService.DisconnectWebSocketAsync();

                // Step 4: Disconnect from Deepgram
                _logger.LogInformation(message: "Disconnecting from Deepgram.");
                await _deepgramService.DisconnectAsync();

                _logger.LogInformation(message: "Call end handling completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Error during call end handling.");
            }
            finally
            {
                Dispose();
            }
        }

        /// <summary>
        /// Rejects a call and performs cleanup.
        /// </summary>
        /// <param name="call">The call to reject.</param>
        private async Task RejectAndCleanupAsync(ICall call)
        {
            try
            {
                await call.RejectAsync(
                    rejectReason: RejectReason.None,
                    cancellationToken: CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Error rejecting call.");
            }
            finally
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            try
            {
                _logger.LogInformation(message: "Disposing meeting session manager.");

                _ktorService?.Dispose();
                _deepgramService?.Dispose();
                // _call?.Resource?.Dispose(); // There is no dispose for call.Resource

                _isDisposed = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Error during disposal.");
            }
        }
    }
}
