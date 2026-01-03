package com.quote.app.core.network

import com.quote.app.features.auth.repository.LoginRequest
import com.quote.app.features.auth.repository.AuthResponse
import com.quote.app.features.auth.repository.RegisterRequest
import com.quote.app.features.jobs.repository.Job
import com.quote.app.features.jobs.repository.JobDetail
import com.quote.app.features.jobs.repository.PaginatedResponse
import com.quote.app.features.jobs.repository.TradeCategory
import retrofit2.http.*

interface ApiService {

    // Auth
    @POST("auth/login")
    suspend fun login(@Body request: LoginRequest): AuthResponse

    @POST("auth/register")
    suspend fun register(@Body request: RegisterRequest): AuthResponse

    // Jobs
    @GET("jobs")
    suspend fun getJobs(
        @Query("pageNumber") pageNumber: Int = 1,
        @Query("pageSize") pageSize: Int = 10,
        @Query("tradeCategoryId") tradeCategoryId: String? = null,
        @Query("state") state: String? = null
    ): PaginatedResponse<Job>

    @GET("jobs/{id}")
    suspend fun getJob(@Path("id") id: String): JobDetail

    // Trade Categories
    @GET("tradecategories")
    suspend fun getTradeCategories(): List<TradeCategory>
}
