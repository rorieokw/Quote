using Microsoft.JSInterop;
using Quote.Shared.DTOs;

namespace Quote.Web.Client.Services;

public class AuthStateService
{
    private readonly IJSRuntime _jsRuntime;
    private AuthResponse? _currentUser;

    public event Action? OnAuthStateChanged;

    public AuthStateService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public AuthResponse? CurrentUser => _currentUser;
    public bool IsAuthenticated => _currentUser != null;
    public bool IsCustomer => _currentUser?.UserType == "Customer";
    public bool IsTradie => _currentUser?.UserType == "Tradie";
    public bool IsAdmin => _currentUser?.UserType == "Admin";

    public async Task InitializeAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "authToken");
            var userJson = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "currentUser");

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userJson))
            {
                _currentUser = System.Text.Json.JsonSerializer.Deserialize<AuthResponse>(userJson);
                OnAuthStateChanged?.Invoke();
            }
        }
        catch
        {
            // Ignore errors during SSR
        }
    }

    public async Task SetUserAsync(AuthResponse user)
    {
        _currentUser = user;
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "authToken", user.Token);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "currentUser",
                System.Text.Json.JsonSerializer.Serialize(user));
        }
        catch
        {
            // Ignore errors during SSR
        }
        OnAuthStateChanged?.Invoke();
    }

    public async Task ClearUserAsync()
    {
        _currentUser = null;
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "authToken");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "currentUser");
        }
        catch
        {
            // Ignore errors during SSR
        }
        OnAuthStateChanged?.Invoke();
    }

    public async Task<string?> GetTokenAsync()
    {
        if (_currentUser != null) return _currentUser.Token;

        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", "authToken");
        }
        catch
        {
            return null;
        }
    }
}
