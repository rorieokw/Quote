using MediatR;
using Microsoft.AspNetCore.Mvc;
using Quote.Application.Auth.Commands.Login;
using Quote.Application.Auth.Commands.Register;
using Quote.Domain.Enums;
using Quote.Shared.DTOs;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Parse the UserType string to enum
        if (!Enum.TryParse<UserType>(request.UserType, true, out var userType))
        {
            return BadRequest(new { errors = new[] { "Invalid user type. Must be 'Customer' or 'Tradie'." } });
        }

        var command = new RegisterCommand
        {
            Email = request.Email,
            Password = request.Password,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            UserType = userType,
            ABN = request.ABN
        };

        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors });
        }

        // Return AuthResponse format that frontend expects
        var authResponse = new AuthResponse(
            result.Data!.UserId,
            result.Data.Email,
            request.FirstName,
            request.LastName,
            request.UserType,
            result.Data.AccessToken
        );

        return Ok(authResponse);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.Succeeded)
        {
            return Unauthorized(new { errors = result.Errors });
        }

        // Return AuthResponse format that frontend expects
        var authResponse = new AuthResponse(
            result.Data!.UserId,
            result.Data.Email,
            result.Data.FirstName,
            result.Data.LastName,
            result.Data.UserType,
            result.Data.AccessToken
        );

        return Ok(authResponse);
    }
}
