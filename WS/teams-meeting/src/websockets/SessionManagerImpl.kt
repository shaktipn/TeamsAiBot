package com.suryadigital.teamsaibot.teamsMeeting.websockets

import com.suryadigital.leo.basedb.Database
import com.suryadigital.leo.inlineLogger.getInlineLogger
import com.suryadigital.teamsaibot.teamsMeeting.websockets.dto.OutgoingMessage
import com.suryadigital.teamsaibot.teamsMeeting.websockets.manager.SessionManager
import com.suryadigital.teamsaibot.teamsMeeting.websockets.manager.SummaryGenerationService
import io.ktor.server.websocket.WebSocketServerSession
import io.ktor.websocket.Frame
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.serialization.json.Json
import org.koin.core.component.KoinComponent
import org.koin.core.component.inject
import java.util.UUID
import java.util.concurrent.ConcurrentHashMap

/**
 * Default implementation of SessionManager.
 * Maintains a thread-safe map of sessions and their connected clients.
 */
class SessionManagerImpl :
    SessionManager,
    KoinComponent {
    private val logger = getInlineLogger(this::class)
    private val database by inject<Database>()
    private val json by inject<Json>()
    private val summaryGenerationService by inject<SummaryGenerationService>()
    private val sessions = ConcurrentHashMap<UUID, MutableSet<WebSocketServerSession>>()
    private val sessionLocks = ConcurrentHashMap<UUID, Mutex>()

    override suspend fun addClient(
        sessionId: UUID,
        connection: WebSocketServerSession,
    ) {
        val lock = sessionLocks.computeIfAbsent(sessionId) { Mutex() }
        lock.withLock {
            val clients = sessions.computeIfAbsent(sessionId) { mutableSetOf() }
            val wasEmpty = clients.isEmpty()
            clients.add(connection)

            logger.info {
                "Client added to session=$sessionId. Total clients: ${clients.size}"
            }

            // Start summary generation when FIRST client connects
            if (wasEmpty) {
                logger.info { "First client connected to session=$sessionId, starting summary generation" }
                summaryGenerationService.startSummaryGeneration(sessionId = sessionId)
            }
        }
    }

    override suspend fun removeClient(
        sessionId: UUID,
        connection: WebSocketServerSession,
    ) {
        val lock = sessionLocks[sessionId] ?: return
        lock.withLock {
            val clients = sessions[sessionId] ?: return@withLock
            clients.remove(connection)
            logger.info {
                "Client removed from session=$sessionId. Remaining clients: ${clients.size}"
            }
            // Clean up empty sessions
            if (clients.isEmpty()) {
                logger.info { "Last client disconnected from session=$sessionId, stopping summary generation" }
                summaryGenerationService.stopSummaryGeneration(sessionId = sessionId)

                sessions.remove(sessionId)
                sessionLocks.remove(sessionId)
                logger.info { "Session $sessionId cleaned up (no remaining clients)" }
            }
        }
    }

    override suspend fun sendToSession(
        sessionId: UUID,
        message: OutgoingMessage,
    ) {
        val clients = sessions[sessionId]
        if (clients.isNullOrEmpty()) {
            logger.warn { "Attempted to send message to session=$sessionId but no clients connected" }
            return
        }
        val messageJson = json.encodeToString(message)
        logger.info {
            "Sending $message to ${clients.size} client(s) in session=$sessionId"
        }
        clients.forEach { client ->
            try {
                client.send(Frame.Text(text = messageJson))
            } catch (e: Exception) {
                logger.error(e) {
                    "Failed to send message to client in session=$sessionId: ${e.message}"
                }
            }
        }
    }
}
