using MediatR;
using Quote.Application.Common.Models;

namespace Quote.Application.PhotoAnnotations.Commands.DeleteAnnotation;

public record DeleteAnnotationCommand : IRequest<Result<bool>>
{
    public Guid AnnotationId { get; init; }
}
