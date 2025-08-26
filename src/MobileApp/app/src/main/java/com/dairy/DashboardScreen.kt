package com.dairy

import androidx.compose.runtime.*
import androidx.compose.material3.*

@Composable
fun DashboardScreen(viewModel: MainViewModel) {
    val stats = viewModel.dashboardStats
    LaunchedEffect(Unit) { viewModel.loadDashboard() }
    Column {
        Text("Dashboard")
        stats?.let {
            Text("Total Farmers: ${it.totalFarmers}")
            Text("Total Customers: ${it.totalCustomers}")
            Text("Total Collectors: ${it.totalCollectors}")
            Text("Today's Sales: ${it.todaysSales}")
            Text("Paid vs Due: ${it.paidVsDue}")
            Text("Total Milk Collected: ${it.totalMilkCollected}")
            Text("Total Milk Sold: ${it.totalMilkSold}")
        }
    }
}
