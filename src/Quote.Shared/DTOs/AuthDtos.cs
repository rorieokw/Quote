namespace Quote.Shared.DTOs;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string UserType, // "Customer" or "Tradie"
    string? Phone = null,
    string? ABN = null
);

public record AuthResponse(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string UserType,
    string Token
);
