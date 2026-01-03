import SwiftUI

@main
struct QuoteApp: App {
    @StateObject private var authViewModel = AuthViewModel()

    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(authViewModel)
        }
    }
}

struct ContentView: View {
    @EnvironmentObject var authViewModel: AuthViewModel

    var body: some View {
        Group {
            if authViewModel.isAuthenticated {
                MainTabView()
            } else {
                LoginView()
            }
        }
    }
}

struct MainTabView: View {
    var body: some View {
        TabView {
            JobsListView()
                .tabItem {
                    Label("Jobs", systemImage: "briefcase")
                }

            CreateJobView()
                .tabItem {
                    Label("Post Job", systemImage: "plus.circle")
                }

            MessagesView()
                .tabItem {
                    Label("Messages", systemImage: "message")
                }

            ProfileView()
                .tabItem {
                    Label("Profile", systemImage: "person")
                }
        }
    }
}
