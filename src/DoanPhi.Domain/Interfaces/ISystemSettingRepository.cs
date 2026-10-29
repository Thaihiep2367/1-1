using DoanPhi.Domain.Entities;

namespace DoanPhi.Domain.Interfaces;

public interface ISystemSettingRepository
{
    Task<SystemSetting> GetSettingAsync(CancellationToken cancellationToken = default);
    Task UpdateSettingAsync(SystemSetting setting, CancellationToken cancellationToken = default);
}
