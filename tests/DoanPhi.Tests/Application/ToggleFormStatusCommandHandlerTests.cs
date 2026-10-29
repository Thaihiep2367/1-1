using DoanPhi.Application.Features.System;
using DoanPhi.Domain.Entities;
using DoanPhi.Domain.Interfaces;
using Moq;
using Xunit;

namespace DoanPhi.Tests.Application;

public class ToggleFormStatusCommandHandlerTests
{
    private readonly Mock<ISystemSettingRepository> _mockRepo;
    private readonly ToggleFormStatusCommandHandler _handler;

    public ToggleFormStatusCommandHandlerTests()
    {
        _mockRepo = new Mock<ISystemSettingRepository>();
        _handler = new ToggleFormStatusCommandHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenClosingForm_UpdatesSettingAndReturnsClosedStatus()
    {
        // Arrange
        var currentSetting = SystemSetting.CreateDefault();
        _mockRepo.Setup(r => r.GetSettingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentSetting);

        const string reason = "Hết hạn nộp đợt 1";
        var command = new ToggleFormStatusCommand(false, reason);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.False(result.IsFormOpen);
        Assert.Equal(reason, result.CloseReason);
        _mockRepo.Verify(r => r.UpdateSettingAsync(It.Is<SystemSetting>(s => !s.IsFormOpen && s.CloseReason == reason), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenReopeningForm_UpdatesSettingAndReturnsOpenStatus()
    {
        // Arrange
        var currentSetting = new SystemSetting(false, "Đã đóng", DateTime.UtcNow);
        _mockRepo.Setup(r => r.GetSettingAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentSetting);

        var command = new ToggleFormStatusCommand(true, null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.True(result.IsFormOpen);
        Assert.Null(result.CloseReason);
        _mockRepo.Verify(r => r.UpdateSettingAsync(It.Is<SystemSetting>(s => s.IsFormOpen && s.CloseReason == null), It.IsAny<CancellationToken>()), Times.Once);
    }
}
