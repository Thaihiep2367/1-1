using System.Text;
using DoanPhi.Application.Common.Exceptions;
using DoanPhi.Application.DTOs;
using DoanPhi.Application.Features.Submissions.Commands;
using DoanPhi.Domain.Entities;
using DoanPhi.Domain.Interfaces;

namespace DoanPhi.Application.Features.Submissions.Queries;

public record GetSubmissionsQuery(string? Search);

public class GetSubmissionsQueryHandler
{
    private readonly IFeeSubmissionRepository _repository;

    public GetSubmissionsQueryHandler(IFeeSubmissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<FeeSubmissionDto>> HandleAsync(GetSubmissionsQuery query, CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(query.Search, cancellationToken);
        return items.Select(SubmitFeeCommandHandler.MapToDto).ToList();
    }
}

public record GetSubmissionByIdQuery(Guid Id);

public class GetSubmissionByIdQueryHandler
{
    private readonly IFeeSubmissionRepository _repository;

    public GetSubmissionByIdQueryHandler(IFeeSubmissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<FeeSubmissionDto> HandleAsync(GetSubmissionByIdQuery query, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(query.Id, cancellationToken)
            ?? throw new NotFoundException("Lượt nộp đoàn phí", query.Id);

        return SubmitFeeCommandHandler.MapToDto(item);
    }
}

public record GetFeeStatsQuery();

public class GetFeeStatsQueryHandler
{
    private readonly IFeeSubmissionRepository _repository;

    public GetFeeStatsQueryHandler(IFeeSubmissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<FeeStatsDto> HandleAsync(GetFeeStatsQuery query, CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(null, cancellationToken);

        int totalCount = items.Count;
        decimal totalMoney = items.Sum(x => x.Amount);
        int doanVienCount = items.Count(x => x.MembershipStatus == MembershipStatusExtensions.DaVaoDoanText);
        int chuaDoanCount = items.Count(x => x.MembershipStatus == MembershipStatusExtensions.ChuaVaoDoanText);
        int withProofCount = items.Count(x => !string.IsNullOrWhiteSpace(x.ProofImage));

        return new FeeStatsDto(
            totalCount,
            totalMoney,
            $"{totalMoney:N0} đ",
            doanVienCount,
            chuaDoanCount,
            withProofCount
        );
    }
}

public record ExportCsvQuery();

public class ExportCsvQueryHandler
{
    private readonly IFeeSubmissionRepository _repository;

    public ExportCsvQueryHandler(IFeeSubmissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<byte[]> HandleAsync(ExportCsvQuery query, CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetAllAsync(null, cancellationToken);

        var sb = new StringBuilder();
        // Thêm UTF-8 BOM để Excel hiển thị tiếng Việt chuẩn 100% không vỡ font
        sb.Append('\uFEFF');
        sb.AppendLine("Thời Gian,Họ Và Tên,MSSV,Tình Trạng,Thời Hạn,Số Tiền (VNĐ),Nội Dung CK,Ghi Chú,Đã Có Ảnh Minh Chứng");

        foreach (var item in items)
        {
            var cleanName = $"\"{item.FullName.Replace("\"", "\"\"")}\"";
            var cleanNotes = $"\"{(item.Notes ?? string.Empty).Replace("\"", "\"\"")}\"";
            var cleanMemo = $"\"{(item.TransferMemo ?? string.Empty).Replace("\"", "\"\"")}\"";
            var hasProof = !string.IsNullOrWhiteSpace(item.ProofImage) ? "Có" : "Không";

            sb.AppendLine($"{item.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm:ss},{cleanName},\"{item.StudentId}\",\"{item.MembershipStatus}\",\"{item.DurationMonths} tháng\",{item.Amount},{cleanMemo},{cleanNotes},\"{hasProof}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
