namespace DoanPhi.Domain.Entities;

public enum MembershipStatus
{
    DaVaoDoan,
    ChuaVaoDoan
}

public static class MembershipStatusExtensions
{
    public const string DaVaoDoanText = "Đã vào Đoàn";
    public const string ChuaVaoDoanText = "Chưa vào Đoàn";

    public static string ToDisplayText(this MembershipStatus status) => status switch
    {
        MembershipStatus.DaVaoDoan => DaVaoDoanText,
        MembershipStatus.ChuaVaoDoan => ChuaVaoDoanText,
        _ => DaVaoDoanText
    };

    public static MembershipStatus ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return MembershipStatus.DaVaoDoan;

        var clean = value.Trim().ToLowerInvariant();
        if (clean.Contains("chưa") || clean.Contains("chua") || clean.Contains("chua_vao_doan"))
        {
            return MembershipStatus.ChuaVaoDoan;
        }

        return MembershipStatus.DaVaoDoan;
    }
}
