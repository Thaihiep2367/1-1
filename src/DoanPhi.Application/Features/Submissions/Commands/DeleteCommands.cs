using DoanPhi.Application.Common.Exceptions;
using DoanPhi.Domain.Interfaces;

namespace DoanPhi.Application.Features.Submissions.Commands;

public record DeleteSubmissionCommand(Guid Id);

public class DeleteSubmissionCommandHandler
{
    private readonly IFeeSubmissionRepository _repository;

    public DeleteSubmissionCommandHandler(IFeeSubmissionRepository repository)
    {
        _repository = repository;
    }

    public async Task HandleAsync(DeleteSubmissionCommand command, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException("Lượt nộp đoàn phí", command.Id);

        await _repository.DeleteAsync(existing.Id, cancellationToken);
    }
}

public record ClearAllSubmissionsCommand();

public class ClearAllSubmissionsCommandHandler
{
    private readonly IFeeSubmissionRepository _repository;

    public ClearAllSubmissionsCommandHandler(IFeeSubmissionRepository repository)
    {
        _repository = repository;
    }

    public async Task HandleAsync(ClearAllSubmissionsCommand command, CancellationToken cancellationToken = default)
    {
        await _repository.ClearAllAsync(cancellationToken);
    }
}
