using Quote.Shared.DTOs;

namespace Quote.Mobile.Services;

public class AuthService
{
    private const string TokenKey = "auth_token";
    private const string UserIdKey = "user_id";
    private const string UserEmailKey = "user_email";
    private const string UserFirstNameKey = "user_firstname";
    private const string UserLastNameKey = "user_lastname";
    private const string UserTypeKey = "user_type";

    public event Action? OnAuthStateChanged;

    public bool IsAuthenticated { get; private set; }
    public string? UserId { get; private set; }
    public string? Email { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? UserType { get; private set; }
    public bool IsTradie => UserType == "Tradie";
    public bool IsCustomer => UserType == "Customer";

    public async Task InitializeAsync()
    {
        try
        {
            var token = await SecureStorage.GetAsync(TokenKey);
            if (!string.IsNullOrEmpty(token))
            {
                IsAuthenticated = true;
                UserId = await SecureStorage.GetAsync(UserIdKey);
                Email = await SecureStorage.GetAsync(UserEmailKey);
                FirstName = await SecureStorage.GetAsync(UserFirstNameKey);
                LastName = await SecureStorage.GetAsync(UserLastNameKey);
                UserType = await SecureStorage.GetAsync(UserTypeKey);
            }
        }
        catch
        {
            // SecureStorage not available (e.g., web)
        }
    }

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await SecureStorage.GetAsync(TokenKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveAuthAsync(AuthResponse auth)
    {
        try
        {
            await SecureStorage.SetAsync(TokenKey, auth.Token);
            await SecureStorage.SetAsync(UserIdKey, auth.UserId.ToString());
            await SecureStorage.SetAsync(UserEmailKey, auth.Email ?? "");
            await SecureStorage.SetAsync(UserFirstNameKey, auth.FirstName ?? "");
            await SecureStorage.SetAsync(UserLastNameKey, auth.LastName ?? "");
            await SecureStorage.SetAsync(UserTypeKey, auth.UserType ?? "");

            IsAuthenticated = true;
            UserId = auth.UserId.ToString();
            Email = auth.Email;
            FirstName = auth.FirstName;
            LastName = auth.LastName;
            UserType = auth.UserType;

            OnAuthStateChanged?.Invoke();
        }
        catch
        {
            // Handle storage errors
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            SecureStorage.RemoveAll();
        }
        catch { }

        IsAuthenticated = false;
        UserId = null;
        Email = null;
        FirstName = null;
        LastName = null;
        UserType = null;

        OnAuthStateChanged?.Invoke();
    }
}
