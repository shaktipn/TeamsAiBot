package com.suryadigital.teamsaibot.teamsMeeting

import com.suryadigital.leo.basedb.Database
import com.suryadigital.leo.basedb.timedQuery
import com.suryadigital.leo.rpc.LeoRPCResult
import com.suryadigital.teamsaibot.auth.UserIdentity
import com.suryadigital.teamsaibot.teamsMeeting.CreateMeetingSessionRPC.Error
import com.suryadigital.teamsaibot.teamsMeeting.CreateMeetingSessionRPC.Request
import com.suryadigital.teamsaibot.teamsMeeting.CreateMeetingSessionRPC.Response
import com.suryadigital.teamsaibot.teamsMeeting.exceptions.executeWithExceptionHandling
import com.suryadigital.teamsaibot.teamsMeeting.queries.InsertIntoMeetingSession
import org.koin.core.component.KoinComponent
import org.koin.core.component.inject
import java.net.URI
import java.util.UUID

internal class CreateMeetingSessionRPCServerImpl(
    private val userIdentity: UserIdentity,
) : CreateMeetingSessionRPC,
    KoinComponent {
    private val database by inject<Database>()
    private val insertIntoMeetingSession by inject<InsertIntoMeetingSession>()

    override suspend fun execute(request: Request): LeoRPCResult<Response, Error> =
        executeWithExceptionHandling {
            LeoRPCResult.LeoResponse(response = getResponse(request))
        }

    private suspend fun getResponse(request: Request): Response =
        database.timedQuery { ctx ->
            // TODO: Check using the meeting details that if we already have the meeting registed. Then just return the values here.
            val sessionId = UUID.randomUUID()
            insertIntoMeetingSession.execute(
                ctx = ctx,
                input =
                    InsertIntoMeetingSession.Input(
                        sessionId = sessionId,
                        meetingId = request.meetingId,
                        threadId = request.threadId,
                        messageId = request.meetingId,
                    ),
            )
            Response(
                sessionId = sessionId,
                liveSummaryUrl = URI("https://google.com").toURL(),
            )
        }
}
