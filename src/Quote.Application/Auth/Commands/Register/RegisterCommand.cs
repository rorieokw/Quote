using MediatR;
using Quote.Application.Common.Models;
using Quote.Domain.Enums;

namespace Quote.Application.Auth.Commands.Register;

public record RegisterCommand : IRequest<Result<RegisterResponse>>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public UserType UserType { get; init; }
    public string? ABN { get; init; }
}

public record RegisterResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
}
