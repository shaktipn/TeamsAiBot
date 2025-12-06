package com.suryadigital.teamsaibot.auth.queries

import com.suryadigital.leo.basedb.getNonNullValue
import com.suryadigital.teamsaibot.jooq.tables.references.User
import com.suryadigital.teamsaibot.jooq.tables.references.WT
import org.jooq.DSLContext

internal class GetUserAndWTDetailsByWTPostgres : GetUserAndWTDetailsByWT() {
    override fun implementation(
        ctx: DSLContext,
        input: Input,
    ): Result? =
        ctx
            .select(User.id, User.isActive, WT.expiresAt)
            .from(User)
            .join(WT)
            .on(User.id.eq(WT.userId))
            .where(WT.isDeleted.isFalse)
            .and(WT.wt.eq(input.wt))
            .fetchOne()
            ?.let { record ->
                Result(
                    userId = record.getNonNullValue(User.id),
                    userIsActive = record.getNonNullValue(User.isActive),
                    wtExpiresAt = record.getNonNullValue(WT.expiresAt),
                )
            }
}
