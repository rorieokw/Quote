import Foundation
import SwiftUI

@MainActor
class JobsViewModel: ObservableObject {
    @Published var jobs: [Job] = []
    @Published var selectedJob: JobDetail?
    @Published var categories: [TradeCategory] = []
    @Published var isLoading = false
    @Published var errorMessage: String?
    @Published var currentPage = 1
    @Published var hasMorePages = true

    private let pageSize = 10

    func loadJobs(refresh: Bool = false) async {
        if refresh {
            currentPage = 1
            hasMorePages = true
        }

        guard hasMorePages, !isLoading else { return }

        isLoading = true
        errorMessage = nil

        do {
            let response: PaginatedResponse<Job> = try await APIClient.shared.request(
                endpoint: "jobs?pageNumber=\(currentPage)&pageSize=\(pageSize)"
            )

            if refresh {
                jobs = response.items
            } else {
                jobs.append(contentsOf: response.items)
            }

            hasMorePages = response.hasNextPage
            currentPage += 1
        } catch {
            errorMessage = "Failed to load jobs"
        }

        isLoading = false
    }

    func loadJobDetail(id: String) async {
        isLoading = true
        errorMessage = nil

        do {
            selectedJob = try await APIClient.shared.request(endpoint: "jobs/\(id)")
        } catch {
            errorMessage = "Failed to load job details"
        }

        isLoading = false
    }

    func loadCategories() async {
        do {
            categories = try await APIClient.shared.request(endpoint: "tradecategories")
        } catch {
            print("Failed to load categories")
        }
    }
}
