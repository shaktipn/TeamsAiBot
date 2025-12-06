package com.suryadigital.teamsaibot.teamsMeeting.websockets.dto

import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable

/**
 * Sealed class representing all possible incoming WebSocket messages.
 * Each message type contains the necessary data for processing.
 */
@Serializable
sealed class IncomingMessage {
    abstract val type: String
    abstract val sessionId: String

    /**
     * Transcript message sent from the bot containing meeting transcription text.
     *
     * @property sessionId Unique identifier for the meeting session
     * @property text The transcribed text content
     * @property isFinal Whether this is a final transcription or interim result
     */
    @Serializable
    @SerialName("TRANSCRIPT")
    data class Transcript(
        override val sessionId: String,
        val text: String,
        val isFinal: Boolean,
        override val type: String = "TRANSCRIPT",
    ) : IncomingMessage()

    /**
     * Meeting end message sent when a meeting session concludes.
     *
     * @property sessionId Unique identifier for the meeting session
     * @property reason Reason for meeting termination
     */
    @Serializable
    @SerialName("MEETING_END")
    data class MeetingEnd(
        override val sessionId: String,
        val reason: String,
        override val type: String = "MEETING_END",
    ) : IncomingMessage()
}

/**
 * Sealed class representing all possible outgoing WebSocket messages.
 */
@Serializable
sealed class OutgoingMessage {
    abstract val sessionId: String

    /**
     * Live summary message sent to all clients in a session.
     * Contains an AI-generated summary of the meeting transcriptions.
     *
     * @property sessionId Unique identifier for the meeting session
     * @property summary The generated summary text
     */
    @Serializable
    @SerialName("LIVE_SUMMARY")
    data class LiveSummary(
        override val sessionId: String,
        val summary: String,
    ) : OutgoingMessage()
}
