package com.dairy

import androidx.compose.runtime.*
import androidx.compose.material3.*

@Composable
fun MilkCollectionScreen(viewModel: MainViewModel) {
    var qty by remember { mutableStateOf("") }
    var fat by remember { mutableStateOf("") }
    var price by remember { mutableStateOf("") }
    Column {
        Text("Milk Collection Entry")
        TextField(value = qty, onValueChange = { qty = it }, label = { Text("Quantity (Ltr)") })
        TextField(value = fat, onValueChange = { fat = it }, label = { Text("Fat %") })
        TextField(value = price, onValueChange = { price = it }, label = { Text("Price/Ltr") })
        Button(onClick = { /* TODO: Call API to submit */ }) {
            Text("Submit")
        }
    }
}
