using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.PhotoAnnotations.Queries.GetAnnotationsForQuote;

public class GetAnnotationsForQuoteQueryHandler : IRequestHandler<GetAnnotationsForQuoteQuery, Result<AnnotationListResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetAnnotationsForQuoteQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<AnnotationListResponse>> Handle(GetAnnotationsForQuoteQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<AnnotationListResponse>.Failure("User not authenticated");
        }

        // Verify the quote exists and belongs to the current user
        var quote = await _context.Quotes
            .FirstOrDefaultAsync(q => q.Id == request.QuoteId && q.TradieId == _currentUser.UserId, cancellationToken);

        if (quote == null)
        {
            return Result<AnnotationListResponse>.Failure("Quote not found");
        }

        var annotations = await _context.PhotoAnnotations
            .Where(a => a.QuoteId == request.QuoteId)
            .OrderByDescending(a => a.Id)
            .Select(a => new PhotoAnnotationDto(
                a.Id,
                a.QuoteId,
                a.OriginalMediaId == Guid.Empty ? null : a.OriginalMediaId,
                a.AnnotatedImageUrl,
                a.AnnotationJson ?? "",
                quote.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        return Result<AnnotationListResponse>.Success(new AnnotationListResponse(
            annotations,
            annotations.Count
        ));
    }
}
