using Banking.API.Models.Auth;
using Banking.Application.DTOs.Auth;
using Banking.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Login([FromBody] LoginHttpRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(
            new LoginRequest(request.Username, request.Password),
            cancellationToken);

        return this.ToActionResult(result);
    }
}
