using System.Security.Claims;
using DoanPhi.Application.DTOs;
using DoanPhi.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoanPhi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    // POST /api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        [FromServices] LoginCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new LoginCommand(request.Username, request.Password), cancellationToken);
        return Ok(result);
    }

    // GET /api/auth/verify
    [HttpGet("verify")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult VerifyToken()
    {
        var username = User.Identity?.Name ?? "Admin";
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "Admin";
        return Ok(new { isAuthenticated = true, username, role });
    }

    // POST /api/auth/change-password
    [HttpPost("change-password")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        [FromServices] ChangePasswordCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var username = User.Identity?.Name ?? "admin";
        await handler.HandleAsync(new ChangePasswordCommand(username, request.CurrentPassword, request.NewPassword), cancellationToken);
        return Ok(new { message = "Đổi mật khẩu thành công. Mật khẩu mới đã được cập nhật vào cơ sở dữ liệu." });
    }
}
