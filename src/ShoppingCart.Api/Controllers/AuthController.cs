using Microsoft.AspNetCore.Mvc;
using ShoppingCart.Application.DTOs.Auth;
using ShoppingCart.Application.Services;

namespace ShoppingCart.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService) => _authService = authService;

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        if (response is null)
            return Unauthorized(new { detail = "Credenciales inválidas." });

        return Ok(response);
    }
}