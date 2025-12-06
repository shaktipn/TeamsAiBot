package com.suryadigital.teamsaibot.auth.queries

import com.suryadigital.leo.basedb.QueryInput
import com.suryadigital.leo.basedb.QueryResult
import com.suryadigital.leo.basedb.SingleResultOrNullQuery
import com.suryadigital.teamsaibot.auth.queries.GetUserAndWTDetailsByWT.Input
import com.suryadigital.teamsaibot.auth.queries.GetUserAndWTDetailsByWT.Result
import java.time.Instant
import java.util.UUID

internal abstract class GetUserAndWTDetailsByWT : SingleResultOrNullQuery<Input, Result>() {
    data class Input(
        val wt: String,
    ) : QueryInput

    data class Result(
        val userId: UUID,
        val userIsActive: Boolean,
        val wtExpiresAt: Instant,
    ) : QueryResult
}
