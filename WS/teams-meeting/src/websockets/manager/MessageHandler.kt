package com.suryadigital.teamsaibot.teamsMeeting.websockets.manager

import com.suryadigital.teamsaibot.teamsMeeting.websockets.dto.IncomingMessage
import com.suryadigital.teamsaibot.teamsMeeting.websockets.dto.OutgoingMessage
import java.util.UUID

/**
 * Interface for handling incoming WebSocket messages.
 * Implementations process messages and optionally return responses.
 */
interface MessageHandler {
    /**
     * Handles an incoming message and optionally returns an outgoing message.
     * The implementation routes to appropriate logic based on message type.
     *
     * @param message The incoming message to process
     * @param sessionId The UUID of the meeting session
     * @return An optional outgoing message to send back, or null if no immediate response is needed
     */
    suspend fun handle(
        message: IncomingMessage,
        sessionId: UUID,
    ): OutgoingMessage?
}
