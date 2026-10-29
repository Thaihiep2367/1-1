namespace DoanPhi.Application.DTOs;

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    string Token,
    string Username,
    string Role,
    DateTime ExpiresAt);

public record SystemStatusDto(
    bool IsFormOpen,
    string? CloseReason,
    DateTime LastUpdated,
    string FormattedLastUpdated);

public record ToggleFormRequest(bool IsFormOpen, string? Reason);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
