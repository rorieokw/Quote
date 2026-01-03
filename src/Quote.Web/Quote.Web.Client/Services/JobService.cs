using System.Net.Http.Json;
using System.Net.Http.Headers;
using Quote.Shared.DTOs;

namespace Quote.Web.Client.Services;

public class JobService
{
    private readonly HttpClient _httpClient;
    private readonly AuthStateService _authState;

    public JobService(HttpClient httpClient, AuthStateService authState)
    {
        _httpClient = httpClient;
        _authState = authState;
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _authState.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<List<TradeCategoryDto>> GetTradeCategoriesAsync()
    {
        try
        {
            var result = await _httpClient.GetFromJsonAsync<List<TradeCategoryDto>>("api/tradecategories");
            return result ?? new List<TradeCategoryDto>();
        }
        catch
        {
            return new List<TradeCategoryDto>();
        }
    }

    public async Task<JobListResponse?> GetJobsAsync(int page = 1, int pageSize = 10, Guid? tradeCategoryId = null)
    {
        try
        {
            await SetAuthHeaderAsync();
            var url = $"api/jobs?page={page}&pageSize={pageSize}";
            if (tradeCategoryId.HasValue)
            {
                url += $"&tradeCategoryId={tradeCategoryId}";
            }
            return await _httpClient.GetFromJsonAsync<JobListResponse>(url);
        }
        catch
        {
            return null;
        }
    }

    public async Task<JobDto?> GetJobAsync(Guid id)
    {
        try
        {
            await SetAuthHeaderAsync();
            return await _httpClient.GetFromJsonAsync<JobDto>($"api/jobs/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool Success, string? Error)> CreateJobAsync(CreateJobRequest request)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.PostAsJsonAsync("api/jobs", request);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            var error = await response.Content.ReadAsStringAsync();
            return (false, error);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<QuoteComparisonResponse?> GetQuotesForComparisonAsync(Guid jobId, string? sortBy = null, bool sortDescending = false)
    {
        try
        {
            await SetAuthHeaderAsync();
            var url = $"api/quotes/compare/{jobId}";
            if (!string.IsNullOrEmpty(sortBy))
            {
                url += $"?sortBy={sortBy}&sortDescending={sortDescending}";
            }
            return await _httpClient.GetFromJsonAsync<QuoteComparisonResponse>(url);
        }
        catch
        {
            return null;
        }
    }
}
