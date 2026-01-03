namespace Quote.Shared.DTOs;

// Job clustering for map view
public record JobClusterDto(
    string SuburbName,
    string Postcode,
    double Latitude,
    double Longitude,
    int JobCount,
    int UrgentCount,
    decimal TotalBudgetMax
);

public record JobMapMarkerDto(
    Guid JobId,
    string Title,
    double Latitude,
    double Longitude,
    string SuburbName,
    string TradeCategoryName,
    decimal? BudgetMax,
    bool IsUrgent
);

// Travel time estimation
public record JobWithTravelDto(
    Guid Id,
    string Title,
    string Description,
    string SuburbName,
    string State,
    string Postcode,
    string TradeCategoryName,
    decimal? BudgetMin,
    decimal? BudgetMax,
    bool IsUrgent,
    string CustomerName,
    double DistanceKm,
    int EstimatedTravelMinutes,
    double? JobLatitude,
    double? JobLongitude
);

public record TravelInfoRequest(
    double FromLatitude,
    double FromLongitude
);

public record MapJobsResponse(
    List<JobClusterDto> Clusters,
    int TotalJobs,
    double CenterLat,
    double CenterLng
);

// Geocoding
public record GeocodeAddressRequest(string Address);

public record GeocodeAddressResponse(
    bool Success,
    string? FormattedAddress,
    double? Latitude,
    double? Longitude,
    string? Suburb,
    string? State,
    string? Postcode,
    string? PlaceId,
    string? ErrorMessage
);

public record ReverseGeocodeRequest(double Latitude, double Longitude);

// Distance Matrix
public record DistanceMatrixRequest(
    double OriginLatitude,
    double OriginLongitude,
    double DestinationLatitude,
    double DestinationLongitude
);

public record DistanceMatrixBatchRequest(
    double OriginLatitude,
    double OriginLongitude,
    List<DestinationPoint> Destinations
);

public record DestinationPoint(double Latitude, double Longitude, Guid? JobId = null);

public record DistanceMatrixResponse(
    int DistanceMeters,
    string DistanceText,
    int DurationSeconds,
    string DurationText,
    int? DurationInTrafficSeconds,
    string? DurationInTrafficText,
    string Status
);

public record DistanceMatrixBatchResponse(
    List<DistanceMatrixResponseItem> Results
);

public record DistanceMatrixResponseItem(
    Guid? JobId,
    double Latitude,
    double Longitude,
    int DistanceMeters,
    string DistanceText,
    int DurationSeconds,
    string DurationText,
    int? DurationInTrafficSeconds,
    string? DurationInTrafficText,
    string Status
);

// Places Autocomplete
public record PlaceAutocompleteRequest(
    string Input,
    string? SessionToken = null
);

public record PlaceAutocompleteResponse(
    List<PlaceAutocompleteItem> Predictions
);

public record PlaceAutocompleteItem(
    string PlaceId,
    string Description,
    string MainText,
    string SecondaryText
);

public record PlaceDetailsRequest(string PlaceId);

public record PlaceDetailsResponse(
    string PlaceId,
    string Name,
    string FormattedAddress,
    double Latitude,
    double Longitude,
    string? Suburb,
    string? State,
    string? Postcode
);

// Route Optimization
public record OptimizedRouteRequest(
    double StartLatitude,
    double StartLongitude,
    List<Guid> JobIds
);

public record OptimizedRouteResponse(
    List<RouteStopDto> Stops,
    int TotalDistanceMeters,
    string TotalDistanceText,
    int TotalDurationSeconds,
    string TotalDurationText,
    int TotalDurationInTrafficSeconds,
    string TotalDurationInTrafficText
);

public record RouteStopDto(
    int Order,
    Guid? JobId,
    string Address,
    double Latitude,
    double Longitude,
    int DistanceFromPreviousMeters,
    string DistanceFromPreviousText,
    int DurationFromPreviousSeconds,
    string DurationFromPreviousText,
    DateTime? EstimatedArrival
);
