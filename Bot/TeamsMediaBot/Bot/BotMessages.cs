namespace TeamsMediaBot.Bot
{
    /// <summary>
    /// Contains standardized bot message templates for consistent communication.
    /// </summary>
    public static class BotMessages
    {
        /// <summary>
        /// Welcome message shown on bot installation and in response to greeting commands.
        /// </summary>
        public const string WelcomeMessage = @"👋 Hello! I'm *Surya AI Bot*.

I can join Teams meetings and provide live transcriptions with speaker identification.

_Quick Start:_
• Type `/join <meeting_url>` to join a meeting
• Type `/help` to see all commands

Let me know if you need any assistance!";

        /// <summary>
        /// Usage message shown when join command is called without a meeting URL.
        /// </summary>
        public const string JoinUsageMessage = @"⚠️ Please provide a meeting URL.

**Usage:** `/join <meeting_url>`

**Example:**
`/join https://teams.microsoft.com/l/meetup-join/...`";
    }
}
