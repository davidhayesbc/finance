// TODO: Re-enable once Privestio.Tests.Common with TestDbContextFactory is created
#if false
using Privestio.Application.Commands.CreateValuation;
using Privestio.Application.Queries.GetValuations;
using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;
using Privestio.Infrastructure.Data;
using Privestio.Tests.Common;
using Xunit;

namespace Privestio.Application.Tests.Commands;

public class ValuationHistoryPreservationTests
{
    [Fact]
    public async Task CreateValuation_WithMultipleDatesAndSources_PreservesAllRecords()
    {
        // Arrange
        var dbContext = new TestDbContextFactory().CreateDbContext();
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var account = new Account(
            "Sunlife RRSP",
            AccountType.Property,
            AccountSubType.RRSP,
            "CAD",
            new Money(100000m, "CAD"),
            DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-5),
            userId
        );
        account.Id = accountId;
        await dbContext.Accounts.AddAsync(account);
        await dbContext.SaveChangesAsync();

        var repository = new ValuationRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);

        // Act - Create multiple valuations on different dates
        var val1 = new Valuation(
            accountId,
            new Money(150000m, "CAD"),
            DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-3),
            "Statement"
        );
        var val2 = new Valuation(
            accountId,
            new Money(155000m, "CAD"),
            DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2),
            "Statement"
        );
        var val3 = new Valuation(
            accountId,
            new Money(160000m, "CAD"),
            DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1),
            "Statement"
        );

        await repository.AddAsync(val1);
        await repository.AddAsync(val2);
        await repository.AddAsync(val3);
        await unitOfWork.SaveChangesAsync();

        // Assert - All valuations should be preserved
        var valuations = await repository.GetByAccountIdAsync(accountId);
        Assert.Equal(3, valuations.Count);
        Assert.Contains(valuations, v => v.EstimatedValue.Amount == 150000m);
        Assert.Contains(valuations, v => v.EstimatedValue.Amount == 155000m);
        Assert.Contains(valuations, v => v.EstimatedValue.Amount == 160000m);
    }

    [Fact]
    public async Task CreateValuation_WithSameDateAndSource_CreatesNewRecord()
    {
        // Arrange
        var dbContext = new TestDbContextFactory().CreateDbContext();
        var userId = Guid.NewGuid();
        var accountId = Guid.NewGuid();

        var account = new Account(
            "Sunlife RRSP",
            AccountType.Property,
            AccountSubType.RRSP,
            "CAD",
            new Money(100000m, "CAD"),
            DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-5),
            userId
        );
        account.Id = accountId;
        await dbContext.Accounts.AddAsync(account);
        await dbContext.SaveChangesAsync();

        var repository = new ValuationRepository(dbContext);
        var unitOfWork = new UnitOfWork(dbContext);
        var effectiveDate = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act - Create two valuations on the same date with the same source
        var val1 = new Valuation(accountId, new Money(150000m, "CAD"), effectiveDate, "Statement");
        var val2 = new Valuation(accountId, new Money(160000m, "CAD"), effectiveDate, "Statement");

        await repository.AddAsync(val1);
        await repository.AddAsync(val2);
        await unitOfWork.SaveChangesAsync();

        // Assert - Both valuations should exist (this is expected behavior)
        // User should manually manage valuations if they need to replace values
        var valuations = await repository.GetByAccountIdAsync(accountId);
        Assert.Equal(2, valuations.Count);
    }
}
#endif
