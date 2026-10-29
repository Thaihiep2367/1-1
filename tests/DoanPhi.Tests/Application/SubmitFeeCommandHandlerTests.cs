using DoanPhi.Application.Common.Exceptions;
using DoanPhi.Application.Features.Submissions.Commands;
using DoanPhi.Domain.Entities;
using DoanPhi.Domain.Interfaces;
using Moq;
using Xunit;

namespace DoanPhi.Tests.Application;

public class SubmitFeeCommandHandlerTests
{
    private readonly Mock<IFeeSubmissionRepository> _mockRepo;
    private readonly Mock<ISystemSettingRepository> _mockSettingRepo;
    private readonly SubmitFeeCommandHandler _handler;

    public SubmitFeeCommandHandlerTests()
    {
        _mockRepo = new Mock<IFeeSubmissionRepository>();
        _mockSettingRepo = new Mock<ISystemSettingRepository>();

        // Mặc định form luôn mở
        _mockSettingRepo.Setup(s => s.GetSettingAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(SystemSetting.CreateDefault());

        _handler = new SubmitFeeCommandHandler(_mockRepo.Object, _mockSettingRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_WithValidData_ReturnsCreatedSubmissionDto()
    {
        // Arrange
        var command = new SubmitFeeCommand(
            "Nguyễn Văn An",
            "20261001",
            MembershipStatusExtensions.DaVaoDoanText,
            12,
            108000m,
            "20261001 NGUYEN VAN AN Doan phi",
            "Lớp K48",
            "data:image/jpeg;base64,sample"
        );

        _mockRepo.Setup(r => r.AddAsync(It.IsAny<FeeSubmission>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((FeeSubmission s, CancellationToken ct) => s);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(command.FullName, result.FullName);
        Assert.Equal("20261001", result.StudentId);
        Assert.Equal(108000m, result.Amount);
        Assert.Equal("12 tháng", result.Duration);
        Assert.True(result.HasProof);
        Assert.NotEqual(Guid.Empty, result.Id);

        _mockRepo.Verify(r => r.AddAsync(It.Is<FeeSubmission>(s => s.FullName == command.FullName), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenFormIsClosed_ThrowsValidationException()
    {
        // Arrange: Quản trị viên đã đóng form
        var closedSetting = new SystemSetting(false, "Đã hết hạn nộp đoàn phí", DateTime.UtcNow);
        _mockSettingRepo.Setup(s => s.GetSettingAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(closedSetting);

        var command = new SubmitFeeCommand(
            "Nguyễn Văn An",
            "20261001",
            MembershipStatusExtensions.DaVaoDoanText,
            12,
            108000m,
            null,
            null,
            null
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
        Assert.Contains("hết hạn", ex.Message);

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<FeeSubmission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task HandleAsync_WithEmptyFullName_ThrowsValidationException(string? invalidFullName)
    {
        // Arrange
        var command = new SubmitFeeCommand(
            invalidFullName!,
            "20261001",
            MembershipStatusExtensions.DaVaoDoanText,
            12,
            108000m,
            null,
            null,
            null
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
        Assert.Contains(nameof(command.FullName), exception.Errors.Keys);

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<FeeSubmission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task HandleAsync_WithEmptyStudentId_ThrowsValidationException(string? invalidStudentId)
    {
        // Arrange
        var command = new SubmitFeeCommand(
            "Nguyễn Văn An",
            invalidStudentId!,
            MembershipStatusExtensions.DaVaoDoanText,
            12,
            108000m,
            null,
            null,
            null
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
        Assert.Contains(nameof(command.StudentId), exception.Errors.Keys);

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<FeeSubmission>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithInvalidAmount_ThrowsValidationException()
    {
        // Arrange: Đã vào đoàn nhưng nộp 50.000 thay vì 108.000
        var command = new SubmitFeeCommand(
            "Nguyễn Văn An",
            "20261001",
            MembershipStatusExtensions.DaVaoDoanText,
            12,
            50000m,
            null,
            null,
            null
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
        Assert.Contains(nameof(command.Amount), exception.Errors.Keys);

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<FeeSubmission>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
