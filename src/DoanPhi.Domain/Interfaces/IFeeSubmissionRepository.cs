using DoanPhi.Domain.Entities;

namespace DoanPhi.Domain.Interfaces;

public interface IFeeSubmissionRepository
{
    Task<IReadOnlyList<FeeSubmission>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default);
    Task<FeeSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FeeSubmission> AddAsync(FeeSubmission submission, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task ClearAllAsync(CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}
