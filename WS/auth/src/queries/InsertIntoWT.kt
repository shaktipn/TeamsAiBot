package com.suryadigital.teamsaibot.auth.queries

import com.suryadigital.leo.basedb.NoResultQuery
import com.suryadigital.leo.basedb.QueryInput
import com.suryadigital.teamsaibot.auth.queries.InsertIntoWT.Input
import java.time.Instant
import java.util.UUID

internal abstract class InsertIntoWT : NoResultQuery<Input>() {
    data class Input(
        val wt: String,
        val userId: UUID,
        val expiresAt: Instant,
    ) : QueryInput
}
