using MediatR;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;
using Quote.Application.Common.Models;
using Quote.Domain.Entities;

namespace Quote.Application.PhotoAnnotations.Commands.CreateAnnotation;

public class CreateAnnotationCommandHandler : IRequestHandler<CreateAnnotationCommand, Result<CreateAnnotationResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IBlobStorageService _blobStorage;

    public CreateAnnotationCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IBlobStorageService blobStorage)
    {
        _context = context;
        _currentUser = currentUser;
        _blobStorage = blobStorage;
    }

    public async Task<Result<CreateAnnotationResponse>> Handle(CreateAnnotationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId == null)
        {
            return Result<CreateAnnotationResponse>.Failure("User not authenticated");
        }

        // Verify the quote exists and belongs to the current user
        var quote = await _context.Quotes
            .FirstOrDefaultAsync(q => q.Id == request.QuoteId && q.TradieId == _currentUser.UserId, cancellationToken);

        if (quote == null)
        {
            return Result<CreateAnnotationResponse>.Failure("Quote not found");
        }

        // Verify original media exists if provided
        if (request.OriginalMediaId.HasValue)
        {
            var mediaExists = await _context.JobMedia
                .AnyAsync(m => m.Id == request.OriginalMediaId, cancellationToken);

            if (!mediaExists)
            {
                return Result<CreateAnnotationResponse>.Failure("Original media not found");
            }
        }

        // Convert base64 to stream and upload
        string imageUrl;
        try
        {
            var base64Data = request.AnnotatedImageBase64;
            if (base64Data.Contains(","))
            {
                base64Data = base64Data.Split(',')[1];
            }

            var imageBytes = Convert.FromBase64String(base64Data);
            using var stream = new MemoryStream(imageBytes);

            var fileName = $"annotation_{Guid.NewGuid()}.png";
            imageUrl = await _blobStorage.UploadAsync(stream, fileName, "image/png");
        }
        catch (Exception ex)
        {
            return Result<CreateAnnotationResponse>.Failure($"Failed to upload annotated image: {ex.Message}");
        }

        var annotation = new PhotoAnnotation
        {
            Id = Guid.NewGuid(),
            QuoteId = request.QuoteId,
            OriginalMediaId = request.OriginalMediaId ?? Guid.Empty,
            AnnotatedImageUrl = imageUrl,
            AnnotationJson = request.AnnotationJson
        };

        _context.PhotoAnnotations.Add(annotation);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<CreateAnnotationResponse>.Success(new CreateAnnotationResponse
        {
            AnnotationId = annotation.Id,
            AnnotatedImageUrl = annotation.AnnotatedImageUrl
        });
    }
}
