package com.suryadigital.teamsaibot.auth.queries

import com.suryadigital.leo.basedb.getNonNullValue
import com.suryadigital.teamsaibot.jooq.tables.references.Password
import com.suryadigital.teamsaibot.jooq.tables.references.User
import org.jooq.DSLContext

internal class GetUserIdAndPasswordByEmailIdPostgres : GetUserIdAndPasswordByEmailId() {
    override fun implementation(
        ctx: DSLContext,
        input: Input,
    ): Result? =
        ctx
            .select(User.id, Password.password)
            .from(User)
            .join(Password)
            .on(
                User.id
                    .eq(Password.userId)
                    .and(Password.isDeleted.isFalse)
                    .and(User.isActive.isTrue),
            ).where(User.email.eq(input.emailId))
            .fetchOne()
            ?.let { record ->
                Result(
                    userId = record.getNonNullValue(User.id),
                    hashedPassword = record.getNonNullValue(Password.password),
                )
            }
}
