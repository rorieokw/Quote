using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;

namespace Quote.Application.MaterialBundles.Commands.DeleteBundle;

public class DeleteBundleCommandHandler : IRequestHandler<DeleteBundleCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DeleteBundleCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(DeleteBundleCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<bool>.Failure("User not authenticated");
        }

        var bundle = await _context.MaterialBundles
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == request.BundleId && b.TradieId == _currentUser.UserId, cancellationToken);

        if (bundle == null)
        {
            return Result<bool>.Failure("Bundle not found");
        }

        // Remove all items first
        _context.MaterialBundleItems.RemoveRange(bundle.Items);

        // Remove the bundle
        _context.MaterialBundles.Remove(bundle);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
