namespace TeamsMediaBot.Services
{
    /// <summary>
    /// Parses user messages into structured command objects.
    /// Supports both mention-based and slash command formats.
    /// </summary>
    public class CommandParser
    {
        /// <summary>
        /// Parses a text message into a typed command.
        /// </summary>
        /// <param name="text">The cleaned command text (mentions already stripped)</param>
        /// <returns>A typed command object</returns>
        public Command Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new UnknownCommand();
            }

            // Clean and normalize the text
            var cleanText = text.Trim();

            // Remove leading slash if present (slash command format)
            if (cleanText.StartsWith("/"))
            {
                cleanText = cleanText.Substring(1).Trim();
            }

            // Parse join command
            if (cleanText.StartsWith("join", StringComparison.OrdinalIgnoreCase))
            {
                return ParseJoinCommand(cleanText);
            }

            // Parse help command
            if (cleanText.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                return new HelpCommand();
            }

            // Parse status command
            if (cleanText.Equals("status", StringComparison.OrdinalIgnoreCase))
            {
                return new StatusCommand();
            }

            // Parse greeting (only works with mentions, not slash commands)
            if (IsGreeting(cleanText))
            {
                return new GreetingCommand();
            }

            // Unknown command
            return new UnknownCommand();
        }

        /// <summary>
        /// Parses a join command to extract the meeting URL.
        /// </summary>
        private JoinCommand ParseJoinCommand(string text)
        {
            // Expected format: "join <meeting_url>"
            var parts = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 2)
            {
                // Everything after "join" is considered the URL
                var url = string.Join(" ", parts.Skip(1));
                return new JoinCommand(url.Trim());
            }

            // Invalid format - no URL provided
            return new JoinCommand(null);
        }

        /// <summary>
        /// Checks if the text is a greeting.
        /// </summary>
        private bool IsGreeting(string text)
        {
            var greetings = new[] { "hi", "hello", "hey", "howdy", "greetings" };
            var lowerText = text.ToLowerInvariant();

            return greetings.Any(g => lowerText == g || lowerText.StartsWith(g + " "));
        }
    }

    /// <summary>
    /// Base class for all commands.
    /// </summary>
    public abstract record Command;

    /// <summary>
    /// Command to join a Teams meeting.
    /// </summary>
    /// <param name="MeetingUrl">The Teams meeting URL to join</param>
    public record JoinCommand(string? MeetingUrl) : Command;

    /// <summary>
    /// Command to show help information.
    /// </summary>
    public record HelpCommand : Command;

    /// <summary>
    /// Command to show current status.
    /// </summary>
    public record StatusCommand : Command;

    /// <summary>
    /// Command for greetings.
    /// </summary>
    public record GreetingCommand : Command;

    /// <summary>
    /// Command for unknown/unrecognized input.
    /// </summary>
    public record UnknownCommand : Command;
}
