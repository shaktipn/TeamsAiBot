package com.suryadigital.teamsaibot.teamsMeeting.websockets.manager

import com.suryadigital.teamsaibot.teamsMeeting.websockets.dto.OutgoingMessage
import io.ktor.server.websocket.WebSocketServerSession
import java.util.UUID

/**
 * Interface for managing WebSocket sessions and broadcasting messages.
 */
interface SessionManager {
    /**
     * Adds a client connection to a session.
     *
     * @param sessionId The unique identifier for the meeting session
     * @param connection The WebSocket connection to add
     */
    suspend fun addClient(
        sessionId: UUID,
        connection: WebSocketServerSession,
    )

    /**
     * Removes a client connection from a session.
     *
     * @param sessionId The unique identifier for the meeting session
     * @param connection The WebSocket connection to remove
     */
    suspend fun removeClient(
        sessionId: UUID,
        connection: WebSocketServerSession,
    )

    /**
     * Sends a message to all clients in a session.
     *
     * @param sessionId The unique identifier for the meeting session
     * @param message The outgoing message to send
     */
    suspend fun sendToSession(
        sessionId: UUID,
        message: OutgoingMessage,
    )
}
