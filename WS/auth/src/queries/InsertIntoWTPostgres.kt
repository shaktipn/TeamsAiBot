package com.suryadigital.teamsaibot.auth.queries

import com.suryadigital.leo.basedb.DBException
import com.suryadigital.teamsaibot.jooq.tables.references.WT
import org.jooq.DSLContext

internal class InsertIntoWTPostgres : InsertIntoWT() {
    override fun implementation(
        ctx: DSLContext,
        input: Input,
    ) {
        ctx
            .insertInto(
                WT,
                WT.wt,
                WT.userId,
                WT.expiresAt,
            ).values(
                input.wt,
                input.userId,
                input.expiresAt,
            ).execute()
            .let { updatedRowCount ->
                if (updatedRowCount != 1) {
                    throw DBException("InsertIntoWT failed")
                }
            }
    }
}
