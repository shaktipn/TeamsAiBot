using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using TeamsMediaBot.Models;

namespace TeamsMediaBot.Utilities
{
    /// <summary>
    /// Parses Teams meeting URLs to extract ChatInfo and MeetingInfo required for joining.
    /// Based on Microsoft Graph Comms Samples:
    /// https://github.com/microsoftgraph/microsoft-graph-comms-samples/blob/master/Samples/Common/Sample.Common/Meetings/JoinInfo.cs
    /// </summary>
    public static class JoinUrlParser
    {
        // Regex pattern for Teams meeting join URLs
        private static readonly Regex JoinUrlRegex = new Regex(
            @"https://teams\.microsoft\.com/l/meetup-join/([^/]+)/([^?]+)\?context=(.+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Parses a Teams meeting URL and extracts join information.
        /// </summary>
        /// <param name="meetingUrl">The Teams meeting join URL</param>
        /// <returns>Parsed meeting join information</returns>
        /// <exception cref="ArgumentException">Thrown when URL format is invalid</exception>
        public static JoinMeetingInfo ParseJoinUrl(string meetingUrl)
        {
            if (string.IsNullOrWhiteSpace(meetingUrl))
            {
                throw new ArgumentException("Meeting URL cannot be null or empty", nameof(meetingUrl));
            }

            // Decode the URL first
            var decodedUrl = HttpUtility.UrlDecode(meetingUrl);

            // Match the URL pattern
            var match = JoinUrlRegex.Match(decodedUrl);
            if (!match.Success)
            {
                throw new ArgumentException(
                    $"Invalid Teams meeting URL format. Expected format: " +
                    $"https://teams.microsoft.com/l/meetup-join/...",
                    nameof(meetingUrl));
            }

            // Extract thread ID and message ID from URL path
            var threadId = match.Groups[1].Value;
            var messageId = match.Groups[2].Value;

            // Validate Thread ID and Message ID
            if (string.IsNullOrWhiteSpace(threadId))
            {
                throw new ArgumentException("Meeting URL must contain Thread ID");
            }
            if (string.IsNullOrWhiteSpace(messageId))
            {
                throw new ArgumentException("Meeting URL must contain Message ID");
            }

            // Extract and decode context parameter
            var contextParam = match.Groups[3].Value;
            var context = DecodeContext(contextParam);

            return new JoinMeetingInfo
            {
                ThreadId = threadId,
                MessageId = messageId,
                TenantId = context.Tid,
                OrganizerId = context.Oid,
                ReplyChainMessageId = context.MessageId,
            };
        }

        /// <summary>
        /// Decodes the context parameter from the meeting URL.
        /// The context is typically a JSON object encoded in the query string.
        /// </summary>
        private static JoinContext DecodeContext(string contextParam)
        {
            JoinContext context;

            try
            {
                // The context parameter might be URL-encoded JSON
                var decodedContext = HttpUtility.UrlDecode(contextParam);

                // Try to parse as JSON
                var jsonDoc = JsonDocument.Parse(decodedContext);
                var root = jsonDoc.RootElement;

                context = new JoinContext
                {
                    Tid = root.TryGetProperty("Tid", out var tid) ? tid.GetString() ?? "" : "",
                    Oid = root.TryGetProperty("Oid", out var oid) ? oid.GetString() ?? "" : "",
                    MessageId = root.TryGetProperty("MessageId", out var msgId) ? msgId.GetString() ?? "" : ""
                };
            }
            catch (JsonException)
            {
                // If JSON parsing fails, try to parse as query string
                var queryParams = ParseQueryString(contextParam);

                context = new JoinContext
                {
                    Tid = queryParams.GetValueOrDefault("Tid", ""),
                    Oid = queryParams.GetValueOrDefault("Oid", ""),
                    MessageId = queryParams.GetValueOrDefault("MessageId", "")
                };
            }

            // Validate all required fields are present
            if (string.IsNullOrWhiteSpace(context.Tid))
            {
                throw new ArgumentException("Meeting URL must contain Tenant ID (Tid) in context parameter");
            }
            if (string.IsNullOrWhiteSpace(context.Oid))
            {
                throw new ArgumentException("Meeting URL must contain Organizer ID (Oid) in context parameter");
            }

            return context;
        }

        /// <summary>
        /// Parses a query string into a dictionary of key-value pairs.
        /// </summary>
        private static Dictionary<string, string> ParseQueryString(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(query))
            {
                return result;
            }

            // Remove leading '?' if present
            if (query.StartsWith("?"))
            {
                query = query.Substring(1);
            }

            // Split by '&' to get key-value pairs
            var pairs = query.Split('&');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split('=', 2);
                if (keyValue.Length == 2)
                {
                    var key = HttpUtility.UrlDecode(keyValue[0]);
                    var value = HttpUtility.UrlDecode(keyValue[1]);
                    result[key] = value;
                }
            }

            return result;
        }
    }
}
