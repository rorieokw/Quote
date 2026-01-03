namespace Quote.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsTradie { get; }
    bool IsCustomer { get; }
    bool IsAdmin { get; }
}
