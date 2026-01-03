using MediatR;
using Quote.Application.Common.Models;

namespace Quote.Application.MaterialBundles.Commands.UpdateBundle;

public record UpdateBundleCommand : IRequest<Result<UpdateBundleResponse>>
{
    public Guid BundleId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? TradeCategoryId { get; init; }
    public bool IsActive { get; init; } = true;
    public List<UpdateBundleItemCommand> Items { get; init; } = new();
}

public record UpdateBundleItemCommand
{
    public Guid? Id { get; init; } // Null for new items
    public string ProductName { get; init; } = string.Empty;
    public string? SupplierName { get; init; }
    public string? ProductUrl { get; init; }
    public decimal DefaultQuantity { get; init; }
    public string? Unit { get; init; }
    public decimal EstimatedUnitPrice { get; init; }
}

public record UpdateBundleResponse
{
    public Guid BundleId { get; init; }
    public string Name { get; init; } = string.Empty;
}
