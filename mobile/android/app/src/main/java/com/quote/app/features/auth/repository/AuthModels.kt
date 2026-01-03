package com.quote.app.features.auth.repository

data class LoginRequest(
    val email: String,
    val password: String
)

data class RegisterRequest(
    val email: String,
    val password: String,
    val firstName: String,
    val lastName: String,
    val phone: String?,
    val userType: Int,
    val abn: String?
)

data class AuthResponse(
    val userId: String,
    val email: String,
    val firstName: String?,
    val lastName: String?,
    val userType: String?,
    val accessToken: String,
    val refreshToken: String
)
