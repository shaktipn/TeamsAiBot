package com.suryadigital.teamsaibot.teamsMeeting.queries

import com.suryadigital.leo.basedb.QueryInput
import com.suryadigital.leo.basedb.QueryResult
import com.suryadigital.leo.basedb.SingleResultOrNullQuery
import com.suryadigital.teamsaibot.teamsMeeting.queries.GetLatestMeetingSummary.Input
import com.suryadigital.teamsaibot.teamsMeeting.queries.GetLatestMeetingSummary.Result
import java.util.UUID

internal abstract class GetLatestMeetingSummary : SingleResultOrNullQuery<Input, Result>() {
    data class Input(
        val sessionId: UUID,
    ) : QueryInput

    data class Result(
        val summary: String,
    ) : QueryResult
}
