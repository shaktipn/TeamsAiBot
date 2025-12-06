package com.suryadigital.teamsaibot.application

import com.suryadigital.leo.basedb.Configuration
import com.suryadigital.leo.basedb.Database
import com.suryadigital.leo.crypto.Argon2ID13HashVerifier
import com.suryadigital.leo.crypto.HashVerifier
import com.suryadigital.leo.ktor.installStandardFeatures
import com.suryadigital.leo.ktor.parseConfig
import com.suryadigital.leo.ktor.standardFeatures.configureJsonCallLogging
import com.suryadigital.leo.rpc.LeoCookieConfig
import com.suryadigital.teamsaibot.ai.AiServerModules
import com.suryadigital.teamsaibot.auth.AuthServerModules
import com.suryadigital.teamsaibot.auth.authRoutes
import com.suryadigital.teamsaibot.teamsMeeting.TeamsMeetingServerModules
import com.suryadigital.teamsaibot.teamsMeeting.teamsMeetingRoutes
import com.suryadigital.teamsaibot.teamsMeeting.websockets.webSocketRoutes
import com.typesafe.config.Config
import com.typesafe.config.ConfigFactory
import io.ktor.http.HttpHeaders
import io.ktor.server.application.Application
import io.ktor.server.application.install
import io.ktor.server.engine.CommandLineConfig
import io.ktor.server.engine.embeddedServer
import io.ktor.server.netty.Netty
import io.ktor.server.plugins.calllogging.CallLoggingConfig
import io.ktor.server.plugins.cors.routing.CORS
import io.ktor.server.websocket.WebSockets
import io.ktor.server.websocket.pingPeriod
import io.ktor.server.websocket.timeout
import kotlinx.serialization.json.Json
import org.koin.dsl.module
import org.koin.ktor.plugin.Koin
import kotlin.time.Duration.Companion.seconds

/**
 * Entrypoint.
 */
fun main(args: Array<String>) {
    config = parseConfig(args)
    val envConfig = CommandLineConfig(args)
    embeddedServer(
        factory = Netty,
        configure = {
            takeFrom(envConfig.engineConfig)
        },
        module = {
            module(false)
        },
    ).start(wait = true)
}

internal fun Application.module(testing: Boolean) {
    if (testing) {
        config = ConfigFactory.load("test.conf")
    }

    install(Koin) {
        if (testing) {
            modules(emptyList())
        } else {
            modules(rootAppModule)
        }
    }

//    installCORS(
//        hosts = config.getStringList("allowedCorsHosts"),
//        httpMethod = HttpMethod.DefaultMethods,
//        httpHeader = listOf("Content-Type"),
//        allowCredentials = true,
//    )

    install(CORS) {
        anyHost()
        allowHeader(HttpHeaders.Origin)
        allowHeader(HttpHeaders.AccessControlAllowOrigin)
    }

    installStandardFeatures(callLoggingConfiguration = CallLoggingConfig::configureJsonCallLogging)

    install(WebSockets) {
        pingPeriod = 20.seconds // Every 20s the server will send a ping signal to all clients.
        timeout = 10.seconds // In 10 sec if the client doesn't respond with a pong to the ping then the connection will be closed.
        maxFrameSize = Long.MAX_VALUE
        masking = false
    }

    authRoutes()
    teamsMeetingRoutes()

    webSocketRoutes()
}

private val utilModule =
    module {
        single {
            Json {
                encodeDefaults = true
            }
        }
        single { config }
        single {
            val config: Config = get()
            val dbConfiguration = Configuration.fromConfig(config.getConfig("database"))
            Database(dbConfiguration)
        }
        single<HashVerifier> { Argon2ID13HashVerifier() }
        single {
            val config: Config = get()
            LeoCookieConfig.fromConfig(config.getConfig("com.suryadigital.leo.rpc.webToken.cookie"))
        }
    }

private val rootAppModule = utilModule + AuthServerModules.appModules + TeamsMeetingServerModules.appModules + AiServerModules.appModules

private lateinit var config: Config
