namespace DoanPhi.Domain.Entities;

public class SystemSetting
{
    public bool IsFormOpen { get; private set; } = true;
    public string? CloseReason { get; private set; }
    public DateTime LastUpdated { get; private set; } = DateTime.UtcNow;

    public SystemSetting(bool isFormOpen, string? closeReason, DateTime lastUpdated)
    {
        IsFormOpen = isFormOpen;
        CloseReason = closeReason;
        LastUpdated = lastUpdated;
    }

    public static SystemSetting CreateDefault()
    {
        return new SystemSetting(true, null, DateTime.UtcNow);
    }

    public void UpdateStatus(bool isFormOpen, string? reason)
    {
        IsFormOpen = isFormOpen;
        CloseReason = isFormOpen ? null : (string.IsNullOrWhiteSpace(reason) ? "Hệ thống đã kết thúc đợt thu đoàn phí." : reason.Trim());
        LastUpdated = DateTime.UtcNow;
    }
}
