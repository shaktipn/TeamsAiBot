package com.suryadigital.teamsaibot.teamsMeeting.queries

import com.suryadigital.leo.basedb.DBException
import com.suryadigital.teamsaibot.jooq.tables.references.MeetingSession
import org.jooq.DSLContext

internal class InsertIntoMeetingSessionPostgres : InsertIntoMeetingSession() {
    override fun implementation(
        ctx: DSLContext,
        input: Input,
    ) {
        ctx
            .insertInto(
                MeetingSession,
                MeetingSession.id,
                MeetingSession.meetingId,
                MeetingSession.threadId,
                MeetingSession.messageId,
                MeetingSession.status,
            ).values(
                input.sessionId,
                input.meetingId,
                input.threadId,
                input.meetingId,
                "REGISTERED",
            ).execute()
            .let { updatedRowCount ->
                if (updatedRowCount != 1) {
                    throw DBException("InsertIntoMeetingSession failed.")
                }
            }
    }
}
