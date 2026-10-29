using DoanPhi.Domain.Entities;
using DoanPhi.Domain.Interfaces;

namespace DoanPhi.Infrastructure.Persistence;

public class InMemoryFeeSubmissionRepository : IFeeSubmissionRepository
{
    private static readonly Lock _syncLock = new();
    private static readonly List<FeeSubmission> _database = [];

    // Seed dữ liệu mẫu ban đầu một lần duy nhất
    static InMemoryFeeSubmissionRepository()
    {
        lock (_syncLock)
        {
            var s1 = FeeSubmission.Create(
                "Nguyễn Văn An",
                "20261001",
                MembershipStatusExtensions.DaVaoDoanText,
                12,
                108000m,
                "20261001 NGUYEN VAN AN Doan phi",
                "K48 - CNTT 01",
                null
            );

            var s2 = FeeSubmission.Create(
                "Trần Thị Bích",
                "20261002",
                MembershipStatusExtensions.ChuaVaoDoanText,
                12,
                84000m,
                "20261002 TRAN THI BICH Doan phi",
                "K48 - Kế toán 02",
                null
            );

            var s3 = FeeSubmission.Create(
                "Lê Hoàng Cường",
                "20261003",
                MembershipStatusExtensions.DaVaoDoanText,
                12,
                108000m,
                "20261003 LE HOANG CUONG Doan phi",
                "SĐT: 0912345678",
                null
            );

            _database.Add(s1);
            _database.Add(s2);
            _database.Add(s3);
        }
    }

    public Task<IReadOnlyList<FeeSubmission>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        lock (_syncLock)
        {
            IEnumerable<FeeSubmission> query = _database;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(s =>
                    s.FullName.ToLowerInvariant().Contains(term) ||
                    s.StudentId.ToLowerInvariant().Contains(term) ||
                    s.TransferMemo.ToLowerInvariant().Contains(term));
            }

            return Task.FromResult<IReadOnlyList<FeeSubmission>>(
                query.OrderByDescending(s => s.CreatedAt).ToList()
            );
        }
    }

    public Task<FeeSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_syncLock)
        {
            var item = _database.FirstOrDefault(s => s.Id == id);
            return Task.FromResult(item);
        }
    }

    public Task<FeeSubmission> AddAsync(FeeSubmission submission, CancellationToken cancellationToken = default)
    {
        lock (_syncLock)
        {
            _database.Insert(0, submission); // Mới nhất lên đầu
            return Task.FromResult(submission);
        }
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_syncLock)
        {
            _database.RemoveAll(s => s.Id == id);
            return Task.CompletedTask;
        }
    }

    public Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncLock)
        {
            _database.Clear();
            return Task.CompletedTask;
        }
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        lock (_syncLock)
        {
            return Task.FromResult(_database.Count);
        }
    }
}
