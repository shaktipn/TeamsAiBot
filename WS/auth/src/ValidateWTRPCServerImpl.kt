package com.suryadigital.teamsaibot.auth

import com.suryadigital.leo.basedb.Database
import com.suryadigital.leo.basedb.timedQuery
import com.suryadigital.leo.rpc.LeoRPCResult
import com.suryadigital.teamsaibot.auth.ValidateWTRPC.Error
import com.suryadigital.teamsaibot.auth.ValidateWTRPC.Request
import com.suryadigital.teamsaibot.auth.ValidateWTRPC.Response
import com.suryadigital.teamsaibot.auth.exceptions.executeWithExceptionHandling
import com.suryadigital.teamsaibot.auth.queries.GetUserAndWTDetailsByWT
import org.koin.core.component.KoinComponent
import org.koin.core.component.inject
import java.time.Instant

internal class ValidateWTRPCServerImpl :
    ValidateWTRPC,
    KoinComponent {
    private val database by inject<Database>()
    private val getUserAndWTDetailsByWT by inject<GetUserAndWTDetailsByWT>()

    override suspend fun execute(request: Request): LeoRPCResult<Response, Error> =
        executeWithExceptionHandling {
            LeoRPCResult.LeoResponse(response = getResponse(request))
        }

    private suspend fun getResponse(request: Request): Response =
        database.timedQuery { ctx ->
            val userAndWtDetails =
                getUserAndWTDetailsByWT.execute(
                    ctx = ctx,
                    input =
                        GetUserAndWTDetailsByWT.Input(
                            wt = request.wt,
                        ),
                ) ?: throw ValidateWTRPC.InvalidWtException()

            if (!userAndWtDetails.userIsActive) {
                throw ValidateWTRPC.UserDisabledException()
            }

            if (userAndWtDetails.wtExpiresAt.isBefore(Instant.now())) {
                throw ValidateWTRPC.InvalidWtException()
            }

            Response(
                userIdentity =
                    UserIdentity(
                        id = userAndWtDetails.userId,
                        privileges = emptyList(),
                    ),
                wt = null,
            )
        }
}
