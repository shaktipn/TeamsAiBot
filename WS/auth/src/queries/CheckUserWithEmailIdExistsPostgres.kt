package com.suryadigital.teamsaibot.auth.queries

import com.suryadigital.teamsaibot.jooq.tables.references.User
import org.jooq.DSLContext

internal class CheckUserWithEmailIdExistsPostgres : CheckUserWithEmailIdExists() {
    override fun implementation(
        ctx: DSLContext,
        input: Input,
    ): Result {
        val exists =
            ctx.fetchExists(
                ctx
                    .selectFrom(User)
                    .where(User.email.eq(input.emailId)),
            )
        return Result(
            exists = exists,
        )
    }
}
