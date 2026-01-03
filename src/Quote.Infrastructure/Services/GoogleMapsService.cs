using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quote.Application.Common.Interfaces;

namespace Quote.Infrastructure.Services;

public class GoogleMapsService : IGoogleMapsService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GoogleMapsService> _logger;
    private readonly string _apiKey;
    private readonly bool _useRealApi;
    private readonly int _cacheDurationSeconds;

    private const string GeocodingBaseUrl = "https://maps.googleapis.com/maps/api/geocode/json";
    private const string DistanceMatrixBaseUrl = "https://maps.googleapis.com/maps/api/distancematrix/json";
    private const string PlacesAutocompleteBaseUrl = "https://maps.googleapis.com/maps/api/place/autocomplete/json";
    private const string PlaceDetailsBaseUrl = "https://maps.googleapis.com/maps/api/place/details/json";

    public GoogleMapsService(
        HttpClient httpClient,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<GoogleMapsService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _apiKey = configuration["GoogleMaps:ApiKey"] ?? "";
        _useRealApi = !string.IsNullOrEmpty(_apiKey) && configuration.GetValue<bool>("GoogleMaps:EnableRealApi");
        _cacheDurationSeconds = configuration.GetValue<int>("GoogleMaps:CacheDistanceMatrixSeconds", 86400);
    }

    public async Task<GeocodeResult?> GeocodeAddressAsync(string address, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"geocode:{address}";
        if (_cache.TryGetValue(cacheKey, out GeocodeResult? cached))
        {
            return cached;
        }

        if (!_useRealApi)
        {
            // Mock response for development
            return GetMockGeocodeResult(address);
        }

        try
        {
            var url = $"{GeocodingBaseUrl}?address={Uri.EscapeDataString(address)}&key={_apiKey}&region=au";
            var response = await _httpClient.GetFromJsonAsync<GoogleGeocodeResponse>(url, cancellationToken);

            if (response?.Results?.Any() == true)
            {
                var result = ParseGeocodeResult(response.Results.First());
                _cache.Set(cacheKey, result, TimeSpan.FromSeconds(_cacheDurationSeconds));
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding address: {Address}", address);
        }

        return null;
    }

    public async Task<GeocodeResult?> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"reverse_geocode:{latitude:F6},{longitude:F6}";
        if (_cache.TryGetValue(cacheKey, out GeocodeResult? cached))
        {
            return cached;
        }

        if (!_useRealApi)
        {
            return GetMockReverseGeocodeResult(latitude, longitude);
        }

        try
        {
            var url = $"{GeocodingBaseUrl}?latlng={latitude},{longitude}&key={_apiKey}";
            var response = await _httpClient.GetFromJsonAsync<GoogleGeocodeResponse>(url, cancellationToken);

            if (response?.Results?.Any() == true)
            {
                var result = ParseGeocodeResult(response.Results.First());
                _cache.Set(cacheKey, result, TimeSpan.FromSeconds(_cacheDurationSeconds));
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reverse geocoding: {Lat}, {Lng}", latitude, longitude);
        }

        return null;
    }

    public async Task<DistanceMatrixResult> GetDistanceMatrixAsync(
        (double Lat, double Lng) origin,
        (double Lat, double Lng) destination,
        CancellationToken cancellationToken = default)
    {
        return await GetDistanceMatrixAsync(origin, new List<(double, double)> { destination }, cancellationToken);
    }

    public async Task<DistanceMatrixResult> GetDistanceMatrixAsync(
        (double Lat, double Lng) origin,
        List<(double Lat, double Lng)> destinations,
        CancellationToken cancellationToken = default)
    {
        var destinationsStr = string.Join("|", destinations.Select(d => $"{d.Lat},{d.Lng}"));
        var cacheKey = $"distance_matrix:{origin.Lat:F6},{origin.Lng:F6}:{destinationsStr}";

        if (_cache.TryGetValue(cacheKey, out DistanceMatrixResult? cached))
        {
            return cached!;
        }

        if (!_useRealApi)
        {
            return GetMockDistanceMatrixResult(origin, destinations);
        }

        try
        {
            var url = $"{DistanceMatrixBaseUrl}?origins={origin.Lat},{origin.Lng}&destinations={Uri.EscapeDataString(destinationsStr)}&key={_apiKey}&departure_time=now";
            var response = await _httpClient.GetFromJsonAsync<GoogleDistanceMatrixResponse>(url, cancellationToken);

            if (response?.Rows?.Any() == true)
            {
                var result = ParseDistanceMatrixResult(response);
                _cache.Set(cacheKey, result, TimeSpan.FromSeconds(_cacheDurationSeconds));
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting distance matrix");
        }

        // Return mock result as fallback
        return GetMockDistanceMatrixResult(origin, destinations);
    }

    public async Task<List<PlaceAutocompleteResult>> AutocompleteAddressAsync(
        string input,
        string? sessionToken = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input) || input.Length < 3)
        {
            return new List<PlaceAutocompleteResult>();
        }

        if (!_useRealApi)
        {
            return GetMockAutocompleteResults(input);
        }

        try
        {
            var url = $"{PlacesAutocompleteBaseUrl}?input={Uri.EscapeDataString(input)}&types=address&components=country:au&key={_apiKey}";
            if (!string.IsNullOrEmpty(sessionToken))
            {
                url += $"&sessiontoken={sessionToken}";
            }

            var response = await _httpClient.GetFromJsonAsync<GoogleAutocompleteResponse>(url, cancellationToken);

            if (response?.Predictions?.Any() == true)
            {
                return response.Predictions.Select(p => new PlaceAutocompleteResult(
                    PlaceId: p.PlaceId ?? "",
                    Description: p.Description ?? "",
                    MainText: p.StructuredFormatting?.MainText ?? "",
                    SecondaryText: p.StructuredFormatting?.SecondaryText ?? ""
                )).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error autocompleting address: {Input}", input);
        }

        return new List<PlaceAutocompleteResult>();
    }

    public async Task<PlaceDetailsResult?> GetPlaceDetailsAsync(
        string placeId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"place_details:{placeId}";
        if (_cache.TryGetValue(cacheKey, out PlaceDetailsResult? cached))
        {
            return cached;
        }

        if (!_useRealApi)
        {
            return GetMockPlaceDetails(placeId);
        }

        try
        {
            var url = $"{PlaceDetailsBaseUrl}?place_id={placeId}&fields=place_id,name,formatted_address,geometry,formatted_phone_number,website,types,opening_hours&key={_apiKey}";
            var response = await _httpClient.GetFromJsonAsync<GooglePlaceDetailsResponse>(url, cancellationToken);

            if (response?.Result != null)
            {
                var result = new PlaceDetailsResult(
                    PlaceId: response.Result.PlaceId ?? "",
                    Name: response.Result.Name ?? "",
                    FormattedAddress: response.Result.FormattedAddress ?? "",
                    Latitude: response.Result.Geometry?.Location?.Lat ?? 0,
                    Longitude: response.Result.Geometry?.Location?.Lng ?? 0,
                    PhoneNumber: response.Result.FormattedPhoneNumber,
                    Website: response.Result.Website,
                    Types: response.Result.Types,
                    IsOpenNow: response.Result.OpeningHours?.OpenNow
                );
                _cache.Set(cacheKey, result, TimeSpan.FromSeconds(_cacheDurationSeconds));
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting place details: {PlaceId}", placeId);
        }

        return null;
    }

    #region Private Helpers

    private GeocodeResult ParseGeocodeResult(GoogleGeocodeResultItem item)
    {
        string? GetComponent(string type) => item.AddressComponents?
            .FirstOrDefault(c => c.Types?.Contains(type) == true)?.LongName;

        return new GeocodeResult(
            FormattedAddress: item.FormattedAddress ?? "",
            Latitude: item.Geometry?.Location?.Lat ?? 0,
            Longitude: item.Geometry?.Location?.Lng ?? 0,
            StreetNumber: GetComponent("street_number"),
            Street: GetComponent("route"),
            Suburb: GetComponent("locality") ?? GetComponent("sublocality"),
            State: GetComponent("administrative_area_level_1"),
            Postcode: GetComponent("postal_code"),
            Country: GetComponent("country"),
            PlaceId: item.PlaceId
        );
    }

    private DistanceMatrixResult ParseDistanceMatrixResult(GoogleDistanceMatrixResponse response)
    {
        var elements = response.Rows?.FirstOrDefault()?.Elements?.Select(e => new DistanceMatrixElement(
            DistanceMeters: e.Distance?.Value ?? 0,
            DistanceText: e.Distance?.Text ?? "",
            DurationSeconds: e.Duration?.Value ?? 0,
            DurationText: e.Duration?.Text ?? "",
            DurationInTrafficSeconds: e.DurationInTraffic?.Value,
            DurationInTrafficText: e.DurationInTraffic?.Text,
            Status: e.Status ?? "UNKNOWN"
        )).ToList() ?? new List<DistanceMatrixElement>();

        return new DistanceMatrixResult(
            Elements: elements,
            OriginAddress: response.OriginAddresses?.FirstOrDefault(),
            DestinationAddress: response.DestinationAddresses?.FirstOrDefault()
        );
    }

    #endregion

    #region Mock Responses

    private GeocodeResult GetMockGeocodeResult(string address)
    {
        // Parse address to extract suburb if possible
        var parts = address.Split(',').Select(p => p.Trim()).ToArray();
        var suburb = parts.Length > 1 ? parts[0] : "Sydney";
        var state = parts.Length > 2 ? parts[^2] : "NSW";

        // Generate consistent coordinates based on address hash
        var hash = address.GetHashCode();
        var baseLat = -33.8688; // Sydney
        var baseLng = 151.2093;
        var latOffset = (hash % 1000) / 10000.0;
        var lngOffset = ((hash >> 10) % 1000) / 10000.0;

        return new GeocodeResult(
            FormattedAddress: address,
            Latitude: baseLat + latOffset,
            Longitude: baseLng + lngOffset,
            StreetNumber: "123",
            Street: "Test Street",
            Suburb: suburb,
            State: state,
            Postcode: "2000",
            Country: "Australia",
            PlaceId: $"mock_place_{Math.Abs(hash)}"
        );
    }

    private GeocodeResult GetMockReverseGeocodeResult(double latitude, double longitude)
    {
        var suburbs = new[] { "Surry Hills", "Bondi", "Newtown", "Parramatta", "Chatswood" };
        var index = Math.Abs((int)(latitude * 1000) + (int)(longitude * 1000)) % suburbs.Length;

        return new GeocodeResult(
            FormattedAddress: $"123 Test Street, {suburbs[index]} NSW 2000",
            Latitude: latitude,
            Longitude: longitude,
            StreetNumber: "123",
            Street: "Test Street",
            Suburb: suburbs[index],
            State: "NSW",
            Postcode: "2000",
            Country: "Australia",
            PlaceId: $"mock_reverse_{latitude:F4}_{longitude:F4}"
        );
    }

    private DistanceMatrixResult GetMockDistanceMatrixResult(
        (double Lat, double Lng) origin,
        List<(double Lat, double Lng)> destinations)
    {
        var elements = destinations.Select(dest =>
        {
            // Calculate approximate distance using Haversine formula
            var distanceKm = CalculateHaversineDistance(origin.Lat, origin.Lng, dest.Lat, dest.Lng);
            var distanceMeters = (int)(distanceKm * 1000);

            // Estimate driving time: ~40 km/h average in urban areas with traffic
            var durationSeconds = (int)(distanceKm / 40.0 * 3600);
            var durationWithTrafficSeconds = (int)(durationSeconds * 1.3); // 30% traffic delay

            return new DistanceMatrixElement(
                DistanceMeters: distanceMeters,
                DistanceText: distanceKm >= 1 ? $"{distanceKm:F1} km" : $"{distanceMeters} m",
                DurationSeconds: durationSeconds,
                DurationText: FormatDuration(durationSeconds),
                DurationInTrafficSeconds: durationWithTrafficSeconds,
                DurationInTrafficText: FormatDuration(durationWithTrafficSeconds),
                Status: "OK"
            );
        }).ToList();

        return new DistanceMatrixResult(
            Elements: elements,
            OriginAddress: null,
            DestinationAddress: null
        );
    }

    private List<PlaceAutocompleteResult> GetMockAutocompleteResults(string input)
    {
        var australianSuburbs = new[]
        {
            ("Sydney CBD, NSW", "Sydney CBD", "New South Wales, Australia"),
            ("Surry Hills, NSW", "Surry Hills", "Sydney NSW, Australia"),
            ("Bondi Beach, NSW", "Bondi Beach", "Sydney NSW, Australia"),
            ("Parramatta, NSW", "Parramatta", "Sydney NSW, Australia"),
            ("Melbourne, VIC", "Melbourne", "Victoria, Australia"),
            ("Brisbane, QLD", "Brisbane", "Queensland, Australia"),
            ("Perth, WA", "Perth", "Western Australia"),
            ("Adelaide, SA", "Adelaide", "South Australia")
        };

        return australianSuburbs
            .Where(s => s.Item1.Contains(input, StringComparison.OrdinalIgnoreCase) ||
                       s.Item2.Contains(input, StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .Select((s, i) => new PlaceAutocompleteResult(
                PlaceId: $"mock_place_{i}_{s.Item2.GetHashCode()}",
                Description: s.Item1,
                MainText: s.Item2,
                SecondaryText: s.Item3
            ))
            .ToList();
    }

    private PlaceDetailsResult GetMockPlaceDetails(string placeId)
    {
        return new PlaceDetailsResult(
            PlaceId: placeId,
            Name: "Mock Location",
            FormattedAddress: "123 Test Street, Sydney NSW 2000, Australia",
            Latitude: -33.8688,
            Longitude: 151.2093,
            PhoneNumber: "+61 2 1234 5678",
            Website: null,
            Types: new List<string> { "street_address" },
            IsOpenNow: null
        );
    }

    private static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth's radius in km
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private static string FormatDuration(int seconds)
    {
        if (seconds < 60) return $"{seconds} secs";
        if (seconds < 3600) return $"{seconds / 60} mins";
        var hours = seconds / 3600;
        var mins = (seconds % 3600) / 60;
        return mins > 0 ? $"{hours} hr {mins} mins" : $"{hours} hr";
    }

    #endregion

    #region Google API Response DTOs

    private class GoogleGeocodeResponse
    {
        [JsonPropertyName("results")]
        public List<GoogleGeocodeResultItem>? Results { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    private class GoogleGeocodeResultItem
    {
        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; set; }

        [JsonPropertyName("geometry")]
        public GoogleGeometry? Geometry { get; set; }

        [JsonPropertyName("address_components")]
        public List<GoogleAddressComponent>? AddressComponents { get; set; }

        [JsonPropertyName("place_id")]
        public string? PlaceId { get; set; }
    }

    private class GoogleGeometry
    {
        [JsonPropertyName("location")]
        public GoogleLatLng? Location { get; set; }
    }

    private class GoogleLatLng
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }

    private class GoogleAddressComponent
    {
        [JsonPropertyName("long_name")]
        public string? LongName { get; set; }

        [JsonPropertyName("short_name")]
        public string? ShortName { get; set; }

        [JsonPropertyName("types")]
        public List<string>? Types { get; set; }
    }

    private class GoogleDistanceMatrixResponse
    {
        [JsonPropertyName("rows")]
        public List<GoogleDistanceMatrixRow>? Rows { get; set; }

        [JsonPropertyName("origin_addresses")]
        public List<string>? OriginAddresses { get; set; }

        [JsonPropertyName("destination_addresses")]
        public List<string>? DestinationAddresses { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    private class GoogleDistanceMatrixRow
    {
        [JsonPropertyName("elements")]
        public List<GoogleDistanceMatrixElementItem>? Elements { get; set; }
    }

    private class GoogleDistanceMatrixElementItem
    {
        [JsonPropertyName("distance")]
        public GoogleTextValue? Distance { get; set; }

        [JsonPropertyName("duration")]
        public GoogleTextValue? Duration { get; set; }

        [JsonPropertyName("duration_in_traffic")]
        public GoogleTextValue? DurationInTraffic { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    private class GoogleTextValue
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("value")]
        public int Value { get; set; }
    }

    private class GoogleAutocompleteResponse
    {
        [JsonPropertyName("predictions")]
        public List<GooglePrediction>? Predictions { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    private class GooglePrediction
    {
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("place_id")]
        public string? PlaceId { get; set; }

        [JsonPropertyName("structured_formatting")]
        public GoogleStructuredFormatting? StructuredFormatting { get; set; }
    }

    private class GoogleStructuredFormatting
    {
        [JsonPropertyName("main_text")]
        public string? MainText { get; set; }

        [JsonPropertyName("secondary_text")]
        public string? SecondaryText { get; set; }
    }

    private class GooglePlaceDetailsResponse
    {
        [JsonPropertyName("result")]
        public GooglePlaceResult? Result { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    private class GooglePlaceResult
    {
        [JsonPropertyName("place_id")]
        public string? PlaceId { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("formatted_address")]
        public string? FormattedAddress { get; set; }

        [JsonPropertyName("geometry")]
        public GoogleGeometry? Geometry { get; set; }

        [JsonPropertyName("formatted_phone_number")]
        public string? FormattedPhoneNumber { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("types")]
        public List<string>? Types { get; set; }

        [JsonPropertyName("opening_hours")]
        public GoogleOpeningHours? OpeningHours { get; set; }
    }

    private class GoogleOpeningHours
    {
        [JsonPropertyName("open_now")]
        public bool? OpenNow { get; set; }
    }

    #endregion
}
