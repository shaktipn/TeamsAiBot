package com.suryadigital.teamsaibot.auth

import com.suryadigital.leo.basedb.Database
import com.suryadigital.leo.basedb.timedQuery
import com.suryadigital.leo.crypto.HashVerifier
import com.suryadigital.leo.rpc.LeoRPCResult
import com.suryadigital.teamsaibot.auth.SignUpWebRPC.Error
import com.suryadigital.teamsaibot.auth.SignUpWebRPC.Request
import com.suryadigital.teamsaibot.auth.SignUpWebRPC.Response
import com.suryadigital.teamsaibot.auth.exceptions.executeWithExceptionHandling
import com.suryadigital.teamsaibot.auth.queries.CheckUserWithEmailIdExists
import com.suryadigital.teamsaibot.auth.queries.InsertIntoPassword
import com.suryadigital.teamsaibot.auth.queries.InsertIntoUser
import org.jooq.DSLContext
import org.koin.core.component.KoinComponent
import org.koin.core.component.inject
import java.util.UUID

internal class SignUpWebRPCServerImpl :
    SignUpWebRPC,
    KoinComponent {
    private val checkUserWithEmailIdExists by inject<CheckUserWithEmailIdExists>()
    private val database by inject<Database>()
    private val hashVerifier by inject<HashVerifier>()
    private val insertIntoUser by inject<InsertIntoUser>()
    private val insertIntoPassword by inject<InsertIntoPassword>()

    override suspend fun execute(request: Request): LeoRPCResult<Response, Error> =
        executeWithExceptionHandling {
            LeoRPCResult.response(getResponse(request))
        }

    private suspend fun getResponse(request: Request): Response =
        database.timedQuery { ctx ->
            if (checkUserWithEmailIdExists
                    .execute(
                        ctx = ctx,
                        input =
                            CheckUserWithEmailIdExists.Input(
                                emailId = request.emailId.value,
                            ),
                    ).exists
            ) {
                throw SignUpWebRPC.EmailIdAlreadyExistsException()
            }
            checkPasswordConstraints(password = request.password)
            addUserAndPassword(
                request = request,
                ctx = ctx,
            )
            Response
        }

    private fun checkPasswordConstraints(password: String) {
        if (!passwordConstraints.matches(password)) {
            throw SignUpWebRPC.InsecurePasswordException(
                "Password must be at least 8 characters long and include 1 uppercase letter, 1 lowercase letter, 1 digit, and 1 special character.",
            )
        }
    }

    private fun addUserAndPassword(
        request: Request,
        ctx: DSLContext,
    ) {
        val userId = UUID.randomUUID()
        val hashedPassowrd = hashVerifier.generateHash(request.password)
        insertIntoUser.execute(
            ctx = ctx,
            input =
                InsertIntoUser.Input(
                    emailId = request.emailId.value,
                    userId = userId,
                ),
        )
        insertIntoPassword.execute(
            ctx = ctx,
            input =
                InsertIntoPassword.Input(
                    userId = userId,
                    hashedPassword = hashedPassowrd,
                ),
        )
    }

    companion object {
        val passwordConstraints = Regex(pattern = "^(?=.*[A-Z])(?=.*[a-z])(?=.*\\d)(?=.*[^A-Za-z0-9]).{8,}$")
    }
}
