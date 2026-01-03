namespace Quote.Application.Common.Interfaces;

public interface IGoogleMapsService
{
    Task<GeocodeResult?> GeocodeAddressAsync(string address, CancellationToken cancellationToken = default);
    Task<GeocodeResult?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
    Task<DistanceMatrixResult> GetDistanceMatrixAsync(
        (double Lat, double Lng) origin,
        (double Lat, double Lng) destination,
        CancellationToken cancellationToken = default);
    Task<DistanceMatrixResult> GetDistanceMatrixAsync(
        (double Lat, double Lng) origin,
        List<(double Lat, double Lng)> destinations,
        CancellationToken cancellationToken = default);
    Task<List<PlaceAutocompleteResult>> AutocompleteAddressAsync(
        string input,
        string? sessionToken = null,
        CancellationToken cancellationToken = default);
    Task<PlaceDetailsResult?> GetPlaceDetailsAsync(
        string placeId,
        CancellationToken cancellationToken = default);
}

public record GeocodeResult(
    string FormattedAddress,
    double Latitude,
    double Longitude,
    string? StreetNumber,
    string? Street,
    string? Suburb,
    string? State,
    string? Postcode,
    string? Country,
    string? PlaceId
);

public record DistanceMatrixResult(
    List<DistanceMatrixElement> Elements,
    string? OriginAddress,
    string? DestinationAddress
);

public record DistanceMatrixElement(
    int DistanceMeters,
    string DistanceText,
    int DurationSeconds,
    string DurationText,
    int? DurationInTrafficSeconds,
    string? DurationInTrafficText,
    string Status
);

public record PlaceAutocompleteResult(
    string PlaceId,
    string Description,
    string MainText,
    string SecondaryText
);

public record PlaceDetailsResult(
    string PlaceId,
    string Name,
    string FormattedAddress,
    double Latitude,
    double Longitude,
    string? PhoneNumber,
    string? Website,
    List<string>? Types,
    bool? IsOpenNow
);
