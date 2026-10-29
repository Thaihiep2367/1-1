using DoanPhi.Application.DTOs;
using DoanPhi.Application.Features.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoanPhi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SystemController : ControllerBase
{
    // GET /api/system/status (Công khai cho mọi người dùng)
    [HttpGet("status")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SystemStatusDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SystemStatusDto>> GetStatus(
        [FromServices] GetSystemStatusQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetSystemStatusQuery(), cancellationToken);
        return Ok(result);
    }

    // POST /api/system/toggle-form (Chỉ dành cho Quản trị viên)
    [HttpPost("toggle-form")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SystemStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<SystemStatusDto>> ToggleForm(
        [FromBody] ToggleFormRequest request,
        [FromServices] ToggleFormStatusCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ToggleFormStatusCommand(request.IsFormOpen, request.Reason), cancellationToken);
        return Ok(result);
    }
}
