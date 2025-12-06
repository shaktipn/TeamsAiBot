package com.suryadigital.teamsaibot.teamsMeeting.queries

import com.suryadigital.leo.basedb.DBException
import com.suryadigital.teamsaibot.jooq.tables.references.TranscriptChunk
import org.jooq.DSLContext

internal class MarkTranscriptsAsProcessedPostgres : MarkTranscriptsAsProcessed() {
    override fun implementation(
        ctx: DSLContext,
        input: Input,
    ) {
        if (input.transcriptIds.isEmpty()) {
            return // No-op for empty list
        }

        val updatedRows =
            ctx
                .update(TranscriptChunk)
                .set(TranscriptChunk.processed, true)
                .where(TranscriptChunk.id.`in`(input.transcriptIds))
                .execute()

        if (updatedRows != input.transcriptIds.size) {
            throw DBException(
                """Expected to mark ${input.transcriptIds.size} transcripts as processed, but updated $updatedRows rows""",
            )
        }
    }
}
