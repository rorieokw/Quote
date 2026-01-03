using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.PhotoAnnotations.Commands.CreateAnnotation;
using Quote.Application.PhotoAnnotations.Commands.DeleteAnnotation;
using Quote.Application.PhotoAnnotations.Queries.GetAnnotation;
using Quote.Application.PhotoAnnotations.Queries.GetAnnotationsForQuote;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/annotations")]
[Authorize]
public class PhotoAnnotationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PhotoAnnotationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Create a new photo annotation
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Quote.Shared.DTOs.CreateAnnotationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateAnnotation([FromBody] CreateAnnotationRequest request)
    {
        var command = new CreateAnnotationCommand
        {
            QuoteId = request.QuoteId,
            OriginalMediaId = request.OriginalMediaId,
            AnnotatedImageBase64 = request.AnnotatedImageBase64,
            AnnotationJson = request.AnnotationJson
        };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        var response = new Quote.Shared.DTOs.CreateAnnotationResponse(result.Data!.AnnotationId, result.Data.AnnotatedImageUrl);

        return CreatedAtAction(nameof(GetAnnotation), new { id = result.Data.AnnotationId }, response);
    }

    /// <summary>
    /// Get a specific annotation by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PhotoAnnotationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAnnotation(Guid id)
    {
        var query = new GetAnnotationQuery { AnnotationId = id };

        var result = await _mediator.Send(query);

        if (!result.Succeeded)
        {
            return NotFound(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get all annotations for a quote
    /// </summary>
    [HttpGet("quote/{quoteId}")]
    [ProducesResponseType(typeof(AnnotationListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAnnotationsForQuote(Guid quoteId)
    {
        var query = new GetAnnotationsForQuoteQuery { QuoteId = quoteId };

        var result = await _mediator.Send(query);

        if (!result.Succeeded)
        {
            return NotFound(new { errors = result.Errors });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Delete an annotation
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAnnotation(Guid id)
    {
        var command = new DeleteAnnotationCommand { AnnotationId = id };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return NotFound(new { errors = result.Errors });
        }

        return NoContent();
    }
}
