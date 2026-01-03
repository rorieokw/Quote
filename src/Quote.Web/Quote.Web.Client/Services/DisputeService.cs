using System.Net.Http.Json;
using Quote.Shared.DTOs;

namespace Quote.Web.Client.Services;

public class DisputeService
{
    private readonly HttpClient _httpClient;
    private readonly AuthStateService _authState;

    public DisputeService(HttpClient httpClient, AuthStateService authState)
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

    // Get my disputes
    public async Task<DisputeListResponse?> GetMyDisputesAsync(string? status = null, int page = 1, int pageSize = 10)
    {
        try
        {
            await SetAuthHeader();
            var url = $"api/disputes/my?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(status))
            {
                url += $"&status={status}";
            }
            return await _httpClient.GetFromJsonAsync<DisputeListResponse>(url);
        }
        catch
        {
            return null;
        }
    }

    // Get all disputes (Admin)
    public async Task<DisputeListResponse?> GetAllDisputesAsync(string? status = null, string? reason = null, int page = 1, int pageSize = 20)
    {
        try
        {
            await SetAuthHeader();
            var url = $"api/disputes?page={page}&pageSize={pageSize}";
            if (!string.IsNullOrEmpty(status))
            {
                url += $"&status={status}";
            }
            if (!string.IsNullOrEmpty(reason))
            {
                url += $"&reason={reason}";
            }
            return await _httpClient.GetFromJsonAsync<DisputeListResponse>(url);
        }
        catch
        {
            return null;
        }
    }

    // Get single dispute
    public async Task<DisputeDto?> GetDisputeAsync(Guid disputeId)
    {
        try
        {
            await SetAuthHeader();
            return await _httpClient.GetFromJsonAsync<DisputeDto>($"api/disputes/{disputeId}");
        }
        catch
        {
            return null;
        }
    }

    // Create dispute
    public async Task<CreateDisputeResponse?> CreateDisputeAsync(CreateDisputeRequest request)
    {
        try
        {
            await SetAuthHeader();
            var response = await _httpClient.PostAsJsonAsync("api/disputes", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CreateDisputeResponse>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    // Resolve dispute (Admin)
    public async Task<bool> ResolveDisputeAsync(Guid disputeId, ResolveDisputeRequest request)
    {
        try
        {
            await SetAuthHeader();
            var response = await _httpClient.PutAsJsonAsync($"api/disputes/{disputeId}/resolve", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Close/withdraw dispute
    public async Task<bool> CloseDisputeAsync(Guid disputeId, string? reason = null)
    {
        try
        {
            await SetAuthHeader();
            var request = new CloseDisputeRequest(reason);
            var response = await _httpClient.PutAsJsonAsync($"api/disputes/{disputeId}/close", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
