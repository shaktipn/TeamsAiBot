package com.suryadigital.teamsaibot.teamsMeeting.common

object CommonUtil {
    fun Boolean.isFalse(block: () -> Unit) {
        if (!this) {
            block()
        }
    }

    fun Boolean.isTrue(block: () -> Unit) {
        if (this) {
            block()
        }
    }
}
