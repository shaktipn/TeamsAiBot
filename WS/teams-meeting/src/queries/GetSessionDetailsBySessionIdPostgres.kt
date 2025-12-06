package com.suryadigital.teamsaibot.teamsMeeting.queries

import com.suryadigital.leo.basedb.fetchOneOrNone
import com.suryadigital.leo.basedb.getNonNullValue
import com.suryadigital.teamsaibot.jooq.tables.references.MeetingSession
import com.suryadigital.teamsaibot.teamsMeeting.utils.MeetingSessionStatus
import org.jooq.DSLContext

internal class GetSessionDetailsBySessionIdPostgres : GetSessionDetailsBySessionId() {
    override fun implementation(
        ctx: DSLContext,
        input: Input,
    ): Result? =
        ctx
            .select(MeetingSession.id, MeetingSession.status)
            .from(MeetingSession)
            .where(MeetingSession.id.eq(input.sessionId))
            .and(MeetingSession.isDeleted.isFalse)
            .fetchOneOrNone()
            ?.let { record ->
                Result(
                    sessionId = record.getNonNullValue(MeetingSession.id),
                    sessionStatus = MeetingSessionStatus.valueOf(record.getNonNullValue(MeetingSession.status)),
                )
            }
}
