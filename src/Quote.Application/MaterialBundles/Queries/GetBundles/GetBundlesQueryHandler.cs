using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.MaterialBundles.Queries.GetBundles;

public class GetBundlesQueryHandler : IRequestHandler<GetBundlesQuery, Result<MaterialBundleListResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetBundlesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<MaterialBundleListResponse>> Handle(GetBundlesQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<MaterialBundleListResponse>.Failure("User not authenticated");
        }

        var query = _context.MaterialBundles
            .Include(b => b.TradeCategory)
            .Include(b => b.Items.OrderBy(i => i.SortOrder))
            .Where(b => b.TradieId == _currentUser.UserId);

        if (!request.IncludeInactive)
        {
            query = query.Where(b => b.IsActive);
        }

        if (request.TradeCategoryId.HasValue)
        {
            query = query.Where(b => b.TradeCategoryId == request.TradeCategoryId);
        }

        var bundles = await query
            .OrderByDescending(b => b.UsageCount)
            .ThenBy(b => b.Name)
            .ToListAsync(cancellationToken);

        var bundleDtos = bundles.Select(b => new MaterialBundleDto(
            b.Id,
            b.Name,
            b.Description,
            b.TradeCategoryId,
            b.TradeCategory?.Name,
            b.Items.Sum(i => i.DefaultQuantity * i.EstimatedUnitPrice),
            b.Items.Count,
            b.UsageCount,
            b.IsActive,
            b.CreatedAt,
            b.Items.Select(i => new MaterialBundleItemDto(
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
        )).ToList();

        return Result<MaterialBundleListResponse>.Success(new MaterialBundleListResponse(
            bundleDtos,
            bundleDtos.Count
        ));
    }
}
