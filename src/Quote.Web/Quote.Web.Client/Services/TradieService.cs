using System.Net.Http.Json;
using Quote.Shared.DTOs;

namespace Quote.Web.Client.Services;

public class TradieService
{
    private readonly HttpClient _httpClient;
    private readonly AuthStateService _authState;

    public TradieService(HttpClient httpClient, AuthStateService authState)
    {
        _httpClient = httpClient;
        _authState = authState;
    }

    private async Task SetAuthHeader()
    {
        var token = await _authState.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }

    // Quick Quote
    public async Task<QuickQuoteResponse?> SubmitQuickQuoteAsync(QuickQuoteRequest request)
    {
        try
        {
            await SetAuthHeader();
            var response = await _httpClient.PostAsJsonAsync("api/quotes/quick", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<QuickQuoteResponse>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    // My Quotes
    public async Task<QuoteListResponse?> GetMyQuotesAsync(int page = 1, int pageSize = 10, string? status = null)
    {
        try
        {
            await SetAuthHeader();
            var url = $"api/quotes/my-quotes?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(status))
            {
                url += $"&status={status}";
            }
            return await _httpClient.GetFromJsonAsync<QuoteListResponse>(url);
        }
        catch
        {
            return null;
        }
    }

    // Templates
    public async Task<TemplateListResponse?> GetTemplatesAsync(Guid? tradeCategoryId = null)
    {
        try
        {
            await SetAuthHeader();
            var url = "api/templates";
            if (tradeCategoryId.HasValue)
            {
                url += $"?tradeCategoryId={tradeCategoryId}";
            }
            return await _httpClient.GetFromJsonAsync<TemplateListResponse>(url);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> CreateTemplateAsync(CreateTemplateRequest request)
    {
        try
        {
            await SetAuthHeader();
            var response = await _httpClient.PostAsJsonAsync("api/templates", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteTemplateAsync(Guid templateId)
    {
        try
        {
            await SetAuthHeader();
            var response = await _httpClient.DeleteAsync($"api/templates/{templateId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Job Preferences
    public async Task<JobPreferencesDto?> GetPreferencesAsync()
    {
        try
        {
            await SetAuthHeader();
            return await _httpClient.GetFromJsonAsync<JobPreferencesDto>("api/tradie/preferences");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdatePreferencesAsync(UpdateJobPreferencesRequest request)
    {
        try
        {
            await SetAuthHeader();
            var response = await _httpClient.PutAsJsonAsync("api/tradie/preferences", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Available Now
    public async Task<AvailableNowDto?> GetAvailableNowAsync()
    {
        try
        {
            await SetAuthHeader();
            return await _httpClient.GetFromJsonAsync<AvailableNowDto>("api/tradie/available-now");
        }
        catch
        {
            return null;
        }
    }

    public async Task<AvailableNowDto?> SetAvailableNowAsync(SetAvailableNowRequest request)
    {
        try
        {
            await SetAuthHeader();
            var response = await _httpClient.PutAsJsonAsync("api/tradie/available-now", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AvailableNowDto>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    // Blocked Suburbs
    public async Task<List<BlockedSuburbDto>> GetBlockedSuburbsAsync()
    {
        try
        {
            await SetAuthHeader();
            return await _httpClient.GetFromJsonAsync<List<BlockedSuburbDto>>("api/tradie/blocked-suburbs") ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task<bool> BlockSuburbAsync(BlockSuburbRequest request)
    {
        try
        {
            await SetAuthHeader();
            var response = await _httpClient.PostAsJsonAsync("api/tradie/blocked-suburbs", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UnblockSuburbAsync(string postcode)
    {
        try
        {
            await SetAuthHeader();
            var response = await _httpClient.DeleteAsync($"api/tradie/blocked-suburbs/{postcode}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Verification Status
    public async Task<VerificationStatusDto?> GetVerificationStatusAsync()
    {
        try
        {
            await SetAuthHeader();
            return await _httpClient.GetFromJsonAsync<VerificationStatusDto>("api/tradie/verification-status");
        }
        catch
        {
            return null;
        }
    }

    // Customer Quality
    public async Task<CustomerQualityDto?> GetCustomerQualityAsync(Guid customerId)
    {
        try
        {
            await SetAuthHeader();
            return await _httpClient.GetFromJsonAsync<CustomerQualityDto>($"api/customers/{customerId}/quality");
        }
        catch
        {
            return null;
        }
    }

    // Portfolio
    public async Task<PortfolioListResponse?> GetPortfolioAsync()
    {
        try
        {
            await SetAuthHeader();
            return await _httpClient.GetFromJsonAsync<PortfolioListResponse>("api/portfolio");
        }
        catch
        {
            return null;
        }
    }

    public async Task<TradiePortfolioDto?> GetPublicPortfolioAsync(Guid tradieId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<TradiePortfolioDto>($"api/tradies/{tradieId}/portfolio");
        }
        catch
        {
            return null;
        }
    }

    // Team
    public async Task<TeamListResponse?> GetTeamMembersAsync()
    {
        try
        {
            await SetAuthHeader();
            return await _httpClient.GetFromJsonAsync<TeamListResponse>("api/team");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> AddTeamMemberAsync(CreateTeamMemberRequest request)
    {
        try
        {
            await SetAuthHeader();
            var response = await _httpClient.PostAsJsonAsync("api/team", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Material Bundles
    public async Task<MaterialBundleListResponse?> GetMaterialBundlesAsync(Guid? tradeCategoryId = null, bool includeInactive = false)
    {
        try
        {
            await SetAuthHeader();
            var url = $"api/material-bundles?includeInactive={includeInactive}";
            if (tradeCategoryId.HasValue)
            {
                url += $"&tradeCategoryId={tradeCategoryId}";
            }
            return await _httpClient.GetFromJsonAsync<MaterialBundleListResponse>(url);
        }
        catch
        {
            return null;
        }
    }

    public async Task<MaterialBundleDto?> GetMaterialBundleAsync(Guid bundleId)
    {
        try
        {
            await SetAuthHeader();
            return await _httpClient.GetFromJsonAsync<MaterialBundleDto>($"api/material-bundles/{bundleId}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<CreateMaterialBundleResponse?> CreateMaterialBundleAsync(CreateMaterialBundleRequest request)
    {
        try
        {
            await SetAuthHeader();
            var response = await _httpClient.PostAsJsonAsync("api/material-bundles", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CreateMaterialBundleResponse>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateMaterialBundleAsync(Guid bundleId, UpdateMaterialBundleRequest request)
    {
        try
        {
            await SetAuthHeader();
            var response = await _httpClient.PutAsJsonAsync($"api/material-bundles/{bundleId}", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteMaterialBundleAsync(Guid bundleId)
    {
        try
        {
            await SetAuthHeader();
            var response = await _httpClient.DeleteAsync($"api/material-bundles/{bundleId}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Price Benchmarking
    public async Task<PriceBenchmarkDto?> GetPriceBenchmarkAsync(Guid tradeCategoryId, string? postcode = null)
    {
        try
        {
            await SetAuthHeader();
            var url = $"api/pricing/benchmarks/{tradeCategoryId}";
            if (!string.IsNullOrEmpty(postcode))
            {
                url += $"?postcode={postcode}";
            }
            return await _httpClient.GetFromJsonAsync<PriceBenchmarkDto>(url);
        }
        catch
        {
            return null;
        }
    }

    public async Task<TradieQuotesComparisonResponse?> GetMyQuotesComparisonAsync()
    {
        try
        {
            await SetAuthHeader();
            return await _httpClient.GetFromJsonAsync<TradieQuotesComparisonResponse>("api/pricing/my-comparison");
        }
        catch
        {
            return null;
        }
    }

    public async Task<QuotePriceComparisonDto?> GetQuoteComparisonAsync(Guid quoteId)
    {
        try
        {
            await SetAuthHeader();
            return await _httpClient.GetFromJsonAsync<QuotePriceComparisonDto>($"api/pricing/compare/{quoteId}");
        }
        catch
        {
            return null;
        }
    }
}
