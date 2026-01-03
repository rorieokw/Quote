using System.Net.Http.Json;
using System.Net.Http.Headers;
using Quote.Shared.DTOs;

namespace Quote.Mobile.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    // Change this to your API URL (use your local IP for testing on device)
    // For emulator: use 10.0.2.2 (Android) or localhost (iOS simulator)
    // For real device: use your computer's local IP (e.g., 192.168.1.x)
#if DEBUG
    private const string BaseUrl = "http://10.0.2.2:5102/api/"; // Android emulator
#else
    private const string BaseUrl = "https://your-production-api.com/api/";
#endif

    public ApiService(AuthService authService)
    {
        _authService = authService;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    // Auth
    public async Task<AuthResponse?> LoginAsync(string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("auth/login", new { email, password });
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AuthResponse>();
        }
        return null;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("auth/register", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<AuthResponse>();
        }
        return null;
    }

    // Jobs
    public async Task<List<JobDto>> GetJobsAsync()
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _httpClient.GetFromJsonAsync<JobListResponse>("jobs");
            return response?.Jobs ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task<JobDto?> GetJobAsync(Guid jobId)
    {
        await SetAuthHeaderAsync();
        try
        {
            return await _httpClient.GetFromJsonAsync<JobDto>($"jobs/{jobId}");
        }
        catch
        {
            return null;
        }
    }

    // Quotes
    public async Task<bool> SubmitQuoteAsync(QuickQuoteRequest request)
    {
        await SetAuthHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("quotes/quick", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<List<QuoteStatusDto>> GetMyQuotesAsync()
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _httpClient.GetFromJsonAsync<QuoteListResponse>("quotes/my-quotes");
            return response?.Quotes ?? new();
        }
        catch
        {
            return new();
        }
    }

    // Templates
    public async Task<List<QuoteTemplateDto>> GetTemplatesAsync()
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _httpClient.GetFromJsonAsync<TemplatesResponse>("templates");
            return response?.Templates ?? new();
        }
        catch
        {
            return new();
        }
    }

    // Profile
    public async Task<TradieProfileDto?> GetProfileAsync()
    {
        await SetAuthHeaderAsync();
        try
        {
            return await _httpClient.GetFromJsonAsync<TradieProfileDto>("tradie/profile");
        }
        catch
        {
            return null;
        }
    }

    // Messages
    public async Task<ConversationListResponse?> GetConversationsAsync(int page = 1, int pageSize = 20)
    {
        await SetAuthHeaderAsync();
        try
        {
            return await _httpClient.GetFromJsonAsync<ConversationListResponse>(
                $"messages/conversations?page={page}&pageSize={pageSize}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<ConversationDetailDto?> GetConversationAsync(Guid conversationId, int page = 1, int pageSize = 50)
    {
        await SetAuthHeaderAsync();
        try
        {
            return await _httpClient.GetFromJsonAsync<ConversationDetailDto>(
                $"messages/conversations/{conversationId}?page={page}&pageSize={pageSize}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<MessageDto?> SendMessageAsync(SendMessageRequest request)
    {
        await SetAuthHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("messages/send", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<MessageDto>();
        }
        return null;
    }

    public async Task<bool> MarkConversationAsReadAsync(Guid conversationId)
    {
        await SetAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"messages/conversations/{conversationId}/read", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<int> GetUnreadCountAsync()
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _httpClient.GetFromJsonAsync<UnreadCountDto>("messages/unread-count");
            return response?.TotalUnread ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    // Material Bundles
    public async Task<List<MaterialBundleDto>> GetMaterialBundlesAsync(Guid? tradeCategoryId = null, bool includeInactive = false)
    {
        await SetAuthHeaderAsync();
        try
        {
            var query = $"material-bundles?includeInactive={includeInactive}";
            if (tradeCategoryId.HasValue)
            {
                query += $"&tradeCategoryId={tradeCategoryId}";
            }
            var response = await _httpClient.GetFromJsonAsync<MaterialBundleListResponse>(query);
            return response?.Bundles ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task<MaterialBundleDto?> GetMaterialBundleAsync(Guid bundleId)
    {
        await SetAuthHeaderAsync();
        try
        {
            return await _httpClient.GetFromJsonAsync<MaterialBundleDto>($"material-bundles/{bundleId}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<CreateMaterialBundleResponse?> CreateMaterialBundleAsync(CreateMaterialBundleRequest request)
    {
        await SetAuthHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("material-bundles", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CreateMaterialBundleResponse>();
        }
        return null;
    }

    public async Task<bool> UpdateMaterialBundleAsync(Guid bundleId, UpdateMaterialBundleRequest request)
    {
        await SetAuthHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync($"material-bundles/{bundleId}", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteMaterialBundleAsync(Guid bundleId)
    {
        await SetAuthHeaderAsync();
        var response = await _httpClient.DeleteAsync($"material-bundles/{bundleId}");
        return response.IsSuccessStatusCode;
    }

    // Photo Annotations
    public async Task<CreateAnnotationResponse?> CreateAnnotationAsync(CreateAnnotationRequest request)
    {
        await SetAuthHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("annotations", request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<CreateAnnotationResponse>();
        }
        return null;
    }

    public async Task<PhotoAnnotationDto?> GetAnnotationAsync(Guid annotationId)
    {
        await SetAuthHeaderAsync();
        try
        {
            return await _httpClient.GetFromJsonAsync<PhotoAnnotationDto>($"annotations/{annotationId}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<PhotoAnnotationDto>> GetAnnotationsForQuoteAsync(Guid quoteId)
    {
        await SetAuthHeaderAsync();
        try
        {
            var response = await _httpClient.GetFromJsonAsync<AnnotationListResponse>($"annotations/quote/{quoteId}");
            return response?.Annotations ?? new();
        }
        catch
        {
            return new();
        }
    }

    public async Task<bool> DeleteAnnotationAsync(Guid annotationId)
    {
        await SetAuthHeaderAsync();
        var response = await _httpClient.DeleteAsync($"annotations/{annotationId}");
        return response.IsSuccessStatusCode;
    }

    // Price Benchmarking
    public async Task<PriceBenchmarkDto?> GetPriceBenchmarkAsync(Guid tradeCategoryId, string? postcode = null)
    {
        await SetAuthHeaderAsync();
        try
        {
            var query = $"pricing/benchmarks/{tradeCategoryId}";
            if (!string.IsNullOrEmpty(postcode))
            {
                query += $"?postcode={postcode}";
            }
            return await _httpClient.GetFromJsonAsync<PriceBenchmarkDto>(query);
        }
        catch
        {
            return null;
        }
    }

    public async Task<QuotePriceComparisonDto?> CompareQuotePriceAsync(Guid quoteId)
    {
        await SetAuthHeaderAsync();
        try
        {
            return await _httpClient.GetFromJsonAsync<QuotePriceComparisonDto>($"pricing/compare/{quoteId}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<TradieQuotesComparisonResponse?> GetMyQuotesComparisonAsync()
    {
        await SetAuthHeaderAsync();
        try
        {
            return await _httpClient.GetFromJsonAsync<TradieQuotesComparisonResponse>("pricing/my-comparison");
        }
        catch
        {
            return null;
        }
    }
}

// Response wrapper classes (ones not in Quote.Shared)
public record TemplatesResponse(List<QuoteTemplateDto> Templates);
