package com.suryadigital.teamsaibot.teamsMeeting.queries

import com.suryadigital.leo.basedb.NoResultQuery
import com.suryadigital.leo.basedb.QueryInput
import com.suryadigital.teamsaibot.teamsMeeting.queries.InsertIntoTranscriptChunk.Input
import java.util.UUID

internal abstract class InsertIntoTranscriptChunk : NoResultQuery<Input>() {
    data class Input(
        val transcript: String,
        val sessionId: UUID,
    ) : QueryInput
}
