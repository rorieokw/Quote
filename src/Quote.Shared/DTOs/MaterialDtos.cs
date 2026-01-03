namespace Quote.Shared.DTOs;

// Material line item for quote submission
public record QuoteMaterialRequest(
    string ProductName,
    string? SupplierName,
    string? ProductUrl,
    decimal Quantity,
    string? Unit,
    decimal UnitPrice
);

// Material display on quotes
public record QuoteMaterialDto(
    Guid Id,
    string ProductName,
    string? SupplierName,
    string? ProductUrl,
    decimal Quantity,
    string? Unit,
    decimal UnitPrice,
    decimal TotalPrice
);

// Material bundle DTOs
public record MaterialBundleDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? TradeCategoryId,
    string? TradeCategoryName,
    decimal TotalCost,
    int ItemCount,
    int UsageCount,
    bool IsActive,
    DateTime CreatedAt,
    List<MaterialBundleItemDto> Items
);

public record MaterialBundleItemDto(
    Guid Id,
    string ProductName,
    string? SupplierName,
    string? ProductUrl,
    decimal DefaultQuantity,
    string? Unit,
    decimal EstimatedUnitPrice,
    decimal TotalPrice,
    int SortOrder
);

public record CreateMaterialBundleRequest(
    string Name,
    string? Description,
    Guid? TradeCategoryId,
    List<CreateMaterialBundleItemRequest> Items
);

public record CreateMaterialBundleItemRequest(
    string ProductName,
    string? SupplierName,
    string? ProductUrl,
    decimal DefaultQuantity,
    string? Unit,
    decimal EstimatedUnitPrice
);

public record UpdateMaterialBundleRequest(
    string Name,
    string? Description,
    Guid? TradeCategoryId,
    bool IsActive,
    List<UpdateMaterialBundleItemRequest> Items
);

public record UpdateMaterialBundleItemRequest(
    Guid? Id, // Null for new items
    string ProductName,
    string? SupplierName,
    string? ProductUrl,
    decimal DefaultQuantity,
    string? Unit,
    decimal EstimatedUnitPrice
);

public record MaterialBundleListResponse(
    List<MaterialBundleDto> Bundles,
    int TotalCount
);

public record CreateMaterialBundleResponse(
    Guid BundleId,
    string Name
);
