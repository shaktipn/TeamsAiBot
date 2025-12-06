package com.suryadigital.teamsaibot.teamsMeeting.websockets.manager

import java.util.UUID

/**
 * Service responsible for managing periodic summary generation jobs for active meeting sessions.
 *
 * Lifecycle:
 * - Start job when FIRST client connects to a session
 * - Stop job when LAST client disconnects from a session
 */
interface SummaryGenerationService {
    /**
     * Starts periodic summary generation for a session.
     * Should be called when the FIRST client connects.
     *
     * @param sessionId The unique identifier for the meeting session
     */
    suspend fun startSummaryGeneration(sessionId: UUID)

    /**
     * Stops periodic summary generation for a session.
     * Should be called when the LAST client disconnects.
     *
     * @param sessionId The unique identifier for the meeting session
     */
    suspend fun stopSummaryGeneration(sessionId: UUID)

    /**
     * Checks if summary generation is currently active for a session.
     *
     * @param sessionId The unique identifier for the meeting session
     * @return true if generation is active, false otherwise
     */
    fun isGenerationActive(sessionId: UUID): Boolean
}
