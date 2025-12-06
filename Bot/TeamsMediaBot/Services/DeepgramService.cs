using Deepgram;
using Deepgram.Models.Listen.v2.WebSocket;
using Microsoft.Extensions.Options;
using TeamsMediaBot.Configuration;

namespace TeamsMediaBot.Services
{
    /// <summary>
    /// Service responsible for handling Deepgram real-time transcription.
    /// Manages the WebSocket connection to Deepgram and processes audio streams.
    /// </summary>
    public class DeepgramService : IDisposable
    {
        private readonly DeepgramConfiguration _config;
        private readonly ILogger<DeepgramService> _logger;
        private ListenWebSocketClient? _deepgramClient;
        private bool _isConnected;

        public DeepgramService(
            IOptions<DeepgramConfiguration> config,
            ILogger<DeepgramService> logger)
        {
            _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Initializes and connects to Deepgram for real-time transcription.
        /// </summary>
        /// <param name="onTranscriptReceived">Callback invoked when a transcript is received.</param>
        public async Task InitializeAsync(Action<string, bool> onTranscriptReceived)
        {
            if (onTranscriptReceived == null)
            {
                throw new ArgumentNullException(nameof(onTranscriptReceived));
            }

            if (string.IsNullOrWhiteSpace(_config.ApiKey))
            {
                throw new InvalidOperationException("Deepgram API key is not configured.");
            }

            try
            {
                // Initialize Deepgram library
                Library.Initialize();

                _logger.LogInformation(message: "Initializing Deepgram client.");

                _deepgramClient = new ListenWebSocketClient(apiKey: _config.ApiKey);

                // Subscribe to transcript events
                await _deepgramClient.Subscribe(new EventHandler<ResultResponse>(async (sender, result) =>
                {
                    await HandleTranscriptResultAsync(
                        result: result,
                        onTranscriptReceived: onTranscriptReceived);
                }));

                // Configure Deepgram settings
                var liveSchema = new LiveSchema
                {
                    Model = _config.Model,
                    Encoding = "linear16",
                    SampleRate = 16000,
                    Channels = 1,
                    Punctuate = true,
                    SmartFormat = true,
                    InterimResults = true,
                    Language = _config.Language,
                    Diarize = true,
                };

                _logger.LogInformation(
                    message: "Connecting to Deepgram with model: {Model}, language: {Language}",
                    _config.Model,
                    _config.Language);

                // Connect to Deepgram
                bool connected = await _deepgramClient.Connect(options: liveSchema);

                if (!connected)
                {
                    throw new InvalidOperationException("Failed to connect to Deepgram.");
                }

                _isConnected = true;
                _logger.LogInformation(message: "Deepgram connected successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Failed to initialize Deepgram.");
                throw;
            }
        }

        /// <summary>
        /// Handles transcript results from Deepgram.
        /// </summary>
        /// <param name="result">The result response from Deepgram.</param>
        /// <param name="onTranscriptReceived">Callback to invoke with the transcript.</param>
        private Task HandleTranscriptResultAsync(
            ResultResponse result,
            Action<string, bool> onTranscriptReceived)
        {
            try
            {
                if (result?.Channel?.Alternatives == null || result.Channel.Alternatives.Count == 0)
                {
                    return Task.CompletedTask;
                }

                var transcript = result.Channel.Alternatives[0]?.Transcript;

                if (string.IsNullOrWhiteSpace(transcript))
                {
                    return Task.CompletedTask;
                }

                bool isFinal = result.IsFinal ?? false;

                _logger.LogDebug(
                    message: "Transcript received: {Transcript}, IsFinal: {IsFinal}",
                    transcript,
                    isFinal);

                onTranscriptReceived?.Invoke(transcript, isFinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Error handling transcript result.");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Sends audio data to Deepgram for transcription.
        /// </summary>
        /// <param name="audioData">The audio data bytes (PCM 16kHz format).</param>
        public void SendAudio(byte[] audioData)
        {
            if (audioData == null || audioData.Length == 0)
            {
                _logger.LogWarning(message: "Attempted to send null or empty audio data.");
                return;
            }

            if (!_isConnected || _deepgramClient == null)
            {
                _logger.LogWarning(message: "Cannot send audio - Deepgram is not connected.");
                return;
            }

            try
            {
                _deepgramClient.Send(data: audioData);
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Failed to send audio to Deepgram.");
            }
        }

        /// <summary>
        /// Closes the connection to Deepgram.
        /// </summary>
        public async Task DisconnectAsync()
        {
            if (_deepgramClient != null && _isConnected)
            {
                try
                {
                    _logger.LogInformation(message: "Disconnecting from Deepgram.");

                    // Send a final close signal to Deepgram
                    await Task.Run(() =>
                    {
                        _deepgramClient.Stop();
                    });

                    _isConnected = false;
                    _logger.LogInformation(message: "Deepgram disconnected successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(exception: ex, message: "Error while disconnecting from Deepgram.");
                }
            }
        }

        public void Dispose()
        {
            try
            {
                if (_isConnected)
                {
                    _deepgramClient?.Stop();
                    _isConnected = false;
                }

                _deepgramClient?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Error during Deepgram disposal.");
            }
        }
    }
}
