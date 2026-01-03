using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.PhotoAnnotations.Queries.GetAnnotationsForQuote;

public record GetAnnotationsForQuoteQuery : IRequest<Result<AnnotationListResponse>>
{
    public Guid QuoteId { get; init; }
}
