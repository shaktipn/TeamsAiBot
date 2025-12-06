package com.suryadigital.teamsaibot.teamsMeeting.websockets

import com.suryadigital.leo.basedb.Database
import com.suryadigital.leo.basedb.timedQuery
import com.suryadigital.leo.inlineLogger.getInlineLogger
import com.suryadigital.teamsaibot.ai.AiService
import com.suryadigital.teamsaibot.teamsMeeting.queries.GetUnprocessedTranscripts
import com.suryadigital.teamsaibot.teamsMeeting.queries.InsertIntoMeetingSummary
import com.suryadigital.teamsaibot.teamsMeeting.queries.MarkTranscriptsAsProcessed
import com.suryadigital.teamsaibot.teamsMeeting.websockets.dto.OutgoingMessage
import com.suryadigital.teamsaibot.teamsMeeting.websockets.manager.SessionManager
import com.suryadigital.teamsaibot.teamsMeeting.websockets.manager.SummaryGenerationService
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import org.koin.core.component.KoinComponent
import org.koin.core.component.inject
import java.util.UUID
import java.util.concurrent.ConcurrentHashMap
import kotlin.time.Duration.Companion.seconds

/**
 * Default implementation of SummaryGenerationService.
 *
 * Manages per-session coroutine jobs that periodically:
 * 1. Fetch unprocessed transcript chunks
 * 2. Generate summary using AI service
 * 3. Send summary to all session clients
 * 4. Mark transcripts as processed
 */
internal class SummaryGenerationServiceImpl(
    private val applicationScope: CoroutineScope,
) : SummaryGenerationService,
    KoinComponent {
    private val logger = getInlineLogger(this::class)
    private val database by inject<Database>()
    private val aiService by inject<AiService>()
    private val sessionManager by inject<SessionManager>()
    private val getUnprocessedTranscripts by inject<GetUnprocessedTranscripts>()
    private val markTranscriptsAsProcessed by inject<MarkTranscriptsAsProcessed>()
    private val insertIntoMeetingSummary by inject<InsertIntoMeetingSummary>()

    private val activeJobs = ConcurrentHashMap<UUID, Job>()

    companion object {
        private val SUMMARY_GENERATION_INTERVAL = 30.seconds
    }

    override suspend fun startSummaryGeneration(sessionId: UUID) {
        // Check if already running
        if (activeJobs.containsKey(sessionId)) {
            logger.warn { "Summary generation already active for session=$sessionId" }
            return
        }
        logger.info { "Starting summary generation for session=$sessionId" }
        val job =
            applicationScope.launch {
                try {
                    while (isActive) {
                        try {
                            generateAndSendSummary(sessionId = sessionId)
                        } catch (e: Exception) {
                            logger.error(e) {
                                "Error generating summary for session=$sessionId: ${e.message}"
                            }
                            // Continue running despite errors in individual iterations
                        }
                        delay(duration = SUMMARY_GENERATION_INTERVAL)
                    }
                } finally {
                    logger.info { "Summary generation stopped for session=$sessionId" }
                }
            }

        activeJobs[sessionId] = job
    }

    override suspend fun stopSummaryGeneration(sessionId: UUID) {
        val job = activeJobs.remove(sessionId)
        if (job == null) {
            logger.warn { "No active summary generation for session=$sessionId" }
            return
        }
        logger.info { "Stopping summary generation for session=$sessionId" }
        job.cancel()
    }

    override fun isGenerationActive(sessionId: UUID): Boolean = activeJobs[sessionId]?.isActive ?: false

    /**
     * Core logic: fetch unprocessed transcripts, generate summary, send to clients, mark processed.
     */
    private suspend fun generateAndSendSummary(sessionId: UUID) {
        val transcripts =
            database.timedQuery(isReadOnly = true) { ctx ->
                getUnprocessedTranscripts
                    .execute(
                        ctx = ctx,
                        input = GetUnprocessedTranscripts.Input(sessionId = sessionId),
                    ).toList()
            }
        if (transcripts.isEmpty()) {
            logger.debug { "No unprocessed transcripts for session=$sessionId" }
            return
        }
        logger.info { "Processing ${transcripts.size} unprocessed transcripts for session=$sessionId" }
        // Step 3: Concatenate transcript texts
        val concatenatedText = transcripts.joinToString(separator = "\n", transform = GetUnprocessedTranscripts.Result::text)
        val summary =
            try {
                aiService.getText(input = concatenatedText)
            } catch (e: Exception) {
                logger.error(e) { "AI service failed for session=$sessionId: ${e.message}" }
                throw e
            }

        try {
            sessionManager.sendToSession(
                sessionId = sessionId,
                message =
                    OutgoingMessage.LiveSummary(
                        sessionId = "$sessionId",
                        summary = summary,
                    ),
            )
        } catch (e: Exception) {
            logger.error(e) { "Failed to send summary to session=$sessionId: ${e.message}" }
            // Continue to mark as processed even if send fails
        }

        // Step 6: Save summary to database
        database.timedQuery { ctx ->
            insertIntoMeetingSummary.execute(
                ctx = ctx,
                input =
                    InsertIntoMeetingSummary.Input(
                        sessionId = sessionId,
                        summary = summary,
                        createdBy = null, // System-generated
                    ),
            )
        }

        // Step 7: Mark transcripts as processed
        val transcriptIds = transcripts.map(GetUnprocessedTranscripts.Result::id)
        database.timedQuery { ctx ->
            markTranscriptsAsProcessed.execute(
                ctx = ctx,
                input = MarkTranscriptsAsProcessed.Input(transcriptIds = transcriptIds),
            )
        }
        logger.info { "Successfully generated and sent summary for session=$sessionId" }
    }
}
