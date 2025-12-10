using TeamsMediaBot.Models;
using TeamsMediaBot.Utilities;

namespace TeamsMediaBot.Services
{
    /// <summary>
    /// Orchestrates the process of joining a Teams meeting via command.
    /// Parses meeting URLs and coordinates with BotMediaService to join.
    /// </summary>
    public class MeetingJoinService
    {
        private readonly BotMediaService _botMediaService;
        private readonly ILogger<MeetingJoinService> _logger;

        public MeetingJoinService(
            BotMediaService botMediaService,
            ILogger<MeetingJoinService> logger)
        {
            _botMediaService = botMediaService ?? throw new ArgumentNullException(nameof(botMediaService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Joins a Teams meeting using the provided meeting URL.
        /// </summary>
        /// <param name="meetingUrl">The Teams meeting join URL</param>
        /// <param name="tenantId">The Azure AD tenant ID</param>
        /// <returns>Result indicating success or failure</returns>
        public async Task<JoinResult> JoinMeetingAsync(string meetingUrl, string tenantId)
        {
            try
            {
                _logger.LogInformation("Attempting to join meeting. URL: {Url}, TenantId: {TenantId}",
                    meetingUrl, tenantId);

                // Step 1: Parse the meeting URL
                JoinMeetingInfo joinInfo;
                try
                {
                    joinInfo = JoinUrlParser.ParseJoinUrl(meetingUrl);
                    _logger.LogInformation("Successfully parsed meeting URL. ThreadId: {ThreadId}",
                        joinInfo.ChatInfo.ThreadId);
                }
                catch (ArgumentException ex)
                {
                    _logger.LogError(ex, "Failed to parse meeting URL");
                    return new JoinResult
                    {
                        Success = false,
                        Error = $"Invalid meeting URL format: {ex.Message}"
                    };
                }

                // Step 2: Build join parameters
                var joinParams = new JoinMeetingParameters
                {
                    ChatInfo = joinInfo.ChatInfo,
                    MeetingInfo = joinInfo.MeetingInfo,
                    TenantId = tenantId
                };

                // Step 3: Call BotMediaService to join the meeting
                var callId = await _botMediaService.JoinMeetingAsync(joinParams);

                _logger.LogInformation("Successfully joined meeting. CallId: {CallId}", callId);

                return new JoinResult
                {
                    Success = true,
                    CallId = callId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to join meeting");
                return new JoinResult
                {
                    Success = false,
                    Error = $"Failed to join meeting: {ex.Message}"
                };
            }
        }
    }

    /// <summary>
    /// Result of a meeting join operation.
    /// </summary>
    public record JoinResult
    {
        /// <summary>
        /// Whether the join operation was successful.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// The Graph call ID if successful.
        /// </summary>
        public string? CallId { get; init; }

        /// <summary>
        /// Error message if unsuccessful.
        /// </summary>
        public string? Error { get; init; }
    }

    /// <summary>
    /// Parameters required to join a Teams meeting.
    /// </summary>
    public record JoinMeetingParameters
    {
        /// <summary>
        /// Chat information extracted from the meeting URL.
        /// </summary>
        public required ChatInfo ChatInfo { get; init; }

        /// <summary>
        /// Meeting information for the Graph API call.
        /// </summary>
        public required MeetingInfo MeetingInfo { get; init; }

        /// <summary>
        /// The Azure AD tenant ID.
        /// </summary>
        public required string TenantId { get; init; }
    }
}
