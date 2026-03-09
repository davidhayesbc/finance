using Privestio.Domain.Entities;

namespace Privestio.Domain.Tests.Entities;

public class SyncTombstoneTests
{
    [Fact]
    public void Constructor_ValidArgs_CreatesSyncTombstone()
    {
        var entityId = Guid.NewGuid();

        var tombstone = new SyncTombstone("Transaction", entityId);

        tombstone.EntityType.Should().Be("Transaction");
        tombstone.EntityId.Should().Be(entityId);
        tombstone.DeletedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        tombstone.SyncedAt.Should().BeNull();
        tombstone.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrEmptyEntityType_ThrowsArgumentException(string? entityType)
    {
        var act = () => new SyncTombstone(entityType!, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MarkSynced_SetsSyncedAtAndUpdatedAt()
    {
        var tombstone = new SyncTombstone("Account", Guid.NewGuid());
        var beforeSync = DateTime.UtcNow;

        tombstone.MarkSynced();

        tombstone.SyncedAt.Should().NotBeNull();
        tombstone.SyncedAt.Should().BeOnOrAfter(beforeSync);
        tombstone.UpdatedAt.Should().BeOnOrAfter(beforeSync);
    }
}

public class SyncCheckpointTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Constructor_ValidArgs_CreatesSyncCheckpoint()
    {
        var checkpoint = new SyncCheckpoint(UserId, "device-abc-123");

        checkpoint.UserId.Should().Be(UserId);
        checkpoint.DeviceId.Should().Be("device-abc-123");
        checkpoint.LastSyncToken.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        checkpoint.Id.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrEmptyDeviceId_ThrowsArgumentException(string? deviceId)
    {
        var act = () => new SyncCheckpoint(UserId, deviceId!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void UpdateToken_SetsLastSyncTokenAndUpdatedAt()
    {
        var checkpoint = new SyncCheckpoint(UserId, "device-1");
        var newToken = DateTime.UtcNow.AddMinutes(5);
        var beforeUpdate = DateTime.UtcNow;

        checkpoint.UpdateToken(newToken);

        checkpoint.LastSyncToken.Should().Be(newToken);
        checkpoint.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
    }
}

public class SyncConflictTests
{
    [Fact]
    public void Constructor_ValidArgs_CreatesSyncConflictWithPendingStatus()
    {
        var entityId = Guid.NewGuid();

        var conflict = new SyncConflict("Budget", entityId, "{\"amount\":100}", "{\"amount\":200}");

        conflict.EntityType.Should().Be("Budget");
        conflict.EntityId.Should().Be(entityId);
        conflict.LocalData.Should().Be("{\"amount\":100}");
        conflict.ServerData.Should().Be("{\"amount\":200}");
        conflict.Status.Should().Be("Pending");
        conflict.DetectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        conflict.ResolvedAt.Should().BeNull();
        conflict.Resolution.Should().BeNull();
        conflict.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Resolve_SetsStatusResolutionAndResolvedAt()
    {
        var conflict = new SyncConflict("Budget", Guid.NewGuid(), "{\"a\":1}", "{\"a\":2}");
        var beforeResolve = DateTime.UtcNow;

        conflict.Resolve("ServerWins");

        conflict.Status.Should().Be("Resolved");
        conflict.Resolution.Should().Be("ServerWins");
        conflict.ResolvedAt.Should().NotBeNull();
        conflict.ResolvedAt.Should().BeOnOrAfter(beforeResolve);
        conflict.UpdatedAt.Should().BeOnOrAfter(beforeResolve);
    }

    [Fact]
    public void Resolve_WithMergedData_UpdatesLocalData()
    {
        var conflict = new SyncConflict("Budget", Guid.NewGuid(), "{\"a\":1}", "{\"a\":2}");

        conflict.Resolve("Merged", "{\"a\":3}");

        conflict.Status.Should().Be("Resolved");
        conflict.Resolution.Should().Be("Merged");
        conflict.LocalData.Should().Be("{\"a\":3}");
    }

    [Fact]
    public void Resolve_WithoutMergedData_DoesNotChangeLocalData()
    {
        var conflict = new SyncConflict("Budget", Guid.NewGuid(), "{\"a\":1}", "{\"a\":2}");

        conflict.Resolve("ServerWins");

        conflict.LocalData.Should().Be("{\"a\":1}");
    }
}

public class IdempotencyRecordTests
{
    [Fact]
    public void Constructor_ValidArgs_CreatesIdempotencyRecord()
    {
        var record = new IdempotencyRecord("op-12345", "{\"status\":\"ok\"}");

        record.OperationId.Should().Be("op-12345");
        record.ResponseData.Should().Be("{\"status\":\"ok\"}");
        record.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_SetsExpiresAtApproximatelySevenDaysInFuture()
    {
        var beforeCreate = DateTime.UtcNow;

        var record = new IdempotencyRecord("op-1", "{}");

        var expectedExpiry = beforeCreate.AddDays(7);
        record.ExpiresAt.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_NullOrEmptyOperationId_ThrowsArgumentException(string? operationId)
    {
        var act = () => new IdempotencyRecord(operationId!, "response");

        act.Should().Throw<ArgumentException>();
    }
}
