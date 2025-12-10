namespace TeamsMediaBot.Configuration
{
    /// <summary>
    /// Strongly-typed configuration for Azure AD authentication.
    /// </summary>
    public class AzureAdConfiguration
    {
        /// <summary>
        /// The Azure AD tenant ID.
        /// </summary>
        public required string TenantId { get; set; }

        /// <summary>
        /// The application (client) ID from Azure AD.
        /// </summary>
        public required string ClientId { get; set; }

        /// <summary>
        /// The client secret from Azure AD.
        /// </summary>
        public required string ClientSecret { get; set; }
    }

    /// <summary>
    /// Strongly-typed configuration for the bot settings.
    /// </summary>
    public class BotConfiguration
    {
        /// <summary>
        /// The public base URL of the bot (e.g., ngrok URL).
        /// </summary>
        public required string BotBaseUrl { get; set; }

        /// <summary>
        /// The public IP address of the bot for media platform.
        /// </summary>
        public required string PublicIpAddress { get; set; }

        /// <summary>
        /// The internal port for media traffic.
        /// </summary>
        public int InstanceInternalPort { get; set; } = 8445;

        /// <summary>
        /// The public port for media traffic.
        /// </summary>
        public int InstancePublicPort { get; set; } = 8445;

        /// <summary>
        /// The certificate thumbprint for secure media connection.
        /// </summary>
        public required string CertificateThumbprint { get; set; }

        /// <summary>
        /// The service FQDN (can be the ngrok domain).
        /// </summary>
        public string ServiceFqdn { get; set; } = "bot.yourdomain.com";
    }

    /// <summary>
    /// Strongly-typed configuration for Ktor server communication.
    /// </summary>
    public class KtorConfiguration
    {
        /// <summary>
        /// The base HTTP URL for Ktor API calls.
        /// </summary>
        public required string ApiBaseUrl { get; set; }

        /// <summary>
        /// The WebSocket URL for real-time communication with Ktor.
        /// </summary>
        public required string WebSocketUrl { get; set; }

        /// <summary>
        /// Endpoint for session initialization.
        /// </summary>
        public string SessionInitEndpoint { get; set; } = "/api/sessions/init";

        /// <summary>
        /// Server-to-server authentication token for Ktor API calls.
        /// </summary>
        public required string S2SToken { get; set; }
    }

    /// <summary>
    /// Strongly-typed configuration for Deepgram API.
    /// </summary>
    public class DeepgramConfiguration
    {
        /// <summary>
        /// The Deepgram API key.
        /// </summary>
        public required string ApiKey { get; set; }

        /// <summary>
        /// The Deepgram model to use for transcription.
        /// </summary>
        public string Model { get; set; } = "nova-2";

        /// <summary>
        /// The language for transcription.
        /// </summary>
        public string Language { get; set; } = "en-US";
    }
}
