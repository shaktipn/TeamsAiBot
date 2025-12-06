using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using Microsoft.Graph.Communications.Calls;
using Microsoft.Graph.Communications.Calls.Media;
using Microsoft.Graph.Communications.Client;
using Microsoft.Graph.Communications.Client.Authentication;
using Microsoft.Graph.Communications.Common.Telemetry;
using Microsoft.Graph.Communications.Resources;
using Microsoft.Identity.Client;
using Microsoft.Skype.Bots.Media;
using TeamsMediaBot.Configuration;

namespace TeamsMediaBot.Services
{
    /// <summary>
    /// Main bot service that manages the Graph Communications Client
    /// and coordinates incoming call handling across multiple concurrent meetings.
    /// </summary>
    public class BotMediaService
    {
        private readonly ILogger<BotMediaService> _logger;
        private readonly IGraphLogger _graphLogger;
        private readonly IServiceProvider _serviceProvider;
        private readonly AzureAdConfiguration _azureAdConfig;
        private readonly BotConfiguration _botConfig;
        private readonly IConfidentialClientApplication _msalClient;

        private readonly ConcurrentDictionary<string, MeetingSessionManager> _activeSessions
            = new ConcurrentDictionary<string, MeetingSessionManager>();

        public ICommunicationsClient Client { get; private set; }

