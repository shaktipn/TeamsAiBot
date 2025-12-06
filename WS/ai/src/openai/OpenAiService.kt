package com.suryadigital.teamsaibot.ai.openai

import com.suryadigital.teamsaibot.ai.AiService

class OpenAiService : AiService {
    override fun getText(input: String): String {
        // TODO: IMplement real sdk.
        return "The user is telling to create a budget in 2 day for a park."
    }
}
