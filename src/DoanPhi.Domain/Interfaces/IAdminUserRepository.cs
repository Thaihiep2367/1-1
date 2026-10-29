using DoanPhi.Domain.Entities;

namespace DoanPhi.Domain.Interfaces;

public interface IAdminUserRepository
{
    Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<AdminUser> CreateAsync(AdminUser user, CancellationToken cancellationToken = default);
    Task UpdateAsync(AdminUser user, CancellationToken cancellationToken = default);
    Task<bool> HasAnyAdminAsync(CancellationToken cancellationToken = default);
}
