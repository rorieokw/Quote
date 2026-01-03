package com.quote.app.features.jobs.repository

import java.util.Date

data class Job(
    val id: String,
    val title: String,
    val description: String,
    val status: String,
    val tradeCategory: String,
    val budgetMin: Double?,
    val budgetMax: Double?,
    val preferredStartDate: Date?,
    val suburbName: String,
    val state: String,
    val postcode: String,
    val distanceKm: Double,
    val quoteCount: Int,
    val customerName: String,
    val createdAt: Date,
    val mediaUrls: List<String>
) {
    val budgetDisplay: String
        get() = when {
            budgetMin != null && budgetMax != null -> "$${budgetMin.toInt()} - $${budgetMax.toInt()}"
            budgetMin != null -> "From $${budgetMin.toInt()}"
            budgetMax != null -> "Up to $${budgetMax.toInt()}"
            else -> "Negotiable"
        }

    val locationDisplay: String
        get() = "$suburbName, $state $postcode"
}

data class JobDetail(
    val id: String,
    val title: String,
    val description: String,
    val status: String,
    val tradeCategory: TradeCategoryDto,
    val budgetMin: Double?,
    val budgetMax: Double?,
    val preferredStartDate: Date?,
    val preferredEndDate: Date?,
    val isFlexibleDates: Boolean,
    val location: LocationDto,
    val propertyType: String,
    val customer: CustomerDto,
    val media: List<JobMediaDto>,
    val quotes: List<QuoteSummaryDto>,
    val createdAt: Date
)

data class TradeCategoryDto(
    val id: String,
    val name: String,
    val icon: String?
)

data class LocationDto(
    val latitude: Double,
    val longitude: Double,
    val suburbName: String,
    val state: String,
    val postcode: String
)

data class CustomerDto(
    val id: String,
    val firstName: String,
    val profilePhotoUrl: String?
)

data class JobMediaDto(
    val id: String,
    val mediaUrl: String,
    val mediaType: String,
    val caption: String?,
    val thumbnailUrl: String?
)

data class QuoteSummaryDto(
    val id: String,
    val tradieId: String,
    val tradieName: String,
    val totalCost: Double,
    val status: String,
    val createdAt: Date
)

data class TradeCategory(
    val id: String,
    val name: String,
    val description: String?,
    val icon: String?
)

data class PaginatedResponse<T>(
    val items: List<T>,
    val pageNumber: Int,
    val totalPages: Int,
    val totalCount: Int,
    val hasPreviousPage: Boolean,
    val hasNextPage: Boolean
)
