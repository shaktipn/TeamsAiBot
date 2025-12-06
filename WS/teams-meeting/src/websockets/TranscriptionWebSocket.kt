package com.suryadigital.teamsaibot.teamsMeeting.websockets

import com.suryadigital.leo.inlineLogger.getInlineLogger
import com.suryadigital.leo.testUtils.runWithKtorMetricsContext
import com.suryadigital.teamsaibot.teamsMeeting.websockets.dto.IncomingMessage
import com.suryadigital.teamsaibot.teamsMeeting.websockets.helpers.TranscriptionUtil
import com.suryadigital.teamsaibot.teamsMeeting.websockets.manager.MessageHandler
import com.suryadigital.teamsaibot.teamsMeeting.websockets.manager.SessionManager
import io.ktor.server.routing.Route
import io.ktor.server.websocket.webSocket
import io.ktor.websocket.CloseReason
import io.ktor.websocket.Frame
import io.ktor.websocket.close
import io.ktor.websocket.readText
import kotlinx.serialization.SerializationException
import kotlinx.serialization.json.Json
import org.koin.ktor.ext.inject

/**
 * Configures the transcription WebSocket endpoint.
 * Clients connect with a sessionId query parameter to join a meeting session.
 */
fun Route.transcriptionWebSocket() {
    val json by inject<Json>()
    val sessionManager by inject<SessionManager>()
    val messageHandler by inject<MessageHandler>()

    webSocket(path = "transcription") {
        val sessionId =
            TranscriptionUtil.getValidSessionId(sessionId = call.request.queryParameters["sessionId"]) ?: run {
                close(
                    reason =
                        CloseReason(
                            code = CloseReason.Codes.CANNOT_ACCEPT,
                            message = "Missing required query parameter: sessionId or its not valid / registered",
                        ),
                )
                return@webSocket
            }
        sessionManager.addClient(sessionId = sessionId, connection = this)
        logger.info { "WebSocket connected for sessionId=$sessionId" }
        try {
            for (frame in incoming) {
                if (frame !is Frame.Text) {
                    continue
                }
                val frameText = frame.readText()
                logger.debug { "Received message for sessionId=$sessionId: $frameText" }
                try {
                    val incomingMessage = json.decodeFromString<IncomingMessage>(frameText)
                    // Validate sessionId matches connection
                    if (incomingMessage.sessionId != "$sessionId") {
                        logger.warn {
                            "SessionId mismatch: connection=$sessionId, message=${incomingMessage.sessionId}"
                        }
                        continue
                    }
                    runWithKtorMetricsContext {
                        val response =
                            messageHandler.handle(
                                message = incomingMessage,
                                sessionId = sessionId,
                            )
                        if (response != null) {
                            sessionManager.sendToSession(
                                sessionId = sessionId,
                                message = response,
                            )
                        }
                    }
                } catch (e: SerializationException) {
                    logger.error(e) {
                        "Failed to parse incoming message for sessionId=$sessionId: $frameText - ${e.message}"
                    }
                }
            }
        } catch (e: Exception) {
            logger.error(e) {
                "Error in WebSocket connection for sessionId=$sessionId: ${e.message}"
            }
        } finally {
            sessionManager.removeClient(sessionId = sessionId, connection = this)
            logger.info { "WebSocket disconnected for sessionId=$sessionId" }
        }
    }
}

private val logger = getInlineLogger(name = "TranscriptionWebSocket")
