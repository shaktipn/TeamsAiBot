package com.suryadigital.teamsaibot.ai.openai

import com.openai.client.okhttp.OpenAIOkHttpClient
import com.openai.models.responses.Response
import com.openai.models.responses.ResponseCreateParams
import com.openai.models.responses.ResponseInputContent
import com.openai.models.responses.ResponseInputItem
import com.openai.models.responses.ResponseInputText
import com.suryadigital.leo.ktUtils.cached
import com.suryadigital.teamsaibot.ai.AiService
import com.suryadigital.teamsaibot.ai.prompts.SystemPrompts.PROCESS_TRANSCRIPTION
import com.typesafe.config.Config
import kotlinx.coroutines.future.await
import org.koin.core.component.KoinComponent
import org.koin.core.component.inject

@Suppress("unused") // Open AI will not be used for processing transcription yet.
internal class OpenAiService :
    AiService,
    KoinComponent {
    private val config by inject<Config>()
    private val openAiClient =
        OpenAIOkHttpClient
            .builder()
            .apiKey(config.getString("openAi.apiKey"))
            .build()
    private val modelToUse by cached { config.getString("openAi.model") }

    override suspend fun getAiReply(input: String): String {
        val ressponseParams =
            ResponseCreateParams
                .builder()
                .model(string = modelToUse)
                .instructions(PROCESS_TRANSCRIPTION)
                .inputOfResponse(
                    listOf(
                        ResponseInputItem.ofMessage(
                            ResponseInputItem.Message
                                .builder()
                                .role(ResponseInputItem.Message.Role.USER)
                                .content(listOf(buildTextPart(input)))
                                .build(),
                        ),
                    ),
                ).build()
        val response =
            openAiClient
                .async()
                .responses()
                .create(ressponseParams)
                .await()
        return response.outputTextOrNull() ?: throw IllegalStateException("No response from Open Ai: $response")
    }

    private fun Response.outputTextOrNull() =
        output()
            .lastOrNull()
            ?.message()
            ?.get()
            ?.content()
            ?.lastOrNull()
            ?.outputText()
            ?.get()
            ?.text()

    private fun buildTextPart(text: String) =
        ResponseInputContent.ofInputText(
            ResponseInputText
                .builder()
                .text(text)
                .build(),
        )
}
