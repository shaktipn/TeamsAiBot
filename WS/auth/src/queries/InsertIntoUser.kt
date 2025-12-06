package com.suryadigital.teamsaibot.auth.queries

import com.suryadigital.leo.basedb.NoResultQuery
import com.suryadigital.leo.basedb.QueryInput
import com.suryadigital.teamsaibot.auth.queries.InsertIntoUser.Input
import java.util.UUID

internal abstract class InsertIntoUser : NoResultQuery<Input>() {
    data class Input(
        val emailId: String,
        val userId: UUID,
    ) : QueryInput
}