        public BotMediaService(
            ILogger<BotMediaService> logger,
            IGraphLogger graphLogger,
            IServiceProvider serviceProvider,
            IOptions<AzureAdConfiguration> azureAdConfig,
            IOptions<BotConfiguration> botConfig,
            IConfidentialClientApplication msalClient)
        {
            Console.WriteLine("[TRACE] BotMediaService constructor started");
            Console.Out.Flush();

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _graphLogger = graphLogger ?? throw new ArgumentNullException(nameof(graphLogger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _azureAdConfig = azureAdConfig?.Value ?? throw new ArgumentNullException(nameof(azureAdConfig));
            _botConfig = botConfig?.Value ?? throw new ArgumentNullException(nameof(botConfig));
            _msalClient = msalClient ?? throw new ArgumentNullException(nameof(msalClient));

            Console.WriteLine("[TRACE] BotMediaService: About to call InitializeGraphCommunicationsClient");
            Console.Out.Flush();

            Client = InitializeGraphCommunicationsClient();

            Console.WriteLine("[TRACE] BotMediaService constructor completed");
            Console.Out.Flush();
        }

        /// <summary>
        /// Initializes the Microsoft Graph Communications Client with media platform support.
        /// </summary>
        /// <returns>Configured communications client.</returns>
        private ICommunicationsClient InitializeGraphCommunicationsClient()
        {
            Console.WriteLine("[TRACE] InitializeGraphCommunicationsClient() started");
            Console.Out.Flush();

            _logger.LogInformation(message: "Initializing Graph Communications Client.");

            // Check if running on Windows (Media Platform SDK requires Windows)
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var errorMessage =
                    "ERROR: Microsoft Graph Media Platform SDK requires Windows.\n" +
                    "This bot uses real-time media capabilities that depend on Windows-specific libraries.\n" +
                    $"Current platform: {RuntimeInformation.OSDescription}\n" +
                    "The application cannot run on macOS or Linux.\n" +
                    "Please run this bot on a Windows machine or Windows Server.";

                _logger.LogError(message: errorMessage);
                Console.Error.WriteLine(errorMessage);

                // Exit gracefully
                Environment.Exit(1);
            }

            // Validate configuration
            if (string.IsNullOrWhiteSpace(_azureAdConfig.ClientId))
            {
                throw new InvalidOperationException("Azure AD ClientId is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_azureAdConfig.ClientSecret))
            {
                throw new InvalidOperationException("Azure AD ClientSecret is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_azureAdConfig.TenantId))
            {
                throw new InvalidOperationException("Azure AD TenantId is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_botConfig.BotBaseUrl))
            {
                throw new InvalidOperationException("Bot BotBaseUrl is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_botConfig.PublicIpAddress))
            {
                throw new InvalidOperationException("Bot PublicIpAddress is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_botConfig.CertificateThumbprint))
            {
                throw new InvalidOperationException("Bot CertificateThumbprint is not configured.");
            }

            // Create authentication provider using injected MSAL client
            var authProvider = new ClientCredentialAuthProvider(
                msalClient: _msalClient,
                logger: _logger);

            // Configure media platform settings
            var mediaPlatformSettings = new MediaPlatformSettings
            {
                MediaPlatformInstanceSettings = new MediaPlatformInstanceSettings
                {
                    CertificateThumbprint = _botConfig.CertificateThumbprint,
                    InstanceInternalPort = _botConfig.InstanceInternalPort,
                    InstancePublicIPAddress = IPAddress.Parse(_botConfig.PublicIpAddress),
                    InstancePublicPort = _botConfig.InstancePublicPort,
                    ServiceFqdn = _botConfig.ServiceFqdn
                },
                ApplicationId = _azureAdConfig.ClientId
            };

            _logger.LogInformation(
                message: "Media platform configured. PublicIP: {PublicIp}, Port: {Port}",
                _botConfig.PublicIpAddress,
                _botConfig.InstancePublicPort);

            // Build the communications client
            var clientBuilder = new CommunicationsClientBuilder(
                appName: "TeamsMediaBot",
                appId: _azureAdConfig.ClientId,
                logger: _graphLogger);

            clientBuilder.SetAuthenticationProvider(authenticationProvider: authProvider);
            clientBuilder.SetServiceBaseUrl(serviceBaseUrlInput: new Uri(_botConfig.BotBaseUrl));
            clientBuilder.SetMediaPlatformSettings(mediaSettings: mediaPlatformSettings);

            var client = clientBuilder.Build();

            // Subscribe to incoming call events
            client.Calls().OnIncoming += OnIncomingCallReceived;

            _logger.LogInformation(message: "Graph Communications Client initialized successfully.");

            return client;
        }

        /// <summary>
        /// Handles incoming call notifications from Microsoft Teams.
        /// Creates a new MeetingSessionManager for each call.
        /// </summary>
        /// <param name="sender">The call collection.</param>
        /// <param name="args">Event arguments containing the incoming call.</param>
        private void OnIncomingCallReceived(
            ICallCollection sender,
            CollectionEventArgs<ICall> args)
        {
            foreach (var call in args.AddedResources)
            {
                // Ignore invalid calls
                if (call?.Id == null)
                {
                    _logger.LogWarning(message: "Received incoming call with null ID, ignoring.");
                    continue;
                }

                _logger.LogInformation(
                    message: "Incoming call received. CallId: {CallId}",
                    call.Id);

                // Process each call asynchronously
                _ = Task.Run(async () => await HandleIncomingCallAsync(call: call));
            }
        }

        /// <summary>
        /// Processes an incoming call by creating a session manager and delegating the handling.
        /// </summary>
        /// <param name="call">The incoming call to handle.</param>
        private async Task HandleIncomingCallAsync(ICall call)
        {
            MeetingSessionManager? sessionManager = null;

            try
            {
                _logger.LogInformation(
                    message: "Creating session manager for call: {CallId}",
                    call.Id);

                // Create service instances for this session
                // Each session gets its own instances to handle concurrent meetings independently
                sessionManager = CreateSessionManager();

                // Track the session
                _activeSessions.TryAdd(key: call.Id, value: sessionManager);

                _logger.LogInformation(
                    message: "Active sessions count: {Count}",
                    _activeSessions.Count);

                // Delegate call handling to the session manager
                await sessionManager.HandleIncomingCallAsync(call: call);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    exception: ex,
                    message: "Failed to handle incoming call. CallId: {CallId}",
                    call.Id);

                // Try to reject the call on error
                try
                {
                    await call.RejectAsync(
                        rejectReason: Microsoft.Graph.Models.RejectReason.None,
                        cancellationToken: CancellationToken.None);
                }
                catch (Exception rejectEx)
                {
                    _logger.LogError(
                        exception: rejectEx,
                        message: "Failed to reject call after error. CallId: {CallId}",
                        call.Id);
                }

                // Clean up the session
                sessionManager?.Dispose();
                _activeSessions.TryRemove(key: call.Id, value: out _);
            }
        }

        /// <summary>
        /// Creates a new MeetingSessionManager with fresh service instances.
        /// Each session gets isolated services to prevent cross-contamination.
        /// </summary>
        /// <returns>A new meeting session manager.</returns>
        private MeetingSessionManager CreateSessionManager()
        {
            // Create new instances of services for this session using DI
            var ktorService = _serviceProvider.GetRequiredService<KtorService>();
            var deepgramService = _serviceProvider.GetRequiredService<DeepgramService>();
            var graphCallService = _serviceProvider.GetRequiredService<GraphCallService>();
            var logger = _serviceProvider.GetRequiredService<ILogger<MeetingSessionManager>>();

            return new MeetingSessionManager(
                communicationsClient: Client,
                ktorService: ktorService,
                deepgramService: deepgramService,
                graphCallService: graphCallService,
                logger: logger);
        }

        /// <summary>
        /// Removes a session from the active sessions dictionary.
        /// Called when a session is disposed.
        /// </summary>
        /// <param name="callId">The call ID of the session to remove.</param>
        public void RemoveSession(string callId)
        {
            if (_activeSessions.TryRemove(key: callId, value: out var session))
            {
                _logger.LogInformation(
                    message: "Session removed. CallId: {CallId}, Remaining sessions: {Count}",
                    callId,
                    _activeSessions.Count);

                session.Dispose();
            }
        }

        /// <summary>
        /// Gets the count of currently active meeting sessions.
        /// </summary>
        /// <returns>Number of active sessions.</returns>
        public int GetActiveSessionCount()
        {
            return _activeSessions.Count;
        }
    }

    /// <summary>
    /// Authentication provider for Microsoft Graph Communications using client credentials flow.
    /// </summary>
    public class ClientCredentialAuthProvider : IRequestAuthenticationProvider
    {
        private readonly IConfidentialClientApplication _msalClient;
        private readonly ILogger _logger;
        private static readonly string[] _scopes = ["https://graph.microsoft.com/.default"];

        public ClientCredentialAuthProvider(
            IConfidentialClientApplication msalClient,
            ILogger logger)
        {
            _msalClient = msalClient ?? throw new ArgumentNullException(nameof(msalClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Authenticates outbound requests to Microsoft Graph.
        /// </summary>
        /// <param name="request">The HTTP request to authenticate.</param>
        /// <param name="tenantId">The tenant ID (not used in client credentials flow).</param>
        public async Task AuthenticateOutboundRequestAsync(
            HttpRequestMessage request,
            string tenantId)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            try
            {
                var result = await _msalClient
                    .AcquireTokenForClient(scopes: _scopes)
                    .ExecuteAsync();

                request.Headers.Authorization = new AuthenticationHeaderValue(
                    scheme: "Bearer",
                    parameter: result.AccessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, message: "Failed to acquire access token.");
                throw;
            }
        }

        /// <summary>
        /// Validates inbound requests from Microsoft Teams.
        /// Note: In production, implement proper JWT validation.
        /// </summary>
        /// <param name="request">The HTTP request to validate.</param>
        /// <returns>Validation result.</returns>
        public Task<RequestValidationResult> ValidateInboundRequestAsync(HttpRequestMessage request)
        {
            // TODO: Implement proper request validation
            // For now, we trust all inbound requests
            // In production, validate the JWT token from Teams
            return Task.FromResult(new RequestValidationResult { IsValid = true });
        }
    }
}
