using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Domain.Enums;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MapsController : ControllerBase
{
    private readonly IGoogleMapsService _mapsService;
    private readonly IApplicationDbContext _context;

    public MapsController(IGoogleMapsService mapsService, IApplicationDbContext context)
    {
        _mapsService = mapsService;
        _context = context;
    }

    [HttpPost("geocode")]
    public async Task<ActionResult<GeocodeAddressResponse>> GeocodeAddress(
        [FromBody] GeocodeAddressRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Address))
        {
            return BadRequest(new GeocodeAddressResponse(
                Success: false,
                FormattedAddress: null,
                Latitude: null,
                Longitude: null,
                Suburb: null,
                State: null,
                Postcode: null,
                PlaceId: null,
                ErrorMessage: "Address is required"
            ));
        }

        var result = await _mapsService.GeocodeAddressAsync(request.Address, cancellationToken);

        if (result == null)
        {
            return Ok(new GeocodeAddressResponse(
                Success: false,
                FormattedAddress: null,
                Latitude: null,
                Longitude: null,
                Suburb: null,
                State: null,
                Postcode: null,
                PlaceId: null,
                ErrorMessage: "Could not geocode address"
            ));
        }

        return Ok(new GeocodeAddressResponse(
            Success: true,
            FormattedAddress: result.FormattedAddress,
            Latitude: result.Latitude,
            Longitude: result.Longitude,
            Suburb: result.Suburb,
            State: result.State,
            Postcode: result.Postcode,
            PlaceId: result.PlaceId,
            ErrorMessage: null
        ));
    }

    [HttpPost("reverse-geocode")]
    public async Task<ActionResult<GeocodeAddressResponse>> ReverseGeocode(
        [FromBody] ReverseGeocodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mapsService.ReverseGeocodeAsync(request.Latitude, request.Longitude, cancellationToken);

        if (result == null)
        {
            return Ok(new GeocodeAddressResponse(
                Success: false,
                FormattedAddress: null,
                Latitude: null,
                Longitude: null,
                Suburb: null,
                State: null,
                Postcode: null,
                PlaceId: null,
                ErrorMessage: "Could not reverse geocode coordinates"
            ));
        }

        return Ok(new GeocodeAddressResponse(
            Success: true,
            FormattedAddress: result.FormattedAddress,
            Latitude: result.Latitude,
            Longitude: result.Longitude,
            Suburb: result.Suburb,
            State: result.State,
            Postcode: result.Postcode,
            PlaceId: result.PlaceId,
            ErrorMessage: null
        ));
    }

    [HttpPost("distance")]
    public async Task<ActionResult<DistanceMatrixResponse>> GetDistance(
        [FromBody] DistanceMatrixRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mapsService.GetDistanceMatrixAsync(
            (request.OriginLatitude, request.OriginLongitude),
            (request.DestinationLatitude, request.DestinationLongitude),
            cancellationToken);

        if (!result.Elements.Any())
        {
            return Ok(new DistanceMatrixResponse(
                DistanceMeters: 0,
                DistanceText: "",
                DurationSeconds: 0,
                DurationText: "",
                DurationInTrafficSeconds: null,
                DurationInTrafficText: null,
                Status: "ZERO_RESULTS"
            ));
        }

        var element = result.Elements.First();
        return Ok(new DistanceMatrixResponse(
            DistanceMeters: element.DistanceMeters,
            DistanceText: element.DistanceText,
            DurationSeconds: element.DurationSeconds,
            DurationText: element.DurationText,
            DurationInTrafficSeconds: element.DurationInTrafficSeconds,
            DurationInTrafficText: element.DurationInTrafficText,
            Status: element.Status
        ));
    }

    [HttpPost("distance/batch")]
    public async Task<ActionResult<DistanceMatrixBatchResponse>> GetDistanceBatch(
        [FromBody] DistanceMatrixBatchRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.Destinations.Any())
        {
            return Ok(new DistanceMatrixBatchResponse(new List<DistanceMatrixResponseItem>()));
        }

        var destinations = request.Destinations
            .Select(d => (d.Latitude, d.Longitude))
            .ToList();

        var result = await _mapsService.GetDistanceMatrixAsync(
            (request.OriginLatitude, request.OriginLongitude),
            destinations,
            cancellationToken);

        var items = result.Elements.Select((e, i) => new DistanceMatrixResponseItem(
            JobId: i < request.Destinations.Count ? request.Destinations[i].JobId : null,
            Latitude: i < request.Destinations.Count ? request.Destinations[i].Latitude : 0,
            Longitude: i < request.Destinations.Count ? request.Destinations[i].Longitude : 0,
            DistanceMeters: e.DistanceMeters,
            DistanceText: e.DistanceText,
            DurationSeconds: e.DurationSeconds,
            DurationText: e.DurationText,
            DurationInTrafficSeconds: e.DurationInTrafficSeconds,
            DurationInTrafficText: e.DurationInTrafficText,
            Status: e.Status
        )).ToList();

        return Ok(new DistanceMatrixBatchResponse(items));
    }

    [HttpGet("autocomplete")]
    public async Task<ActionResult<PlaceAutocompleteResponse>> Autocomplete(
        [FromQuery] string input,
        [FromQuery] string? sessionToken = null,
        CancellationToken cancellationToken = default)
    {
        var results = await _mapsService.AutocompleteAddressAsync(input, sessionToken, cancellationToken);

        return Ok(new PlaceAutocompleteResponse(
            results.Select(r => new PlaceAutocompleteItem(
                PlaceId: r.PlaceId,
                Description: r.Description,
                MainText: r.MainText,
                SecondaryText: r.SecondaryText
            )).ToList()
        ));
    }

    [HttpGet("place/{placeId}")]
    public async Task<ActionResult<PlaceDetailsResponse>> GetPlaceDetails(
        string placeId,
        CancellationToken cancellationToken)
    {
        var result = await _mapsService.GetPlaceDetailsAsync(placeId, cancellationToken);

        if (result == null)
        {
            return NotFound();
        }

        // Try to extract suburb, state, postcode from address
        var geocode = await _mapsService.ReverseGeocodeAsync(result.Latitude, result.Longitude, cancellationToken);

        return Ok(new PlaceDetailsResponse(
            PlaceId: result.PlaceId,
            Name: result.Name,
            FormattedAddress: result.FormattedAddress,
            Latitude: result.Latitude,
            Longitude: result.Longitude,
            Suburb: geocode?.Suburb,
            State: geocode?.State,
            Postcode: geocode?.Postcode
        ));
    }

    [Authorize]
    [HttpPost("optimize-route")]
    public async Task<ActionResult<OptimizedRouteResponse>> OptimizeRoute(
        [FromBody] OptimizedRouteRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.JobIds.Any())
        {
            return BadRequest("At least one job is required");
        }

        // Get job locations
        var jobs = await _context.Jobs
            .Where(j => request.JobIds.Contains(j.Id))
            .Select(j => new { j.Id, j.Latitude, j.Longitude, j.SuburbName, j.State })
            .ToListAsync(cancellationToken);

        if (!jobs.Any())
        {
            return NotFound("No jobs found");
        }

        // Use greedy nearest neighbor algorithm for route optimization
        var stops = new List<RouteStopDto>();
        var visited = new HashSet<Guid>();
        var currentLat = request.StartLatitude;
        var currentLng = request.StartLongitude;
        var order = 0;
        var totalDistance = 0;
        var totalDuration = 0;
        var totalDurationInTraffic = 0;
        var currentTime = DateTime.Now;

        // Add starting point
        stops.Add(new RouteStopDto(
            Order: order++,
            JobId: null,
            Address: "Starting Location",
            Latitude: currentLat,
            Longitude: currentLng,
            DistanceFromPreviousMeters: 0,
            DistanceFromPreviousText: "",
            DurationFromPreviousSeconds: 0,
            DurationFromPreviousText: "",
            EstimatedArrival: currentTime
        ));

        while (visited.Count < jobs.Count)
        {
            // Find nearest unvisited job
            var unvisited = jobs.Where(j => !visited.Contains(j.Id)).ToList();
            var destinations = unvisited.Select(j => (j.Latitude, j.Longitude)).ToList();

            var distances = await _mapsService.GetDistanceMatrixAsync(
                (currentLat, currentLng),
                destinations,
                cancellationToken);

            var nearestIndex = 0;
            var nearestDuration = int.MaxValue;

            for (int i = 0; i < distances.Elements.Count; i++)
            {
                var duration = distances.Elements[i].DurationInTrafficSeconds ?? distances.Elements[i].DurationSeconds;
                if (duration < nearestDuration)
                {
                    nearestDuration = duration;
                    nearestIndex = i;
                }
            }

            var nearestJob = unvisited[nearestIndex];
            var nearestElement = distances.Elements[nearestIndex];

            visited.Add(nearestJob.Id);
            currentLat = nearestJob.Latitude;
            currentLng = nearestJob.Longitude;
            totalDistance += nearestElement.DistanceMeters;
            totalDuration += nearestElement.DurationSeconds;
            totalDurationInTraffic += nearestElement.DurationInTrafficSeconds ?? nearestElement.DurationSeconds;
            currentTime = currentTime.AddSeconds(nearestElement.DurationInTrafficSeconds ?? nearestElement.DurationSeconds);

            stops.Add(new RouteStopDto(
                Order: order++,
                JobId: nearestJob.Id,
                Address: $"{nearestJob.SuburbName}, {nearestJob.State}",
                Latitude: nearestJob.Latitude,
                Longitude: nearestJob.Longitude,
                DistanceFromPreviousMeters: nearestElement.DistanceMeters,
                DistanceFromPreviousText: nearestElement.DistanceText,
                DurationFromPreviousSeconds: nearestElement.DurationSeconds,
                DurationFromPreviousText: nearestElement.DurationText,
                EstimatedArrival: currentTime
            ));
        }

        return Ok(new OptimizedRouteResponse(
            Stops: stops,
            TotalDistanceMeters: totalDistance,
            TotalDistanceText: FormatDistance(totalDistance),
            TotalDurationSeconds: totalDuration,
            TotalDurationText: FormatDuration(totalDuration),
            TotalDurationInTrafficSeconds: totalDurationInTraffic,
            TotalDurationInTrafficText: FormatDuration(totalDurationInTraffic)
        ));
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<MapJobsResponse>> GetJobsForMap(
        [FromQuery] double? lat,
        [FromQuery] double? lng,
        [FromQuery] int? radiusKm,
        [FromQuery] Guid? tradeCategoryId,
        CancellationToken cancellationToken)
    {
        var query = _context.Jobs
            .Include(j => j.TradeCategory)
            .Where(j => j.Status == JobStatus.Open)
            .AsQueryable();

        if (tradeCategoryId.HasValue)
        {
            query = query.Where(j => j.TradeCategoryId == tradeCategoryId.Value);
        }

        var jobs = await query.ToListAsync(cancellationToken);

        // Filter by radius if origin provided
        if (lat.HasValue && lng.HasValue && radiusKm.HasValue)
        {
            jobs = jobs.Where(j =>
                CalculateDistance(lat.Value, lng.Value, j.Latitude, j.Longitude) <= radiusKm.Value
            ).ToList();
        }

        // Group by suburb for clustering
        var clusters = jobs
            .GroupBy(j => new { j.SuburbName, j.Postcode })
            .Select(g => new JobClusterDto(
                SuburbName: g.Key.SuburbName,
                Postcode: g.Key.Postcode,
                Latitude: g.Average(j => j.Latitude),
                Longitude: g.Average(j => j.Longitude),
                JobCount: g.Count(),
                UrgentCount: g.Count(j => j.PreferredStartDate.HasValue && j.PreferredStartDate.Value <= DateTime.UtcNow.AddDays(3)),
                TotalBudgetMax: g.Sum(j => j.BudgetMax ?? 0)
            ))
            .ToList();

        var centerLat = clusters.Any() ? clusters.Average(c => c.Latitude) : lat ?? -33.8688;
        var centerLng = clusters.Any() ? clusters.Average(c => c.Longitude) : lng ?? 151.2093;

        return Ok(new MapJobsResponse(
            Clusters: clusters,
            TotalJobs: jobs.Count,
            CenterLat: centerLat,
            CenterLng: centerLng
        ));
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private static string FormatDistance(int meters)
    {
        if (meters >= 1000)
            return $"{meters / 1000.0:F1} km";
        return $"{meters} m";
    }

    private static string FormatDuration(int seconds)
    {
        if (seconds < 60) return $"{seconds} secs";
        if (seconds < 3600) return $"{seconds / 60} mins";
        var hours = seconds / 3600;
        var mins = (seconds % 3600) / 60;
        return mins > 0 ? $"{hours} hr {mins} mins" : $"{hours} hr";
    }
}
