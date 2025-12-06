package com.suryadigital.teamsaibot.teamsMeeting

import com.suryadigital.leo.ktor.metrics.KtorMetrics
import com.suryadigital.leo.ktor.metrics.Metrics
import com.suryadigital.teamsaibot.auth.UserIdentity
import com.suryadigital.teamsaibot.teamsMeeting.queries.GetSessionDetailsBySessionId
import com.suryadigital.teamsaibot.teamsMeeting.queries.GetSessionDetailsBySessionIdPostgres
import com.suryadigital.teamsaibot.teamsMeeting.queries.GetUnprocessedTranscripts
import com.suryadigital.teamsaibot.teamsMeeting.queries.GetUnprocessedTranscriptsPostgres
import com.suryadigital.teamsaibot.teamsMeeting.queries.InsertIntoMeetingSession
import com.suryadigital.teamsaibot.teamsMeeting.queries.InsertIntoMeetingSessionPostgres
import com.suryadigital.teamsaibot.teamsMeeting.queries.InsertIntoMeetingSummary
import com.suryadigital.teamsaibot.teamsMeeting.queries.InsertIntoMeetingSummaryPostgres
import com.suryadigital.teamsaibot.teamsMeeting.queries.InsertIntoTranscriptChunk
import com.suryadigital.teamsaibot.teamsMeeting.queries.InsertIntoTranscriptChunkPostgres
import com.suryadigital.teamsaibot.teamsMeeting.queries.MarkTranscriptsAsProcessed
import com.suryadigital.teamsaibot.teamsMeeting.queries.MarkTranscriptsAsProcessedPostgres
import com.suryadigital.teamsaibot.teamsMeeting.websockets.MessageHandlerImpl
import com.suryadigital.teamsaibot.teamsMeeting.websockets.SessionManagerImpl
import com.suryadigital.teamsaibot.teamsMeeting.websockets.SummaryGenerationServiceImpl
import com.suryadigital.teamsaibot.teamsMeeting.websockets.manager.MessageHandler
import com.suryadigital.teamsaibot.teamsMeeting.websockets.manager.SessionManager
import com.suryadigital.teamsaibot.teamsMeeting.websockets.manager.SummaryGenerationService
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import org.koin.core.module.Module
import org.koin.dsl.module

/**
 * Server modules for teams-meeting.
 */
object TeamsMeetingServerModules {
    private val routesModule =
        module {
            factory<CreateMeetingSessionRPC> { (userIdentity: UserIdentity) ->
                CreateMeetingSessionRPCServerImpl(userIdentity = userIdentity)
            }
        }

    private val databaseModule =
        module {
            single<InsertIntoMeetingSession> { InsertIntoMeetingSessionPostgres() }
            single<GetSessionDetailsBySessionId> { GetSessionDetailsBySessionIdPostgres() }
            single<InsertIntoTranscriptChunk> { InsertIntoTranscriptChunkPostgres() }
            single<GetUnprocessedTranscripts> { GetUnprocessedTranscriptsPostgres() }
            single<MarkTranscriptsAsProcessed> { MarkTranscriptsAsProcessedPostgres() }
            single<InsertIntoMeetingSummary> { InsertIntoMeetingSummaryPostgres() }
        }

    private val utilModule =
        module {
            single<MessageHandler> { MessageHandlerImpl() }
            single<SessionManager> { SessionManagerImpl() }

            // Application-level coroutine scope for summary generation
            single<CoroutineScope> {
                CoroutineScope(context = Dispatchers.Default + SupervisorJob() + KtorMetrics(Metrics()))
            }

            // Summary generation service
            single<SummaryGenerationService> {
                SummaryGenerationServiceImpl(applicationScope = get())
            }
        }

    /**
     * Contains the list of necessary moduels for teams meeting.
     */
    val appModules: List<Module> = routesModule + databaseModule + utilModule
}
