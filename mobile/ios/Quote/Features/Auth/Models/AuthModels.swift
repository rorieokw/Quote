import Foundation

struct LoginRequest: Encodable {
    let email: String
    let password: String
}

struct RegisterRequest: Encodable {
    let email: String
    let password: String
    let firstName: String
    let lastName: String
    let phone: String?
    let userType: Int
    let abn: String?
}

struct AuthResponse: Decodable {
    let userId: String
    let email: String
    let firstName: String?
    let lastName: String?
    let userType: String?
    let accessToken: String
    let refreshToken: String
}

struct User: Codable, Identifiable {
    let id: String
    let email: String
    let firstName: String
    let lastName: String
    let userType: UserType
    let profilePhotoUrl: String?

    var fullName: String {
        "\(firstName) \(lastName)"
    }
}

enum UserType: String, Codable {
    case customer = "Customer"
    case tradie = "Tradie"
    case admin = "Admin"
}
