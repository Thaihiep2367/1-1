namespace DoanPhi.Domain.Entities;

public class FeeSubmission
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string StudentId { get; private set; } = string.Empty;
    public string MembershipStatus { get; private set; } = string.Empty;
    public int DurationMonths { get; private set; } = 12;
    public decimal Amount { get; private set; }
    public string TransferMemo { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public string? ProofImage { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Constructor phục vụ đóng gói và ORM
    public FeeSubmission(
        Guid id,
        string fullName,
        string studentId,
        string membershipStatus,
        int durationMonths,
        decimal amount,
        string transferMemo,
        string? notes,
        string? proofImage,
        DateTime createdAt)
    {
        Id = id;
        FullName = fullName;
        StudentId = studentId;
        MembershipStatus = membershipStatus;
        DurationMonths = durationMonths;
        Amount = amount;
        TransferMemo = transferMemo;
        Notes = notes;
        ProofImage = proofImage;
        CreatedAt = createdAt;
    }

    public static FeeSubmission Create(
        string fullName,
        string studentId,
        string membershipStatus,
        int durationMonths,
        decimal amount,
        string transferMemo,
        string? notes,
        string? proofImage)
    {
        return new FeeSubmission(
            Guid.NewGuid(),
            fullName.Trim(),
            studentId.Trim().ToUpperInvariant(),
            membershipStatus.Trim(),
            durationMonths,
            amount,
            transferMemo.Trim(),
            notes?.Trim(),
            proofImage,
            DateTime.UtcNow
        );
    }
}
