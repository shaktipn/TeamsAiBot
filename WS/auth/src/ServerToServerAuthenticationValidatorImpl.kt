package com.suryadigital.teamsaibot.auth

import com.suryadigital.leo.rpc.LeoInvalidS2STokenException
import com.suryadigital.leo.rpc.ServerToServerAuthenticationValidator
import com.typesafe.config.Config
import org.koin.core.component.KoinComponent
import org.koin.core.component.inject

internal class ServerToServerAuthenticationValidatorImpl :
    ServerToServerAuthenticationValidator,
    KoinComponent {
    private val config by inject<Config>()

    override suspend fun validateSecret(secret: String) {
        if (secret != config.getString("s2sAuth.secret")) {
            throw LeoInvalidS2STokenException()
        }
    }
}
