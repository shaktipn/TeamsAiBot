package com.suryadigital.teamsaibot.auth.queries

import com.suryadigital.leo.basedb.DBException
import com.suryadigital.teamsaibot.jooq.tables.references.Password
import org.jooq.DSLContext

internal class InsertIntoPasswordPostgres : InsertIntoPassword() {
    override fun implementation(
        ctx: DSLContext,
        input: Input,
    ) {
        ctx
            .insertInto(
                Password,
                Password.password,
                Password.userId,
            ).values(
                input.hashedPassword,
                input.userId,
            ).execute()
            .let { updatedRowCount ->
                if (updatedRowCount != 1) {
                    throw DBException("InsertIntoPassword failed")
                }
            }
    }
}
