package com.suryadigital.teamsaibot.teamsMeeting.queries

import com.suryadigital.leo.basedb.QueryInput
import com.suryadigital.leo.basedb.QueryResult
import com.suryadigital.leo.basedb.SingleResultOrNullQuery
import com.suryadigital.teamsaibot.teamsMeeting.queries.GetSessionDetailsBySessionId.Input
import com.suryadigital.teamsaibot.teamsMeeting.queries.GetSessionDetailsBySessionId.Result
import com.suryadigital.teamsaibot.teamsMeeting.utils.MeetingSessionStatus
import java.util.UUID

internal abstract class GetSessionDetailsBySessionId : SingleResultOrNullQuery<Input, Result>() {
    data class Input(
        val sessionId: UUID,
    ) : QueryInput

    data class Result(
        val sessionId: UUID,
        val sessionStatus: MeetingSessionStatus,
    ) : QueryResult
}
