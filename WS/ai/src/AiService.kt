package com.suryadigital.teamsaibot.ai

interface AiService {
    suspend fun getAiReply(input: String): String
}
