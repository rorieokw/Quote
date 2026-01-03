using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Entities;
using Quote.Domain.Enums;

namespace Quote.Application.MaterialBundles.Commands.CreateBundle;

public class CreateBundleCommandHandler : IRequestHandler<CreateBundleCommand, Result<CreateBundleResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateBundleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<CreateBundleResponse>> Handle(CreateBundleCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<CreateBundleResponse>.Failure("User not authenticated");
        }

        // Verify user is a tradie
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId, cancellationToken);

        if (user == null || user.UserType != UserType.Tradie)
        {
            return Result<CreateBundleResponse>.Failure("Only tradies can create material bundles");
        }

        // Verify trade category exists (if provided)
        if (request.TradeCategoryId.HasValue)
        {
            var categoryExists = await _context.TradeCategories
                .AnyAsync(c => c.Id == request.TradeCategoryId && c.IsActive, cancellationToken);

            if (!categoryExists)
            {
                return Result<CreateBundleResponse>.Failure("Invalid trade category");
            }
        }

        var bundle = new MaterialBundle
        {
            Id = Guid.NewGuid(),
            TradieId = _currentUser.UserId.Value,
            Name = request.Name,
            Description = request.Description,
            TradeCategoryId = request.TradeCategoryId,
            IsActive = true,
            UsageCount = 0
        };

        // Add items
        var sortOrder = 0;
        foreach (var item in request.Items)
        {
            bundle.Items.Add(new MaterialBundleItem
            {
                Id = Guid.NewGuid(),
                BundleId = bundle.Id,
                ProductName = item.ProductName,
                SupplierName = item.SupplierName,
                ProductUrl = item.ProductUrl,
                DefaultQuantity = item.DefaultQuantity,
                Unit = item.Unit,
                EstimatedUnitPrice = item.EstimatedUnitPrice,
                SortOrder = sortOrder++
            });
        }

        _context.MaterialBundles.Add(bundle);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<CreateBundleResponse>.Success(new CreateBundleResponse
        {
            BundleId = bundle.Id,
            Name = bundle.Name
        });
    }
}
