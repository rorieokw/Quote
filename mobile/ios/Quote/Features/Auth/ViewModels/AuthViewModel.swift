import Foundation
import SwiftUI

@MainActor
class AuthViewModel: ObservableObject {
    @Published var isAuthenticated = false
    @Published var currentUser: User?
    @Published var isLoading = false
    @Published var errorMessage: String?

    private let keychainKey = "accessToken"

    init() {
        checkAuthStatus()
    }

    private func checkAuthStatus() {
        if let token = KeychainManager.shared.get(key: keychainKey) {
            APIClient.shared.setAuthToken(token)
            isAuthenticated = true
        }
    }

    func login(email: String, password: String) async {
        isLoading = true
        errorMessage = nil

        do {
            let request = LoginRequest(email: email, password: password)
            let response: AuthResponse = try await APIClient.shared.request(
                endpoint: "auth/login",
                method: "POST",
                body: request
            )

            _ = KeychainManager.shared.save(key: keychainKey, value: response.accessToken)
            APIClient.shared.setAuthToken(response.accessToken)
            isAuthenticated = true
        } catch let error as APIError {
            switch error {
            case .serverError(let message):
                errorMessage = message
            case .unauthorized:
                errorMessage = "Invalid email or password"
            default:
                errorMessage = "Login failed. Please try again."
            }
        } catch {
            errorMessage = "An unexpected error occurred"
        }

        isLoading = false
    }

    func register(
        email: String,
        password: String,
        firstName: String,
        lastName: String,
        phone: String?,
        isTradie: Bool,
        abn: String?
    ) async {
        isLoading = true
        errorMessage = nil

        do {
            let request = RegisterRequest(
                email: email,
                password: password,
                firstName: firstName,
                lastName: lastName,
                phone: phone,
                userType: isTradie ? 2 : 1,
                abn: abn
            )

            let response: AuthResponse = try await APIClient.shared.request(
                endpoint: "auth/register",
                method: "POST",
                body: request
            )

            _ = KeychainManager.shared.save(key: keychainKey, value: response.accessToken)
            APIClient.shared.setAuthToken(response.accessToken)
            isAuthenticated = true
        } catch let error as APIError {
            switch error {
            case .serverError(let message):
                errorMessage = message
            default:
                errorMessage = "Registration failed. Please try again."
            }
        } catch {
            errorMessage = "An unexpected error occurred"
        }

        isLoading = false
    }

    func logout() {
        _ = KeychainManager.shared.delete(key: keychainKey)
        APIClient.shared.setAuthToken(nil)
        currentUser = nil
        isAuthenticated = false
    }
}
