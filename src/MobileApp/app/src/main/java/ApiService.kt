package com.dairy

import retrofit2.Response
import retrofit2.http.*

interface ApiService {
    @GET("collections")
    suspend fun getCollections(): Response<List<MilkCollection>>
    
    @POST("collections")
    suspend fun addCollection(@Body collection: MilkCollection): Response<MilkCollection>
    
    @GET("farmers")
    suspend fun getFarmers(): Response<List<Farmer>>
    
    @GET("shifts")
    suspend fun getShifts(): Response<List<Shift>>
}

data class MilkCollection(
    val id: Int = 0,
    val farmerId: Int,
    val shiftId: Int,
    val quantity: Double,
    val fatPercentage: Double,
    val ratePerLiter: Double,
    val totalAmount: Double,
    val collectionDate: String
)

data class Farmer(
    val id: Int,
    val name: String,
    val code: String
)

data class Shift(
    val id: Int,
    val name: String,
    val startTime: String,
    val endTime: String
)