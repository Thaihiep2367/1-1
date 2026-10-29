using DoanPhi.Application.Common.Exceptions;
using DoanPhi.Application.Common.Interfaces;
using DoanPhi.Application.DTOs;
using DoanPhi.Domain.Entities;
using DoanPhi.Domain.Interfaces;

namespace DoanPhi.Application.Features.Auth.Commands;

public record LoginCommand(string Username, string Password);

public class LoginCommandHandler
{
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IAdminUserRepository _adminUserRepository;
    private readonly IPasswordHasher _passwordHasher;

    public const string DefaultAdminUsername = "admin";
    public const string DefaultInitialPassword = "admin";

    public LoginCommandHandler(
        IJwtTokenGenerator jwtTokenGenerator,
        IAdminUserRepository adminUserRepository,
        IPasswordHasher passwordHasher)
    {
        _jwtTokenGenerator = jwtTokenGenerator;
        _adminUserRepository = adminUserRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResponse> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Username) || string.IsNullOrWhiteSpace(command.Password))
        {
            throw new ValidationException("Vui lòng nhập tên đăng nhập và mật khẩu.");
        }

        var username = command.Username.Trim().ToLowerInvariant();
        var adminUser = await _adminUserRepository.GetByUsernameAsync(username, cancellationToken);

        // Trường hợp hệ thống mới khởi tạo chưa có tài khoản nào trong database:
        if (adminUser == null)
        {
            var hasAny = await _adminUserRepository.HasAnyAdminAsync(cancellationToken);
            if (!hasAny && username == DefaultAdminUsername && 
                (command.Password.Trim() == "admin123" || command.Password.Trim() == DefaultInitialPassword))
            {
                // Tự động khởi tạo tài khoản admin đầu tiên đã băm mật khẩu vào database
                var initialHash = _passwordHasher.HashPassword(command.Password.Trim());
                adminUser = AdminUser.Create(DefaultAdminUsername, initialHash, "Quản trị viên Ban Chấp hành");
                await _adminUserRepository.CreateAsync(adminUser, cancellationToken);
            }
            else
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    { "Auth", ["Tên đăng nhập hoặc mật khẩu quản trị viên không chính xác."] }
                });
            }
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(command.Password.Trim(), adminUser.PasswordHash);
        if (!isPasswordValid)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Auth", ["Tên đăng nhập hoặc mật khẩu quản trị viên không chính xác."] }
            });
        }

        // Tự động nâng cấp hash nếu mật khẩu cũ trong DB còn ở dạng plain-text
        if (!adminUser.PasswordHash.StartsWith("PBKDF2$", StringComparison.Ordinal))
        {
            adminUser.UpdatePassword(_passwordHasher.HashPassword(command.Password.Trim()));
        }

        adminUser.RecordLogin();
        await _adminUserRepository.UpdateAsync(adminUser, cancellationToken);

        const string role = "Admin";
        var token = _jwtTokenGenerator.GenerateToken(adminUser.Username, role);
        var expiresAt = DateTime.UtcNow.AddDays(7);

        return new LoginResponse(token, adminUser.Username, role, expiresAt);
    }
}
