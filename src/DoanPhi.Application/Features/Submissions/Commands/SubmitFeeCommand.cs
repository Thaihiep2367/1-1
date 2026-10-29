using DoanPhi.Application.Common.Exceptions;
using DoanPhi.Application.Common.Helpers;
using DoanPhi.Application.DTOs;
using DoanPhi.Domain.Entities;
using DoanPhi.Domain.Interfaces;

namespace DoanPhi.Application.Features.Submissions.Commands;

public record SubmitFeeCommand(
    string FullName,
    string StudentId,
    string MembershipStatus,
    int DurationMonths,
    decimal Amount,
    string? Memo,
    string? Notes,
    string? ProofImage);

public class SubmitFeeCommandHandler
{
    private readonly IFeeSubmissionRepository _repository;
    private readonly ISystemSettingRepository _systemSettingRepository;

    public SubmitFeeCommandHandler(
        IFeeSubmissionRepository repository,
        ISystemSettingRepository systemSettingRepository)
    {
        _repository = repository;
        _systemSettingRepository = systemSettingRepository;
    }

    public async Task<FeeSubmissionDto> HandleAsync(SubmitFeeCommand command, CancellationToken cancellationToken = default)
    {
        // 0. Kiểm tra hệ thống có đang mở nhận form hay không
        var setting = await _systemSettingRepository.GetSettingAsync(cancellationToken);
        if (!setting.IsFormOpen)
        {
            throw new ValidationException(setting.CloseReason ?? "Hệ thống hiện tại đã đóng tiếp nhận nộp đoàn phí.");
        }

        var errors = new Dictionary<string, string[]>();

        // 1. Validate Họ Tên
        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            errors.Add(nameof(command.FullName), ["Họ và tên sinh viên không được để trống."]);
        }
        else if (command.FullName.Trim().Length > 100)
        {
            errors.Add(nameof(command.FullName), ["Họ và tên không được vượt quá 100 ký tự."]);
        }

        // 2. Validate Mã Sinh Viên (MSSV)
        if (string.IsNullOrWhiteSpace(command.StudentId))
        {
            errors.Add(nameof(command.StudentId), ["Mã sinh viên (MSSV) không được để trống."]);
        }
        else if (command.StudentId.Trim().Length > 20)
        {
            errors.Add(nameof(command.StudentId), ["Mã sinh viên không được vượt quá 20 ký tự."]);
        }

        // 3. Validate Định Mức Thu Phí (Cả năm 12 tháng)
        var parsedStatus = MembershipStatusExtensions.ParseStatus(command.MembershipStatus);
        var expectedAmount = parsedStatus == MembershipStatus.DaVaoDoan ? 108000m : 84000m;

        if (command.Amount <= 0 || command.Amount != expectedAmount)
        {
            errors.Add(nameof(command.Amount), [$"Số tiền nộp không chính xác. Định mức quy định là {expectedAmount:N0} VNĐ."]);
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        // 4. Chuẩn hoá nội dung chuyển khoản nếu chưa có
        string memo = command.Memo?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(memo))
        {
            var cleanName = StringHelper.RemoveVietnameseTones(command.FullName);
            var cleanMssv = StringHelper.RemoveVietnameseTones(command.StudentId).ToUpperInvariant();
            memo = $"{cleanMssv} {cleanName} Doan phi".Trim();
        }

        // 5. Tạo Entity
        var submission = FeeSubmission.Create(
            command.FullName,
            command.StudentId,
            parsedStatus.ToDisplayText(),
            command.DurationMonths <= 0 ? 12 : command.DurationMonths,
            expectedAmount,
            memo,
            command.Notes,
            command.ProofImage
        );

        var saved = await _repository.AddAsync(submission, cancellationToken);

        // 6. Map sang DTO
        return MapToDto(saved);
    }

    public static FeeSubmissionDto MapToDto(FeeSubmission s)
    {
        return new FeeSubmissionDto(
            s.Id,
            s.FullName,
            s.StudentId,
            s.MembershipStatus,
            $"{s.DurationMonths} tháng",
            s.Amount,
            $"{s.Amount:N0} đ",
            s.TransferMemo,
            s.Notes,
            !string.IsNullOrWhiteSpace(s.ProofImage),
            s.ProofImage,
            s.CreatedAt,
            s.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss")
        );
    }
}
