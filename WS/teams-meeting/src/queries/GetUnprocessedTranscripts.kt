package com.suryadigital.teamsaibot.teamsMeeting.queries

import com.suryadigital.leo.basedb.IterableResultQuery
import com.suryadigital.leo.basedb.QueryInput
import com.suryadigital.leo.basedb.QueryResult
import com.suryadigital.teamsaibot.teamsMeeting.queries.GetUnprocessedTranscripts.Input
import com.suryadigital.teamsaibot.teamsMeeting.queries.GetUnprocessedTranscripts.Result
import java.time.Instant
import java.util.UUID

internal abstract class GetUnprocessedTranscripts : IterableResultQuery<Input, Result>() {
    data class Input(
        val sessionId: UUID,
    ) : QueryInput

    data class Result(
        val id: UUID,
        val text: String,
        val speakerName: String?,
        val createdAt: Instant,
    ) : QueryResult
}
