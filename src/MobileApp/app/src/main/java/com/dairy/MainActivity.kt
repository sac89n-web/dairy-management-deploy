package com.dairy

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.material3.*
import androidx.lifecycle.viewmodel.compose.viewModel

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            DairyApp()
        }
    }
}

@Composable
fun DairyApp() {
    val viewModel: MainViewModel = viewModel()
    Surface {
        when (viewModel.screen) {
            Screen.Login -> LoginScreen(viewModel)
            Screen.Dashboard -> DashboardScreen(viewModel)
            Screen.MilkCollection -> MilkCollectionScreen(viewModel)
        }
    }
}
