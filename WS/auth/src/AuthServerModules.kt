package com.suryadigital.teamsaibot.auth

import com.suryadigital.leo.rpc.ServerToServerAuthenticationValidator
import com.suryadigital.teamsaibot.auth.queries.CheckUserWithEmailIdExists
import com.suryadigital.teamsaibot.auth.queries.CheckUserWithEmailIdExistsPostgres
import com.suryadigital.teamsaibot.auth.queries.GetUserAndWTDetailsByWT
import com.suryadigital.teamsaibot.auth.queries.GetUserAndWTDetailsByWTPostgres
import com.suryadigital.teamsaibot.auth.queries.GetUserIdAndPasswordByEmailId
import com.suryadigital.teamsaibot.auth.queries.GetUserIdAndPasswordByEmailIdPostgres
import com.suryadigital.teamsaibot.auth.queries.InsertIntoPassword
import com.suryadigital.teamsaibot.auth.queries.InsertIntoPasswordPostgres
import com.suryadigital.teamsaibot.auth.queries.InsertIntoUser
import com.suryadigital.teamsaibot.auth.queries.InsertIntoUserPostgres
import com.suryadigital.teamsaibot.auth.queries.InsertIntoWT
import com.suryadigital.teamsaibot.auth.queries.InsertIntoWTPostgres
import io.ktor.server.application.ApplicationCall
import org.koin.core.module.Module
import org.koin.dsl.module

/**
 * Server modules for auth.
 */
object AuthServerModules {
    private val routesModule =
        module {
            single<SignUpWebRPC> { SignUpWebRPCServerImpl() }
            factory<SignInWebRPC> { (call: ApplicationCall) -> SignInWebRPCServerImpl(call) }
            single<ValidateWTRPC> { ValidateWTRPCServerImpl() }
            single<ServerToServerAuthenticationValidator> { ServerToServerAuthenticationValidatorImpl() }
        }

    private val databaseModule =
        module {
            single<CheckUserWithEmailIdExists> { CheckUserWithEmailIdExistsPostgres() }
            single<InsertIntoUser> { InsertIntoUserPostgres() }
            single<InsertIntoPassword> { InsertIntoPasswordPostgres() }
            single<GetUserIdAndPasswordByEmailId> { GetUserIdAndPasswordByEmailIdPostgres() }
            single<InsertIntoWT> { InsertIntoWTPostgres() }
            single<GetUserAndWTDetailsByWT> { GetUserAndWTDetailsByWTPostgres() }
        }

    /**
     * App modules for auth.
     */
    val appModules: List<Module> = routesModule + databaseModule
}
