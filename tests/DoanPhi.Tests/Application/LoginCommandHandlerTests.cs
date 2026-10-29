using DoanPhi.Application.Common.Exceptions;
using DoanPhi.Application.Common.Interfaces;
using DoanPhi.Application.Features.Auth.Commands;
using DoanPhi.Domain.Entities;
using DoanPhi.Domain.Interfaces;
using Moq;
using Xunit;

namespace DoanPhi.Tests.Application;

public class LoginCommandHandlerTests
{
    private readonly Mock<IJwtTokenGenerator> _mockJwtGenerator;
    private readonly Mock<IAdminUserRepository> _mockAdminUserRepo;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
        _mockAdminUserRepo = new Mock<IAdminUserRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();

        _handler = new LoginCommandHandler(
            _mockJwtGenerator.Object,
            _mockAdminUserRepo.Object,
            _mockPasswordHasher.Object);
    }

    [Theory]
    [InlineData("admin", "admin123")]
    [InlineData("admin", "admin")]
    public async Task HandleAsync_WithValidAdminCredentials_ReturnsLoginResponseWithToken(string username, string password)
    {
        // Arrange
        const string fakeToken = "mocked-jwt-token-string";
        var existingUser = AdminUser.Create("admin", "PBKDF2$100000$fakessalt$fakehash", "Quản trị viên");

        _mockAdminUserRepo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _mockPasswordHasher.Setup(h => h.VerifyPassword(password, existingUser.PasswordHash))
            .Returns(true);

        _mockJwtGenerator.Setup(g => g.GenerateToken("admin", "Admin"))
            .Returns(fakeToken);

        var command = new LoginCommand(username, password);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("admin", result.Username);
        Assert.Equal("Admin", result.Role);
        Assert.Equal(fakeToken, result.Token);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
        _mockJwtGenerator.Verify(g => g.GenerateToken("admin", "Admin"), Times.Once);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "   ")]
    [InlineData("admin", "")]
    [InlineData("", "admin123")]
    public async Task HandleAsync_WithEmptyCredentials_ThrowsValidationException(string username, string password)
    {
        // Arrange
        var command = new LoginCommand(username, password);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
    }

    [Theory]
    [InlineData("admin", "wrong_password")]
    [InlineData("user", "admin123")]
    [InlineData("hacker", "123456")]
    public async Task HandleAsync_WithInvalidCredentials_ThrowsValidationException(string username, string password)
    {
        // Arrange
        if (username == "admin")
        {
            var existingUser = AdminUser.Create("admin", "PBKDF2$100000$fakessalt$fakehash", "Quản trị viên");
            _mockAdminUserRepo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingUser);
            _mockPasswordHasher.Setup(h => h.VerifyPassword(password, existingUser.PasswordHash))
                .Returns(false);
        }
        else
        {
            _mockAdminUserRepo.Setup(r => r.GetByUsernameAsync(username, It.IsAny<CancellationToken>()))
                .ReturnsAsync((AdminUser?)null);
            _mockAdminUserRepo.Setup(r => r.HasAnyAdminAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }

        var command = new LoginCommand(username, password);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
        Assert.True(ex.Errors.ContainsKey("Auth"));
    }
}
