package com.suryadigital.teamsaibot.auth.queries

import com.suryadigital.leo.basedb.NoResultQuery
import com.suryadigital.leo.basedb.QueryInput
import com.suryadigital.teamsaibot.auth.queries.InsertIntoPassword.Input
import java.util.UUID

internal abstract class InsertIntoPassword : NoResultQuery<Input>() {
    data class Input(
        val userId: UUID,
        val hashedPassword: ByteArray,
    ) : QueryInput {
        override fun equals(other: Any?): Boolean {
            if (this === other) return true
            if (javaClass != other?.javaClass) return false

            other as Input

            if (userId != other.userId) return false
            if (!hashedPassword.contentEquals(other.hashedPassword)) return false

            return true
        }

        override fun hashCode(): Int {
            var result = userId.hashCode()
            result = 31 * result + hashedPassword.contentHashCode()
            return result
        }
    }
}
