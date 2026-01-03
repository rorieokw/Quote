package com.quote.app.features.jobs.ui

import androidx.compose.foundation.layout.*
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MainScreen(onLogout: () -> Unit) {
    val navController = rememberNavController()
    var selectedTab by remember { mutableIntStateOf(0) }

    val tabs = listOf(
        TabItem("Jobs", Icons.Default.Work, "jobs"),
        TabItem("Post", Icons.Default.Add, "post"),
        TabItem("Messages", Icons.Default.Email, "messages"),
        TabItem("Profile", Icons.Default.Person, "profile")
    )

    Scaffold(
        bottomBar = {
            NavigationBar {
                tabs.forEachIndexed { index, tab ->
                    NavigationBarItem(
                        selected = selectedTab == index,
                        onClick = {
                            selectedTab = index
                            navController.navigate(tab.route) {
                                popUpTo(navController.graph.startDestinationId)
                                launchSingleTop = true
                            }
                        },
                        icon = { Icon(tab.icon, contentDescription = tab.label) },
                        label = { Text(tab.label) }
                    )
                }
            }
        }
    ) { padding ->
        NavHost(
            navController = navController,
            startDestination = "jobs",
            modifier = Modifier.padding(padding)
        ) {
            composable("jobs") {
                JobsListScreen()
            }
            composable("post") {
                CreateJobScreen()
            }
            composable("messages") {
                MessagesScreen()
            }
            composable("profile") {
                ProfileScreen(onLogout = onLogout)
            }
        }
    }
}

data class TabItem(
    val label: String,
    val icon: androidx.compose.ui.graphics.vector.ImageVector,
    val route: String
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun JobsListScreen() {
    Scaffold(
        topBar = {
            TopAppBar(title = { Text("Jobs") })
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            Text("Jobs list - Coming soon")
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CreateJobScreen() {
    Scaffold(
        topBar = {
            TopAppBar(title = { Text("Post a Job") })
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            Text("Create job form - Coming soon")
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun MessagesScreen() {
    Scaffold(
        topBar = {
            TopAppBar(title = { Text("Messages") })
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            Text("Messages - Coming soon")
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ProfileScreen(onLogout: () -> Unit) {
    Scaffold(
        topBar = {
            TopAppBar(title = { Text("Profile") })
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
        ) {
            Button(onClick = onLogout) {
                Text("Sign Out")
            }
        }
    }
}
