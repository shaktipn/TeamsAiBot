namespace TeamsMediaBot.Models
{
    /// <summary>
    /// Parsed information from a Teams meeting URL required to join a meeting.
    /// </summary>
    public record JoinMeetingInfo
    {
        public required ChatInfo ChatInfo { get; init; }
        public required MeetingInfo MeetingInfo { get; init; }
    }

    /// <summary>
    /// Chat information extracted from meeting URL.
    /// </summary>
    public record ChatInfo
    {
        /// <summary>
        /// The Teams thread ID for the meeting.
        /// </summary>
        public required string ThreadId { get; init; }

        /// <summary>
        /// The message ID within the thread.
        /// </summary>
        public required string MessageId { get; init; }

        /// <summary>
        /// Optional reply chain message ID.
        /// </summary>
        public string? ReplyChainMessageId { get; init; }
    }

    /// <summary>
    /// Meeting information for Graph API call.
    /// </summary>
    public record MeetingInfo
    {
        /// <summary>
        /// Whether the bot can join without the meeting organizer present.
        /// </summary>
        public string? AllowConversationWithoutHost { get; init; }
    }

    /// <summary>
    /// Context information parsed from meeting URL query parameters.
    /// Used internally for parsing meeting URLs.
    /// </summary>
    public record JoinContext
    {
        /// <summary>
        /// Tenant ID (Azure AD tenant).
        /// </summary>
        public required string Tid { get; init; }

        /// <summary>
        /// Object ID of the meeting organizer.
        /// </summary>
        public required string Oid { get; init; }

        /// <summary>
        /// Message ID from the meeting invitation.
        /// </summary>
        public required string MessageId { get; init; }
    }
}
