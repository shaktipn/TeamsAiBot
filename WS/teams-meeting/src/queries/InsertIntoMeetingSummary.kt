package com.suryadigital.teamsaibot.teamsMeeting.queries

import com.suryadigital.leo.basedb.NoResultQuery
import com.suryadigital.leo.basedb.QueryInput
import com.suryadigital.teamsaibot.teamsMeeting.queries.InsertIntoMeetingSummary.Input
import java.util.UUID

internal abstract class InsertIntoMeetingSummary : NoResultQuery<Input>() {
    data class Input(
        val sessionId: UUID,
        val summary: String,
        val createdBy: UUID?,
    ) : QueryInput
}
