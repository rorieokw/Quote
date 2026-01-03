package com.quote.app.features.auth.viewmodel

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.quote.app.features.auth.repository.AuthRepository
import dagger.hilt.android.lifecycle.HiltViewModel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import javax.inject.Inject

data class AuthUiState(
    val isLoading: Boolean = false,
    val errorMessage: String? = null,
    val isSuccess: Boolean = false
)

@HiltViewModel
class AuthViewModel @Inject constructor(
    private val authRepository: AuthRepository
) : ViewModel() {

    private val _uiState = MutableStateFlow(AuthUiState())
    val uiState: StateFlow<AuthUiState> = _uiState.asStateFlow()

    private val _isAuthenticated = MutableStateFlow(authRepository.isLoggedIn())
    val isAuthenticated: StateFlow<Boolean> = _isAuthenticated.asStateFlow()

    fun login(email: String, password: String) {
        viewModelScope.launch {
            _uiState.value = AuthUiState(isLoading = true)

            authRepository.login(email, password)
                .onSuccess {
                    _uiState.value = AuthUiState(isSuccess = true)
                    _isAuthenticated.value = true
                }
                .onFailure { error ->
                    _uiState.value = AuthUiState(
                        errorMessage = error.message ?: "Login failed"
                    )
                }
        }
    }

    fun register(
        email: String,
        password: String,
        firstName: String,
        lastName: String,
        phone: String?,
        isTradie: Boolean,
        abn: String?
    ) {
        viewModelScope.launch {
            _uiState.value = AuthUiState(isLoading = true)

            authRepository.register(
                email = email,
                password = password,
                firstName = firstName,
                lastName = lastName,
                phone = phone,
                isTradie = isTradie,
                abn = abn
            )
                .onSuccess {
                    _uiState.value = AuthUiState(isSuccess = true)
                    _isAuthenticated.value = true
                }
                .onFailure { error ->
                    _uiState.value = AuthUiState(
                        errorMessage = error.message ?: "Registration failed"
                    )
                }
        }
    }

    fun logout() {
        authRepository.logout()
        _isAuthenticated.value = false
    }

    fun clearError() {
        _uiState.value = _uiState.value.copy(errorMessage = null)
    }
}
