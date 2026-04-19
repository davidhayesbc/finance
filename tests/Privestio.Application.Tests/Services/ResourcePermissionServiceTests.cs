using FluentAssertions;
using Moq;
using Privestio.Application.Interfaces;
using Privestio.Application.Services;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Xunit;

namespace Privestio.Application.Tests.Services;

public class ResourcePermissionServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IAccountRepository> _accountRepoMock;
    private readonly ResourcePermissionService _service;

    public ResourcePermissionServiceTests()
    {
        _accountRepoMock = new Mock<IAccountRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _unitOfWorkMock.Setup(u => u.Accounts).Returns(_accountRepoMock.Object);
        _service = new ResourcePermissionService(_unitOfWorkMock.Object);
    }

    [Fact]
    public void EnsureOwnership_MatchingUserId_DoesNotThrow()
    {
        var userId = Guid.NewGuid();
        var account = new Account(
            "Test",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(0m),
            DateOnly.FromDateTime(DateTime.UtcNow),
            userId
        );

        var act = () => _service.EnsureAccountOwnership(account, userId);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureOwnership_DifferentUserId_ThrowsUnauthorizedAccessException()
    {
        var account = new Account(
            "Test",
            AccountType.Banking,
            AccountSubType.Chequing,
            "CAD",
            new Money(0m),
            DateOnly.FromDateTime(DateTime.UtcNow),
            Guid.NewGuid()
        );

        var act = () => _service.EnsureAccountOwnership(account, Guid.NewGuid());
        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void EnsureBudgetOwnership_MatchingUserId_DoesNotThrow()
    {
        var userId = Guid.NewGuid();
        var budget = new Budget(userId, Guid.NewGuid(), 2026, 1, new Money(100m));

        var act = () => _service.EnsureBudgetOwnership(budget, userId);
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureBudgetOwnership_DifferentUserId_ThrowsUnauthorizedAccessException()
    {
        var budget = new Budget(Guid.NewGuid(), Guid.NewGuid(), 2026, 1, new Money(100m));

        var act = () => _service.EnsureBudgetOwnership(budget, Guid.NewGuid());
        act.Should().Throw<UnauthorizedAccessException>();
    }
}
