package com.suryadigital.teamsaibot.ai

import com.suryadigital.teamsaibot.ai.openai.OpenAiService
import org.koin.core.module.Module
import org.koin.dsl.module

/**
 *
 */
object AiServerModules {
    private val utilModule =
        module {
            single<AiService> { OpenAiService() }
        }

    /**
     * Contains the list of necessary moduels for ai operations.
     */
    val appModules: List<Module> = listOf(utilModule)
}
