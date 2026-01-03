using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;

namespace Quote.Application.PhotoAnnotations.Commands.DeleteAnnotation;

public class DeleteAnnotationCommandHandler : IRequestHandler<DeleteAnnotationCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBlobStorageService _blobStorage;

    public DeleteAnnotationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IBlobStorageService blobStorage)
    {
        _context = context;
        _currentUser = currentUser;
        _blobStorage = blobStorage;
    }

    public async Task<Result<bool>> Handle(DeleteAnnotationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<bool>.Failure("User not authenticated");
        }

        var annotation = await _context.PhotoAnnotations
            .Include(a => a.Quote)
            .FirstOrDefaultAsync(a => a.Id == request.AnnotationId, cancellationToken);

        if (annotation == null)
        {
            return Result<bool>.Failure("Annotation not found");
        }

        // Verify the quote belongs to the current user
        if (annotation.Quote.TradieId != _currentUser.UserId)
        {
            return Result<bool>.Failure("Not authorized to delete this annotation");
        }

        // Delete the image from storage
        try
        {
            await _blobStorage.DeleteAsync(annotation.AnnotatedImageUrl);
        }
        catch
        {
            // Continue even if image deletion fails
        }

        _context.PhotoAnnotations.Remove(annotation);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
