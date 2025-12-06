package com.suryadigital.teamsaibot.teamsMeeting.queries

import com.suryadigital.leo.basedb.getNonNullValue
import com.suryadigital.teamsaibot.jooq.tables.references.TranscriptChunk
import org.jooq.DSLContext

/**
 * PostgreSQL implementation of GetUnprocessedTranscripts query.
 * Fetches transcript chunks that are final and not yet processed.
 */
internal class GetUnprocessedTranscriptsPostgres : GetUnprocessedTranscripts() {
    override fun implementation(
        ctx: DSLContext,
        input: Input,
    ): List<Result> =
        ctx
            .select(
                TranscriptChunk.id,
                TranscriptChunk.text,
                TranscriptChunk.speakerName,
                TranscriptChunk.createdAt,
            ).from(TranscriptChunk)
            .where(TranscriptChunk.sessionId.eq(input.sessionId))
            .and(TranscriptChunk.isFinal.isTrue)
            .and(TranscriptChunk.processed.isFalse)
            .and(TranscriptChunk.isDeleted.isFalse)
            .orderBy(TranscriptChunk.createdAt.asc())
            .fetch()
            .map { record ->
                Result(
                    id = record.getNonNullValue(TranscriptChunk.id),
                    text = record.getNonNullValue(TranscriptChunk.text),
                    speakerName = record.get(TranscriptChunk.speakerName),
                    createdAt = record.getNonNullValue(TranscriptChunk.createdAt),
                )
            }
}
