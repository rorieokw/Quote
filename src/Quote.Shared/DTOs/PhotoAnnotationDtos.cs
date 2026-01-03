namespace Quote.Shared.DTOs;

// Request to create a new annotation
public record CreateAnnotationRequest(
    Guid QuoteId,
    Guid? OriginalMediaId,
    string AnnotatedImageBase64,
    string AnnotationJson
);

// Request to update an existing annotation
public record UpdateAnnotationRequest(
    string AnnotatedImageBase64,
    string AnnotationJson
);

// Annotation display DTO
public record PhotoAnnotationDto(
    Guid Id,
    Guid QuoteId,
    Guid? OriginalMediaId,
    string AnnotatedImageUrl,
    string AnnotationJson,
    DateTime CreatedAt
);

// List response
public record AnnotationListResponse(
    List<PhotoAnnotationDto> Annotations,
    int TotalCount
);

// Create response
public record CreateAnnotationResponse(
    Guid AnnotationId,
    string AnnotatedImageUrl
);
