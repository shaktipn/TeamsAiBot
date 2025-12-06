package com.suryadigital.teamsaibot.auth

import com.suryadigital.leo.basedb.Database
import com.suryadigital.leo.basedb.timedQuery
import com.suryadigital.leo.crypto.HashVerifier
import com.suryadigital.leo.ktor.setCookie
import com.suryadigital.leo.rpc.LeoCookieConfig
import com.suryadigital.leo.rpc.LeoRPCResult
import com.suryadigital.teamsaibot.auth.SignInWebRPC.Error
import com.suryadigital.teamsaibot.auth.SignInWebRPC.Request
import com.suryadigital.teamsaibot.auth.SignInWebRPC.Response
import com.suryadigital.teamsaibot.auth.exceptions.executeWithExceptionHandling
import com.suryadigital.teamsaibot.auth.queries.GetUserIdAndPasswordByEmailId
import com.suryadigital.teamsaibot.auth.queries.InsertIntoWT
import io.ktor.server.application.ApplicationCall
import org.koin.core.component.KoinComponent
import org.koin.core.component.inject
import java.time.Instant
import java.util.UUID

internal class SignInWebRPCServerImpl(
    private val call: ApplicationCall,
) : SignInWebRPC,
    KoinComponent {
    private val database by inject<Database>()
    private val getUserIdAndPasswordByEmailId by inject<GetUserIdAndPasswordByEmailId>()
    private val hashVerifier by inject<HashVerifier>()
    private val cookieConfig by inject<LeoCookieConfig>()
    private val insertIntoWT by inject<InsertIntoWT>()

    override suspend fun execute(request: Request): LeoRPCResult<Response, Error> =
        executeWithExceptionHandling {
            LeoRPCResult.LeoResponse(response = getResponse(request))
        }

    private suspend fun getResponse(request: Request): Response =
        database.timedQuery { ctx ->
            val (userId, hashedPassword) =
                getUserIdAndPasswordByEmailId.execute(
                    ctx = ctx,
                    input =
                        GetUserIdAndPasswordByEmailId.Input(
                            emailId = request.emailId.value,
                        ),
                ) ?: throw SignInWebRPC.InvalidCredentialsException()
            if (!hashVerifier.isHashVerified(
                    hashedValue = hashedPassword,
                    actualValue = request.password,
                )
            ) {
                throw SignInWebRPC.InvalidCredentialsException()
            }
            val freshWt = UUID.randomUUID().toString()
            insertIntoWT.execute(
                ctx,
                InsertIntoWT.Input(
                    wt = freshWt,
                    userId = userId,
                    expiresAt = Instant.now().plusSeconds(cookieConfig.maxAgeInSeconds.toLong()),
                ),
            )
            call.response.setCookie(
                name = cookieConfig.name,
                value = freshWt,
                maxAge = cookieConfig.maxAgeInSeconds,
                path = cookieConfig.path,
                domain = cookieConfig.domain,
                httpOnly = cookieConfig.httpOnly,
                secure = cookieConfig.secure,
                extensions = cookieConfig.extensions,
            )
            Response
        }
}
