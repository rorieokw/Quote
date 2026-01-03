using System.Net.Http.Json;
using System.Net.Http.Headers;
using Quote.Shared.DTOs;

namespace Quote.Web.Client.Services;

public class MessagesService
{
    private readonly HttpClient _httpClient;
    private readonly AuthStateService _authState;

    public MessagesService(HttpClient httpClient, AuthStateService authState)
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

    public async Task<ConversationListResponse?> GetConversationsAsync(int page = 1, int pageSize = 20)
    {
        try
        {
            await SetAuthHeaderAsync();
            return await _httpClient.GetFromJsonAsync<ConversationListResponse>(
                $"api/messages/conversations?page={page}&pageSize={pageSize}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<ConversationDetailDto?> GetConversationAsync(Guid conversationId, int page = 1, int pageSize = 50)
    {
        try
        {
            await SetAuthHeaderAsync();
            return await _httpClient.GetFromJsonAsync<ConversationDetailDto>(
                $"api/messages/conversations/{conversationId}?page={page}&pageSize={pageSize}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<MessageDto?> SendMessageAsync(SendMessageRequest request)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.PostAsJsonAsync("api/messages/send", request);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<MessageDto>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> MarkAsReadAsync(Guid conversationId)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.PostAsync($"api/messages/conversations/{conversationId}/read", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> GetUnreadCountAsync()
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetFromJsonAsync<UnreadCountDto>("api/messages/unread-count");
            return response?.TotalUnread ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}
