using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.PhotoAnnotations.Queries.GetAnnotation;

public class GetAnnotationQueryHandler : IRequestHandler<GetAnnotationQuery, Result<PhotoAnnotationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetAnnotationQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<PhotoAnnotationDto>> Handle(GetAnnotationQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<PhotoAnnotationDto>.Failure("User not authenticated");
        }

        var annotation = await _context.PhotoAnnotations
            .Include(a => a.Quote)
            .FirstOrDefaultAsync(a => a.Id == request.AnnotationId, cancellationToken);

        if (annotation == null)
        {
            return Result<PhotoAnnotationDto>.Failure("Annotation not found");
        }

        // Verify the quote belongs to the current user
        if (annotation.Quote.TradieId != _currentUser.UserId)
        {
            return Result<PhotoAnnotationDto>.Failure("Not authorized to view this annotation");
        }

        var dto = new PhotoAnnotationDto(
            annotation.Id,
            annotation.QuoteId,
            annotation.OriginalMediaId == Guid.Empty ? null : annotation.OriginalMediaId,
            annotation.AnnotatedImageUrl,
            annotation.AnnotationJson ?? "",
            annotation.Quote.CreatedAt
        );

        return Result<PhotoAnnotationDto>.Success(dto);
    }
}
