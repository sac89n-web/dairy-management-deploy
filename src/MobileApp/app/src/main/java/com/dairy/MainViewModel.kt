package com.dairy

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.launch

enum class Screen { Login, Dashboard, MilkCollection }

class MainViewModel : ViewModel() {
    var screen: Screen = Screen.Login
    var token: String? = null
    var error: String? = null
    var dashboardStats: DashboardStats? = null

    fun login(username: String, password: String) {
        viewModelScope.launch {
            // TODO: Call API via Retrofit
            if (username == "collector" && password == "password") {
                token = "sample.jwt.token"
                screen = Screen.Dashboard
            } else {
                error = "Invalid credentials"
            }
        }
    }

    fun loadDashboard() {
        viewModelScope.launch {
            // TODO: Call API via Retrofit with JWT
            dashboardStats = DashboardStats(10, 20, 2, 5000.0, "4000/1000", 1200.0, 1100.0)
        }
    }
}

data class DashboardStats(
    val totalFarmers: Int,
    val totalCustomers: Int,
    val totalCollectors: Int,
    val todaysSales: Double,
    val paidVsDue: String,
    val totalMilkCollected: Double,
    val totalMilkSold: Double
)
