using DoanPhi.Application.Common.Exceptions;
using DoanPhi.Application.Features.Submissions.Queries;
using DoanPhi.Domain.Entities;
using DoanPhi.Domain.Interfaces;
using Moq;
using Xunit;

namespace DoanPhi.Tests.Application;

public class GetSubmissionByIdQueryHandlerTests
{
    private readonly Mock<IFeeSubmissionRepository> _mockRepo;
    private readonly GetSubmissionByIdQueryHandler _handler;

    public GetSubmissionByIdQueryHandlerTests()
    {
        _mockRepo = new Mock<IFeeSubmissionRepository>();
        _handler = new GetSubmissionByIdQueryHandler(_mockRepo.Object);
    }

    [Fact]
    public async Task HandleAsync_WithExistingId_ReturnsSubmissionDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var submission = new FeeSubmission(
            id,
            "Trần Thị Bích",
            "20261002",
            MembershipStatusExtensions.ChuaVaoDoanText,
            12,
            84000m,
            "20261002 TRAN THI BICH Doan phi",
            "Lớp Kế toán",
            null,
            DateTime.UtcNow
        );

        _mockRepo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(submission);

        // Act
        var result = await _handler.HandleAsync(new GetSubmissionByIdQuery(id));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Trần Thị Bích", result.FullName);
        Assert.Equal(84000m, result.Amount);
    }

    [Fact]
    public async Task HandleAsync_WithNonExistingId_ThrowsNotFoundException()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync(nonExistingId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync((FeeSubmission?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.HandleAsync(new GetSubmissionByIdQuery(nonExistingId)));
    }
}
