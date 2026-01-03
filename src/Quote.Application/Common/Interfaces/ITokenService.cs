using Quote.Domain.Entities;

namespace Quote.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    (Guid userId, string email)? ValidateToken(string token);
}
