package com.suryadigital.teamsaibot.auth.queries

import com.suryadigital.leo.basedb.QueryInput
import com.suryadigital.leo.basedb.QueryResult
import com.suryadigital.leo.basedb.SingleResultOrNullQuery
import com.suryadigital.teamsaibot.auth.queries.GetUserIdAndPasswordByEmailId.Input
import com.suryadigital.teamsaibot.auth.queries.GetUserIdAndPasswordByEmailId.Result
import java.util.UUID

internal abstract class GetUserIdAndPasswordByEmailId : SingleResultOrNullQuery<Input, Result>() {
    data class Input(
        val emailId: String,
    ) : QueryInput

    data class Result(
        val userId: UUID,
        val hashedPassword: ByteArray,
    ) : QueryResult {
        override fun equals(other: Any?): Boolean {
            if (this === other) return true
            if (javaClass != other?.javaClass) return false

            other as Result

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
