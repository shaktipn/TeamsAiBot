namespace TeamsMediaBot.Models
{
    /// <summary>
    /// Parsed information from a Teams meeting URL required to join a meeting.
    /// </summary>
    public record JoinMeetingInfo
    {
        /// <summary>
        /// The unique identifier of the meeting thread.
        /// </summary>
        public required string ThreadId { get; init; }

        /// <summary>
        /// The unique identifier of the meeting message.
        /// </summary>
        public required string MessageId { get; init; }

        /// <summary>
        /// The Azure Active Directory tenant identifier associated with the meeting.
        /// </summary>
        public required string TenantId { get; init; }

        /// <summary>
        /// The identifier of the meeting organizer.
        /// </summary>
        public required string OrganizerId { get; init; }

        /// <summary>
        /// The identifier of the reply chain message, when present. May be null.
        /// </summary>
        public required string? ReplyChainMessageId { get; init; }
    }

    /// <summary>
    /// Context information parsed from meeting URL query parameters.
    /// Contains tenant and organizer information required for meeting join.
    /// All fields are required and must be present in the meeting URL.
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

        /// <summary>
        /// Convenience property for accessing Tenant ID.
        /// </summary>
        public string TenantId => Tid;

        /// <summary>
        /// Convenience property for accessing Organizer ID.
        /// </summary>
        public string OrganizerId => Oid;
    }
}
