package com.suryadigital.teamsaibot.auth.queries

import com.suryadigital.leo.basedb.QueryInput
import com.suryadigital.leo.basedb.QueryResult
import com.suryadigital.leo.basedb.SingleResultQuery
import com.suryadigital.teamsaibot.auth.queries.CheckUserWithEmailIdExists.Input
import com.suryadigital.teamsaibot.auth.queries.CheckUserWithEmailIdExists.Result

internal abstract class CheckUserWithEmailIdExists : SingleResultQuery<Input, Result>() {
    data class Input(
        val emailId: String,
    ) : QueryInput

    data class Result(
        val exists: Boolean,
    ) : QueryResult
}
