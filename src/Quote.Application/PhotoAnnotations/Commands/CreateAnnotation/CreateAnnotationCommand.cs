using MediatR;
using Quote.Application.Common.Models;

namespace Quote.Application.PhotoAnnotations.Commands.CreateAnnotation;

public record CreateAnnotationCommand : IRequest<Result<CreateAnnotationResponse>>
{
    public Guid QuoteId { get; init; }
    public Guid? OriginalMediaId { get; init; }
    public string AnnotatedImageBase64 { get; init; } = string.Empty;
    public string AnnotationJson { get; init; } = string.Empty;
}

public record CreateAnnotationResponse
{
    public Guid AnnotationId { get; init; }
    public string AnnotatedImageUrl { get; init; } = string.Empty;
}
