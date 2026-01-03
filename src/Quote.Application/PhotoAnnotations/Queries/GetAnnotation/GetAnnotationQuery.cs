using MediatR;
using Quote.Application.Common.Models;
using Quote.Shared.DTOs;

namespace Quote.Application.PhotoAnnotations.Queries.GetAnnotation;

public record GetAnnotationQuery : IRequest<Result<PhotoAnnotationDto>>
{
    public Guid AnnotationId { get; init; }
}
