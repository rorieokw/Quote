using MediatR;
using Quote.Application.Common.Models;

namespace Quote.Application.MaterialBundles.Commands.CreateBundle;

public record CreateBundleCommand : IRequest<Result<CreateBundleResponse>>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid? TradeCategoryId { get; init; }
    public List<CreateBundleItemCommand> Items { get; init; } = new();
}

public record CreateBundleItemCommand
{
    public string ProductName { get; init; } = string.Empty;
    public string? SupplierName { get; init; }
    public string? ProductUrl { get; init; }
    public decimal DefaultQuantity { get; init; }
    public string? Unit { get; init; }
    public decimal EstimatedUnitPrice { get; init; }
}

public record CreateBundleResponse
{
    public Guid BundleId { get; init; }
    public string Name { get; init; } = string.Empty;
}
