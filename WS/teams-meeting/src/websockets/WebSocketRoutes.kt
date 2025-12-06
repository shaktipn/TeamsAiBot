package com.suryadigital.teamsaibot.teamsMeeting.websockets

import io.ktor.server.application.Application
import io.ktor.server.routing.route
import io.ktor.server.routing.routing

/**
 * Configures all WebSocket routes for the application.
 * Creates instances of required dependencies and registers endpoints.
 */
fun Application.webSocketRoutes() {
    routing {
        route(path = "ws") {
            // Register transcription WebSocket endpoint
            // Accessible at: ws://host/ws/transcription?sessionId=<uuid>
            transcriptionWebSocket()

            // Legacy endpoints (can be removed if not needed)
            getSummary()
        }
    }
}
