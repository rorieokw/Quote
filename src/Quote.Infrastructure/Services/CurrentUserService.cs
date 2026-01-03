using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Quote.Application.Common.Interfaces;
using Quote.Domain.Enums;

namespace Quote.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userId != null ? Guid.Parse(userId) : null;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool IsTradie => GetUserType() == UserType.Tradie;

    public bool IsCustomer => GetUserType() == UserType.Customer;

    public bool IsAdmin => GetUserType() == UserType.Admin;

    private UserType? GetUserType()
    {
        var userTypeString = _httpContextAccessor.HttpContext?.User?.FindFirst("user_type")?.Value;
        if (userTypeString != null && Enum.TryParse<UserType>(userTypeString, out var userType))
        {
            return userType;
        }
        return null;
    }
}
