using DoanPhi.Application.Common.Exceptions;
using DoanPhi.Application.Common.Interfaces;
using DoanPhi.Application.Features.Auth.Commands;
using DoanPhi.Domain.Entities;
using DoanPhi.Domain.Interfaces;
using Moq;
using Xunit;

namespace DoanPhi.Tests.Application;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IAdminUserRepository> _mockAdminUserRepo;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandHandlerTests()
    {
        _mockAdminUserRepo = new Mock<IAdminUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();

        _handler = new ChangePasswordCommandHandler(
            _mockAdminUserRepo.Object,
            _mockPasswordHasher.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidCurrentPassword_UpdatesPasswordHashInRepository()
    {
        // Arrange
        var user = AdminUser.Create("admin", "old_hash", "Admin Full Name");
        _mockAdminUserRepo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher.Setup(h => h.VerifyPassword("currentSecret123", "old_hash"))
            .Returns(true);

        _mockPasswordHasher.Setup(h => h.HashPassword("newSecurePass999"))
            .Returns("new_hashed_password");

        var command = new ChangePasswordCommand("admin", "currentSecret123", "newSecurePass999");

        // Act
        await _handler.HandleAsync(command);

        // Assert
        Assert.Equal("new_hashed_password", user.PasswordHash);
        _mockAdminUserRepo.Verify(r => r.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithWrongCurrentPassword_ThrowsValidationException()
    {
        // Arrange
        var user = AdminUser.Create("admin", "old_hash", "Admin Full Name");
        _mockAdminUserRepo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockPasswordHasher.Setup(h => h.VerifyPassword("wrongPassword", "old_hash"))
            .Returns(false);

        var command = new ChangePasswordCommand("admin", "wrongPassword", "newSecurePass999");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
        _mockAdminUserRepo.Verify(r => r.UpdateAsync(It.IsAny<AdminUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
