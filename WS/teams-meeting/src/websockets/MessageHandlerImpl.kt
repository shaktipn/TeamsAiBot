package com.suryadigital.teamsaibot.teamsMeeting.websockets

import com.suryadigital.leo.basedb.Database
import com.suryadigital.leo.basedb.timedQuery
import com.suryadigital.leo.inlineLogger.getInlineLogger
import com.suryadigital.teamsaibot.teamsMeeting.queries.InsertIntoTranscriptChunk
import com.suryadigital.teamsaibot.teamsMeeting.websockets.dto.IncomingMessage
import com.suryadigital.teamsaibot.teamsMeeting.websockets.dto.OutgoingMessage
import com.suryadigital.teamsaibot.teamsMeeting.websockets.manager.MessageHandler
import org.koin.core.component.KoinComponent
import org.koin.core.component.inject
import java.util.UUID

/**
* Default implementation of MessageHandler.
* Routes messages to appropriate handlers and performs business logic.
*/
class MessageHandlerImpl :
    MessageHandler,
    KoinComponent {
    private val logger = getInlineLogger()
    private val database by inject<Database>()
    private val insertIntoTranscriptChunk by inject<InsertIntoTranscriptChunk>()

    override suspend fun handle(
        message: IncomingMessage,
        sessionId: UUID,
    ): OutgoingMessage? =
        when (message) {
            is IncomingMessage.Transcript ->
                handleTranscript(
                    message = message,
                    sessionId = sessionId,
                )
            is IncomingMessage.MeetingEnd ->
                handleMeetingEnd(
                    message = message,
                    sessionId = sessionId,
                )
        }

    /**
     * Handles transcript messages by storing them in the database.
     * Returns null as summaries are sent periodically by SessionManager.
     *
     * @param message The transcript message
     * @param sessionId The session UUID
     * @return null (no immediate response)
     */
    private suspend fun handleTranscript(
        message: IncomingMessage.Transcript,
        sessionId: UUID,
    ): OutgoingMessage? {
        logger.info {
            "Received transcript for session=$sessionId, isFinal=${message.isFinal}, text=${message.text}"
        }
        if (message.isFinal) {
            database.timedQuery { ctx ->
                insertIntoTranscriptChunk.execute(
                    ctx = ctx,
                    input =
                        InsertIntoTranscriptChunk.Input(
                            transcript = message.text,
                            sessionId = UUID.fromString(message.sessionId),
                        ),
                )
            }
        }
        return null
    }

    /**
     * Handles meeting end messages by performing cleanup.
     *
     * @param message The meeting end message
     * @param sessionId The session UUID
     * @return null (no immediate response)
     */
    private suspend fun handleMeetingEnd(
        message: IncomingMessage.MeetingEnd,
        sessionId: UUID,
    ): OutgoingMessage? {
        logger.info {
            "Meeting ended for session=$sessionId, reason=${message.reason}"
        }
        // TODO: Mark session as finished.
        return null
    }
}
