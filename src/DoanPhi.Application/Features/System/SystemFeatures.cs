using DoanPhi.Application.DTOs;
using DoanPhi.Domain.Interfaces;

namespace DoanPhi.Application.Features.System;

public record GetSystemStatusQuery();

public class GetSystemStatusQueryHandler
{
    private readonly ISystemSettingRepository _repository;

    public GetSystemStatusQueryHandler(ISystemSettingRepository repository)
    {
        _repository = repository;
    }

    public async Task<SystemStatusDto> HandleAsync(GetSystemStatusQuery query, CancellationToken cancellationToken = default)
    {
        var setting = await _repository.GetSettingAsync(cancellationToken);
        return new SystemStatusDto(
            setting.IsFormOpen,
            setting.CloseReason,
            setting.LastUpdated,
            setting.LastUpdated.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss")
        );
    }
}

public record ToggleFormStatusCommand(bool IsFormOpen, string? Reason);

public class ToggleFormStatusCommandHandler
{
    private readonly ISystemSettingRepository _repository;

    public ToggleFormStatusCommandHandler(ISystemSettingRepository repository)
    {
        _repository = repository;
    }

    public async Task<SystemStatusDto> HandleAsync(ToggleFormStatusCommand command, CancellationToken cancellationToken = default)
    {
        var setting = await _repository.GetSettingAsync(cancellationToken);
        setting.UpdateStatus(command.IsFormOpen, command.Reason);
        await _repository.UpdateSettingAsync(setting, cancellationToken);

        return new SystemStatusDto(
            setting.IsFormOpen,
            setting.CloseReason,
            setting.LastUpdated,
            setting.LastUpdated.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss")
        );
    }
}
