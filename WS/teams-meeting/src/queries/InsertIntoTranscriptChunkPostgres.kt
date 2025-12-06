package com.suryadigital.teamsaibot.teamsMeeting.queries

import com.suryadigital.leo.basedb.DBException
import com.suryadigital.teamsaibot.jooq.tables.references.TranscriptChunk
import org.jooq.DSLContext
import java.util.UUID

internal class InsertIntoTranscriptChunkPostgres : InsertIntoTranscriptChunk() {
    override fun implementation(
        ctx: DSLContext,
        input: Input,
    ) {
        ctx
            .insertInto(
                TranscriptChunk,
                TranscriptChunk.id,
                TranscriptChunk.text,
                TranscriptChunk.sessionId,
                TranscriptChunk.isFinal,
            ).values(
                UUID.randomUUID(),
                input.transcript,
                input.sessionId,
                true,
            ).execute()
            .let { updatedRows ->
                if (updatedRows != 1) {
                    throw DBException("InsertIntoTranscriptChunk failed.")
                }
            }
    }
}
