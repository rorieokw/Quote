import SwiftUI

struct JobsListView: View {
    @StateObject private var viewModel = JobsViewModel()

    var body: some View {
        NavigationStack {
            List {
                ForEach(viewModel.jobs) { job in
                    NavigationLink(value: job) {
                        JobRowView(job: job)
                    }
                }

                if viewModel.hasMorePages && !viewModel.isLoading {
                    ProgressView()
                        .frame(maxWidth: .infinity)
                        .onAppear {
                            Task {
                                await viewModel.loadJobs()
                            }
                        }
                }
            }
            .listStyle(.plain)
            .navigationTitle("Jobs")
            .navigationDestination(for: Job.self) { job in
                JobDetailView(jobId: job.id)
            }
            .refreshable {
                await viewModel.loadJobs(refresh: true)
            }
            .overlay {
                if viewModel.isLoading && viewModel.jobs.isEmpty {
                    ProgressView("Loading jobs...")
                }

                if let error = viewModel.errorMessage, viewModel.jobs.isEmpty {
                    ContentUnavailableView {
                        Label("Error", systemImage: "exclamationmark.triangle")
                    } description: {
                        Text(error)
                    } actions: {
                        Button("Retry") {
                            Task {
                                await viewModel.loadJobs(refresh: true)
                            }
                        }
                    }
                }
            }
        }
        .task {
            await viewModel.loadJobs()
        }
    }
}

struct JobRowView: View {
    let job: Job

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Text(job.tradeCategory)
                    .font(.caption)
                    .padding(.horizontal, 8)
                    .padding(.vertical, 4)
                    .background(Color.blue.opacity(0.1))
                    .foregroundColor(.blue)
                    .cornerRadius(4)

                Spacer()

                Text(job.budgetDisplay)
                    .font(.subheadline)
                    .fontWeight(.semibold)
            }

            Text(job.title)
                .font(.headline)
                .lineLimit(2)

            Text(job.description)
                .font(.subheadline)
                .foregroundColor(.secondary)
                .lineLimit(2)

            HStack {
                Image(systemName: "mappin.circle")
                    .foregroundColor(.secondary)
                Text(job.locationDisplay)
                    .font(.caption)
                    .foregroundColor(.secondary)

                Spacer()

                if job.quoteCount > 0 {
                    Text("\(job.quoteCount) quotes")
                        .font(.caption)
                        .foregroundColor(.orange)
                }
            }
        }
        .padding(.vertical, 8)
    }
}

struct JobDetailView: View {
    let jobId: String
    @StateObject private var viewModel = JobsViewModel()

    var body: some View {
        ScrollView {
            if let job = viewModel.selectedJob {
                VStack(alignment: .leading, spacing: 16) {
                    if !job.media.isEmpty {
                        TabView {
                            ForEach(job.media) { media in
                                AsyncImage(url: URL(string: media.mediaUrl)) { image in
                                    image
                                        .resizable()
                                        .aspectRatio(contentMode: .fill)
                                } placeholder: {
                                    Rectangle()
                                        .fill(Color.gray.opacity(0.2))
                                }
                            }
                        }
                        .tabViewStyle(.page)
                        .frame(height: 250)
                    }

                    VStack(alignment: .leading, spacing: 16) {
                        Text(job.title)
                            .font(.title2)
                            .fontWeight(.bold)

                        HStack {
                            Label(job.tradeCategory.name, systemImage: "wrench.and.screwdriver")
                            Spacer()
                            Text(job.status)
                                .padding(.horizontal, 8)
                                .padding(.vertical, 4)
                                .background(Color.green.opacity(0.1))
                                .foregroundColor(.green)
                                .cornerRadius(4)
                        }

                        Divider()

                        Text("Description")
                            .font(.headline)
                        Text(job.description)
                            .foregroundColor(.secondary)

                        Divider()

                        Label("\(job.location.suburbName), \(job.location.state) \(job.location.postcode)",
                              systemImage: "mappin.circle")

                        if let start = job.preferredStartDate {
                            Label("Preferred start: \(start, style: .date)",
                                  systemImage: "calendar")
                        }

                        Divider()

                        Button {
                            // Submit quote action
                        } label: {
                            Text("Submit Quote")
                                .frame(maxWidth: .infinity)
                        }
                        .buttonStyle(.borderedProminent)
                    }
                    .padding()
                }
            } else if viewModel.isLoading {
                ProgressView()
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
            }
        }
        .navigationBarTitleDisplayMode(.inline)
        .task {
            await viewModel.loadJobDetail(id: jobId)
        }
    }
}

struct CreateJobView: View {
    var body: some View {
        NavigationStack {
            Text("Create Job - Coming Soon")
                .navigationTitle("Post a Job")
        }
    }
}

struct MessagesView: View {
    var body: some View {
        NavigationStack {
            Text("Messages - Coming Soon")
                .navigationTitle("Messages")
        }
    }
}

struct ProfileView: View {
    @EnvironmentObject var authViewModel: AuthViewModel

    var body: some View {
        NavigationStack {
            List {
                Section {
                    Button("Sign Out", role: .destructive) {
                        authViewModel.logout()
                    }
                }
            }
            .navigationTitle("Profile")
        }
    }
}
