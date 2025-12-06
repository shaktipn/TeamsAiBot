package com.suryadigital.teamsaibot.teamsMeeting.queries

import com.suryadigital.leo.basedb.NoResultQuery
import com.suryadigital.leo.basedb.QueryInput
import com.suryadigital.teamsaibot.teamsMeeting.queries.InsertIntoMeetingSession.Input
import java.util.UUID

internal abstract class InsertIntoMeetingSession : NoResultQuery<Input>() {
    data class Input(
        val sessionId: UUID,
        val meetingId: String,
        val threadId: String,
        val messageId: String,
    ) : QueryInput
}
