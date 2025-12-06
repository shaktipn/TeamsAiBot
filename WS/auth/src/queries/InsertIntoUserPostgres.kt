package com.suryadigital.teamsaibot.auth.queries

import com.suryadigital.leo.basedb.DBException
import com.suryadigital.teamsaibot.jooq.tables.references.User
import org.jooq.DSLContext

internal class InsertIntoUserPostgres : InsertIntoUser() {
    override fun implementation(
        ctx: DSLContext,
        input: Input,
    ) {
        ctx
            .insertInto(
                User,
                User.id,
                User.email,
            ).values(
                input.userId,
                input.emailId,
            ).execute()
            .let { updatedRowCount ->
                if (updatedRowCount != 1) {
                    throw DBException("InsertIntoUser failed")
                }
            }
    }
}
