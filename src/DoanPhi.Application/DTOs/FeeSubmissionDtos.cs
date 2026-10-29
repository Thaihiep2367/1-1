namespace DoanPhi.Application.DTOs;

public record FeeSubmissionDto(
    Guid Id,
    string FullName,
    string StudentId,
    string MembershipStatus,
    string Duration,
    decimal Amount,
    string FormattedAmount,
    string Memo,
    string? Notes,
    bool HasProof,
    string? ProofImage,
    DateTime CreatedAt,
    string FormattedDate);

public record SubmitFeeRequest(
    string FullName,
    string StudentId,
    string MembershipStatus,
    int DurationMonths,
    decimal Amount,
    string? Memo,
    string? Notes,
    string? ProofImage);

public record FeeStatsDto(
    int TotalSubmissions,
    decimal TotalAmount,
    string FormattedTotalAmount,
    int DoanVienCount,
    int ChuaDoanCount,
    int WithProofCount);
