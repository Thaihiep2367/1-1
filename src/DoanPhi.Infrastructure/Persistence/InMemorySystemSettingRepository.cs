using DoanPhi.Domain.Entities;
using DoanPhi.Domain.Interfaces;

namespace DoanPhi.Infrastructure.Persistence;

public class InMemorySystemSettingRepository : ISystemSettingRepository
{
    private static readonly Lock _syncLock = new();
    private static SystemSetting _currentSetting = SystemSetting.CreateDefault();

    public Task<SystemSetting> GetSettingAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncLock)
        {
            return Task.FromResult(_currentSetting);
        }
    }

    public Task UpdateSettingAsync(SystemSetting setting, CancellationToken cancellationToken = default)
    {
        lock (_syncLock)
        {
            _currentSetting = setting;
            return Task.CompletedTask;
        }
    }
}
