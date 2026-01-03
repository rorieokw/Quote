import Foundation

struct Job: Codable, Identifiable {
    let id: String
    let title: String
    let description: String
    let status: String
    let tradeCategory: String
    let budgetMin: Double?
    let budgetMax: Double?
    let preferredStartDate: Date?
    let suburbName: String
    let state: String
    let postcode: String
    let distanceKm: Double
    let quoteCount: Int
    let customerName: String
    let createdAt: Date
    let mediaUrls: [String]

    var budgetDisplay: String {
        if let min = budgetMin, let max = budgetMax {
            return "$\(Int(min)) - $\(Int(max))"
        } else if let min = budgetMin {
            return "From $\(Int(min))"
        } else if let max = budgetMax {
            return "Up to $\(Int(max))"
        }
        return "Negotiable"
    }

    var locationDisplay: String {
        "\(suburbName), \(state) \(postcode)"
    }
}

struct JobDetail: Codable, Identifiable {
    let id: String
    let title: String
    let description: String
    let status: String
    let tradeCategory: TradeCategoryDto
    let budgetMin: Double?
    let budgetMax: Double?
    let preferredStartDate: Date?
    let preferredEndDate: Date?
    let isFlexibleDates: Bool
    let location: LocationDto
    let propertyType: String
    let customer: CustomerDto
    let media: [JobMediaDto]
    let quotes: [QuoteSummaryDto]
    let createdAt: Date
}

struct TradeCategoryDto: Codable {
    let id: String
    let name: String
    let icon: String?
}

struct LocationDto: Codable {
    let latitude: Double
    let longitude: Double
    let suburbName: String
    let state: String
    let postcode: String
}

struct CustomerDto: Codable {
    let id: String
    let firstName: String
    let profilePhotoUrl: String?
}

struct JobMediaDto: Codable, Identifiable {
    let id: String
    let mediaUrl: String
    let mediaType: String
    let caption: String?
    let thumbnailUrl: String?
}

struct QuoteSummaryDto: Codable, Identifiable {
    let id: String
    let tradieId: String
    let tradieName: String
    let totalCost: Double
    let status: String
    let createdAt: Date
}

struct TradeCategory: Codable, Identifiable {
    let id: String
    let name: String
    let description: String?
    let icon: String?
}

struct PaginatedResponse<T: Codable>: Codable {
    let items: [T]
    let pageNumber: Int
    let totalPages: Int
    let totalCount: Int
    let hasPreviousPage: Bool
    let hasNextPage: Bool
}
