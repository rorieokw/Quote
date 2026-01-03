namespace Quote.Shared.DTOs;

public record PortfolioItemDto(
    Guid Id,
    string Title,
    string? Description,
    Guid TradeCategoryId,
    string TradeCategoryName,
    bool IsPublic,
    bool IsFeatured,
    int SortOrder,
    DateTime CreatedAt,
    List<PortfolioMediaDto> Media
);

public record PortfolioMediaDto(
    Guid Id,
    string MediaUrl,
    string MediaType,
    bool IsBefore,
    string? Caption,
    int SortOrder
);

public record CreatePortfolioItemRequest(
    string Title,
    string? Description,
    Guid TradeCategoryId,
    Guid? CompletedJobId,
    bool IsPublic,
    bool IsFeatured
);

public record UpdatePortfolioItemRequest(
    string Title,
    string? Description,
    bool IsPublic,
    bool IsFeatured,
    int SortOrder
);

public record AddPortfolioMediaRequest(
    string MediaUrl,
    string MediaType,
    bool IsBefore,
    string? Caption,
    int SortOrder
);

public record PortfolioListResponse(
    List<PortfolioItemDto> Items,
    int TotalCount
);

// Public portfolio view for customers
public record TradiePortfolioDto(
    Guid TradieId,
    string TradieName,
    string? BusinessName,
    decimal Rating,
    int TotalJobsCompleted,
    List<PortfolioItemDto> FeaturedItems,
    List<PortfolioItemDto> AllItems
);
