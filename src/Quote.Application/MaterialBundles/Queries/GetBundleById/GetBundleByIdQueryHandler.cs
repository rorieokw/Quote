using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.MaterialBundles.Queries.GetBundleById;

public class GetBundleByIdQueryHandler : IRequestHandler<GetBundleByIdQuery, Result<MaterialBundleDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetBundleByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<MaterialBundleDto>> Handle(GetBundleByIdQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<MaterialBundleDto>.Failure("User not authenticated");
        }

        var bundle = await _context.MaterialBundles
            .Include(b => b.TradeCategory)
            .Include(b => b.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(b => b.Id == request.BundleId && b.TradieId == _currentUser.UserId, cancellationToken);

        if (bundle == null)
        {
            return Result<MaterialBundleDto>.Failure("Bundle not found");
        }

        var bundleDto = new MaterialBundleDto(
            bundle.Id,
            bundle.Name,
            bundle.Description,
            bundle.TradeCategoryId,
            bundle.TradeCategory?.Name,
            bundle.Items.Sum(i => i.DefaultQuantity * i.EstimatedUnitPrice),
            bundle.Items.Count,
            bundle.UsageCount,
            bundle.IsActive,
            bundle.CreatedAt,
            bundle.Items.Select(i => new MaterialBundleItemDto(
                i.Id,
                i.ProductName,
                i.SupplierName,
                i.ProductUrl,
                i.DefaultQuantity,
                i.Unit,
                i.EstimatedUnitPrice,
                i.DefaultQuantity * i.EstimatedUnitPrice,
                i.SortOrder
            )).ToList()
        );

        return Result<MaterialBundleDto>.Success(bundleDto);
    }
}
