package com.suryadigital.teamsaibot.ai.anthropic

import com.anthropic.client.okhttp.AnthropicOkHttpClientAsync
import com.anthropic.models.messages.ContentBlockParam
import com.anthropic.models.messages.MessageCreateParams
import com.anthropic.models.messages.MessageParam
import com.anthropic.models.messages.TextBlockParam
import com.suryadigital.leo.inlineLogger.getInlineLogger
import com.suryadigital.leo.ktUtils.cached
import com.suryadigital.teamsaibot.ai.AiService
import com.suryadigital.teamsaibot.ai.prompts.SystemPrompts.PROCESS_TRANSCRIPTION
import com.typesafe.config.Config
import org.koin.core.component.KoinComponent
import org.koin.core.component.inject

internal class AnthropicAiService :
    AiService,
    KoinComponent {
    private val config by inject<Config>()
    private val apiKey by cached { config.getString("anthropic.apiKey") }
    private val modelToUse by cached { config.getString("anthropic.model") }
    private val asyncClient =
        AnthropicOkHttpClientAsync
            .builder()
            .apiKey(apiKey = apiKey)
            .build()
    private val logger = getInlineLogger(this::class)

    override suspend fun getAiReply(input: String): String {
        val messageCreateParams =
            MessageCreateParams
                .builder()
                .temperature(1.0)
                .model(modelToUse)
                .maxTokens(63_000L)
                .system(PROCESS_TRANSCRIPTION)
                .addUserMessage(
                    MessageParam.Content.ofBlockParams(
                        buildList {
                            add(ContentBlockParam.ofText(TextBlockParam.builder().text(input).build()))
                        },
                    ),
                ).build()
        logger.info { "Initiating Anthropic text generation." }
        val response =
            asyncClient
                .messages()
                .create(messageCreateParams)
                .join()
        response.stopReason().ifPresent { stopReason ->
            logger.info { "Anthropic Ai call finished due to: ${stopReason.value()}" }
        }
        return response
            .content()
            .first()
            .text()
            .get()
            .text()
    }
}
