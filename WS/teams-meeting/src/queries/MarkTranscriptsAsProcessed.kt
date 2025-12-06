package com.suryadigital.teamsaibot.teamsMeeting.queries

import com.suryadigital.leo.basedb.NoResultQuery
import com.suryadigital.leo.basedb.QueryInput
import com.suryadigital.teamsaibot.teamsMeeting.queries.MarkTranscriptsAsProcessed.Input
import java.util.UUID

internal abstract class MarkTranscriptsAsProcessed : NoResultQuery<Input>() {
    data class Input(
        val transcriptIds: List<UUID>,
    ) : QueryInput
}
