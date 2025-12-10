package com.suryadigital.teamsaibot.teamsMeeting.queries

import com.suryadigital.leo.basedb.fetchOneOrNone
import com.suryadigital.leo.basedb.getNonNullValue
import com.suryadigital.teamsaibot.jooq.tables.references.MeetingSession
import com.suryadigital.teamsaibot.jooq.tables.references.MeetingSummary
import org.jooq.DSLContext

internal class GetLatestMeetingSummaryPostgres : GetLatestMeetingSummary() {
    override fun implementation(
        ctx: DSLContext,
        input: Input,
    ): Result? =
        ctx
            .select(MeetingSummary.summary)
            .from(MeetingSummary)
            .join(MeetingSession)
            .on(MeetingSession.id.eq(MeetingSummary.sessionId))
            .where(MeetingSummary.sessionId.eq(input.sessionId))
            .and(MeetingSession.isDeleted.isFalse)
            .and(MeetingSummary.isDeleted.isFalse)
            .orderBy(MeetingSummary.createdAt.desc())
            .limit(1)
            .fetchOneOrNone()
            ?.let { record ->
                Result(summary = record.getNonNullValue(MeetingSummary.summary))
            }
}
