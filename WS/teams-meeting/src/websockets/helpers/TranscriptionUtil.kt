package com.suryadigital.teamsaibot.teamsMeeting.websockets.helpers

import com.suryadigital.leo.basedb.Database
import com.suryadigital.leo.basedb.timedQuery
import com.suryadigital.leo.testUtils.runWithKtorMetricsContext
import com.suryadigital.teamsaibot.teamsMeeting.queries.GetSessionDetailsBySessionId
import com.suryadigital.teamsaibot.teamsMeeting.utils.MeetingSessionStatus
import org.koin.core.component.KoinComponent
import org.koin.core.component.inject
import java.util.UUID

internal object TranscriptionUtil : KoinComponent {
    private val getSessionDetailsBySessionId by inject<GetSessionDetailsBySessionId>()
    private val database by inject<Database>()

    suspend fun getValidSessionId(sessionId: String?): UUID? {
        return runWithKtorMetricsContext {
            val validUUID =
                sessionId?.let { runCatching { UUID.fromString(it) }.getOrNull() }
                    ?: return@runWithKtorMetricsContext null

            val session =
                database.timedQuery(isReadOnly = true) { ctx ->
                    getSessionDetailsBySessionId.execute(
                        ctx = ctx,
                        input = GetSessionDetailsBySessionId.Input(sessionId = validUUID),
                    )
                } ?: return@runWithKtorMetricsContext null

            return@runWithKtorMetricsContext when (session.sessionStatus) {
                MeetingSessionStatus.ACTIVE,
                MeetingSessionStatus.REGISTERED,
                -> session.sessionId
                else -> null
            }
        }
    }

    fun getStructuredAiInput(
        transcriptChunks: List<String>,
        previousSummary: String,
    ): String =
        buildString {
            transcriptChunks.forEachIndexed { index, chunk ->
                appendLine(value = "$index - $chunk")
            }
            appendLine()
            appendLine(value = "Previous Summary:")
            appendLine(previousSummary)
        }
}
