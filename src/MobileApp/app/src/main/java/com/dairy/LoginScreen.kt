package com.dairy

import androidx.compose.runtime.*
import androidx.compose.material3.*

@Composable
fun LoginScreen(viewModel: MainViewModel) {
    var username by remember { mutableStateOf("") }
    var password by remember { mutableStateOf("") }
    Column {
        Text("Login")
        TextField(value = username, onValueChange = { username = it }, label = { Text("Username") })
        TextField(value = password, onValueChange = { password = it }, label = { Text("Password") })
        Button(onClick = { viewModel.login(username, password) }) {
            Text("Login")
        }
        viewModel.error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
    }
}
