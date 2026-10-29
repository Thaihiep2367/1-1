using DoanPhi.Application.Common.Exceptions;
using DoanPhi.Application.Common.Interfaces;
using DoanPhi.Domain.Interfaces;

namespace DoanPhi.Application.Features.Auth.Commands;

public record ChangePasswordCommand(string Username, string CurrentPassword, string NewPassword);

public class ChangePasswordCommandHandler
{
    private readonly IAdminUserRepository _adminUserRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordCommandHandler(
        IAdminUserRepository adminUserRepository,
        IPasswordHasher passwordHasher)
    {
        _adminUserRepository = adminUserRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.CurrentPassword))
        {
            throw new ValidationException("Vui lòng nhập mật khẩu hiện tại.");
        }

        if (string.IsNullOrWhiteSpace(command.NewPassword) || command.NewPassword.Length < 6)
        {
            throw new ValidationException("Mật khẩu mới phải có tối thiểu 6 ký tự.");
        }

        var user = await _adminUserRepository.GetByUsernameAsync(command.Username, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException("Tài khoản quản trị", command.Username);
        }

        bool isCurrentValid = _passwordHasher.VerifyPassword(command.CurrentPassword, user.PasswordHash);
        if (!isCurrentValid)
        {
            throw new ValidationException("Mật khẩu hiện tại không chính xác.");
        }

        var newHash = _passwordHasher.HashPassword(command.NewPassword);
        user.UpdatePassword(newHash);
        await _adminUserRepository.UpdateAsync(user, cancellationToken);
    }
}
