namespace Quote.Shared.DTOs;

public record TradeCategoryDto(
    Guid Id,
    string Name,
    string? Description,
    string? Icon
);
