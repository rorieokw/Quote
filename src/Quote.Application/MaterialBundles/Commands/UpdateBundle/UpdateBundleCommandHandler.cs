using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Entities;

namespace Quote.Application.MaterialBundles.Commands.UpdateBundle;

public class UpdateBundleCommandHandler : IRequestHandler<UpdateBundleCommand, Result<UpdateBundleResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public UpdateBundleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<UpdateBundleResponse>> Handle(UpdateBundleCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<UpdateBundleResponse>.Failure("User not authenticated");
        }

        var bundle = await _context.MaterialBundles
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == request.BundleId && b.TradieId == _currentUser.UserId, cancellationToken);

        if (bundle == null)
        {
            return Result<UpdateBundleResponse>.Failure("Bundle not found");
        }

        // Verify trade category exists (if provided)
        if (request.TradeCategoryId.HasValue)
        {
            var categoryExists = await _context.TradeCategories
                .AnyAsync(c => c.Id == request.TradeCategoryId && c.IsActive, cancellationToken);

            if (!categoryExists)
            {
                return Result<UpdateBundleResponse>.Failure("Invalid trade category");
            }
        }

        // Update bundle properties
        bundle.Name = request.Name;
        bundle.Description = request.Description;
        bundle.TradeCategoryId = request.TradeCategoryId;
        bundle.IsActive = request.IsActive;

        // Remove items not in the request
        var requestItemIds = request.Items.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToHashSet();
        var itemsToRemove = bundle.Items.Where(i => !requestItemIds.Contains(i.Id)).ToList();
        foreach (var item in itemsToRemove)
        {
            bundle.Items.Remove(item);
        }

        // Update existing items and add new ones
        var sortOrder = 0;
        foreach (var itemRequest in request.Items)
        {
            if (itemRequest.Id.HasValue)
            {
                // Update existing item
                var existingItem = bundle.Items.FirstOrDefault(i => i.Id == itemRequest.Id.Value);
                if (existingItem != null)
                {
                    existingItem.ProductName = itemRequest.ProductName;
                    existingItem.SupplierName = itemRequest.SupplierName;
                    existingItem.ProductUrl = itemRequest.ProductUrl;
                    existingItem.DefaultQuantity = itemRequest.DefaultQuantity;
                    existingItem.Unit = itemRequest.Unit;
                    existingItem.EstimatedUnitPrice = itemRequest.EstimatedUnitPrice;
                    existingItem.SortOrder = sortOrder++;
                }
            }
            else
            {
                // Add new item
                bundle.Items.Add(new MaterialBundleItem
                {
                    Id = Guid.NewGuid(),
                    BundleId = bundle.Id,
                    ProductName = itemRequest.ProductName,
                    SupplierName = itemRequest.SupplierName,
                    ProductUrl = itemRequest.ProductUrl,
                    DefaultQuantity = itemRequest.DefaultQuantity,
                    Unit = itemRequest.Unit,
                    EstimatedUnitPrice = itemRequest.EstimatedUnitPrice,
                    SortOrder = sortOrder++
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<UpdateBundleResponse>.Success(new UpdateBundleResponse
        {
            BundleId = bundle.Id,
            Name = bundle.Name
        });
    }
}
