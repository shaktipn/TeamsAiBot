package com.suryadigital.teamsaibot.teamsMeeting.queries

import com.suryadigital.leo.basedb.DBException
import com.suryadigital.teamsaibot.jooq.tables.references.MeetingSummary
import org.jooq.DSLContext
import java.util.UUID

internal class InsertIntoMeetingSummaryPostgres : InsertIntoMeetingSummary() {
    override fun implementation(
        ctx: DSLContext,
        input: Input,
    ) {
        ctx
            .insertInto(
                MeetingSummary,
                MeetingSummary.id,
                MeetingSummary.sessionId,
                MeetingSummary.summary,
                MeetingSummary.createdBy,
            ).values(
                UUID.randomUUID(),
                input.sessionId,
                input.summary,
                input.createdBy,
            ).execute()
            .let { updatedRows ->
                if (updatedRows != 1) {
                    throw DBException("InsertIntoMeetingSummary failed.")
                }
            }
    }
}
