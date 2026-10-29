using DoanPhi.Application.DTOs;
using DoanPhi.Application.Features.Submissions.Commands;
using DoanPhi.Application.Features.Submissions.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DoanPhi.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SubmissionsController : ControllerBase
{
    // ==========================================
    // 1. DÀNH CHO NGƯỜI DÙNG (CÔNG KHAI - KHÔNG CẦN ĐĂNG NHẬP)
    // ==========================================

    // POST /api/submissions (Sinh viên nộp thông tin & minh chứng)
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FeeSubmissionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FeeSubmissionDto>> Create(
        [FromBody] SubmitFeeRequest request,
        [FromServices] SubmitFeeCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new SubmitFeeCommand(
            request.FullName,
            request.StudentId,
            request.MembershipStatus,
            request.DurationMonths,
            request.Amount,
            request.Memo,
            request.Notes,
            request.ProofImage
        );

        var created = await handler.HandleAsync(command, cancellationToken);
        return CreatedAtRoute("GetSubmissionById", new { id = created.Id }, created);
    }

    // ==========================================
    // 2. DÀNH RIÊNG CHO QUẢN TRỊ VIÊN (BẮT BUỘC ĐĂNG NHẬP ADMIN)
    // ==========================================

    // GET /api/submissions?search=... (Xem danh sách)
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<FeeSubmissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<FeeSubmissionDto>>> GetAll(
        [FromQuery] string? search,
        [FromServices] GetSubmissionsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetSubmissionsQuery(search), cancellationToken);
        return Ok(result);
    }

    // GET /api/submissions/stats (Tổng hợp báo cáo số liệu)
    [HttpGet("stats")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(FeeStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FeeStatsDto>> GetStats(
        [FromServices] GetFeeStatsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetFeeStatsQuery(), cancellationToken);
        return Ok(result);
    }

    // GET /api/submissions/{id} (Xem chi tiết bản ghi)
    [HttpGet("{id:guid}", Name = "GetSubmissionById")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(FeeSubmissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeeSubmissionDto>> GetById(
        Guid id,
        [FromServices] GetSubmissionByIdQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetSubmissionByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    // DELETE /api/submissions/{id} (Xóa bản ghi)
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] DeleteSubmissionCommandHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteSubmissionCommand(id), cancellationToken);
        return NoContent();
    }

    // DELETE /api/submissions (Xóa tất cả)
    [HttpDelete]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearAll(
        [FromServices] ClearAllSubmissionsCommandHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new ClearAllSubmissionsCommand(), cancellationToken);
        return NoContent();
    }

    // GET /api/submissions/export-csv (Xuất báo cáo Excel / CSV)
    [HttpGet("export-csv")]
    [Authorize(Roles = "Admin")]
    [Produces("text/csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCsv(
        [FromServices] ExportCsvQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var csvBytes = await handler.HandleAsync(new ExportCsvQuery(), cancellationToken);
        var fileName = $"Danh_Sach_Dong_Doan_Phi_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return File(csvBytes, "text/csv; charset=utf-8", fileName);
    }
}
