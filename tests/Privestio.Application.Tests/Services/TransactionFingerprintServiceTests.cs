using Privestio.Application.Services;
using Privestio.Domain.ValueObjects;

namespace Privestio.Application.Tests.Services;

public class TransactionFingerprintServiceTests
{
    private readonly TransactionFingerprintService _service = new();

    [Fact]
    public void GenerateFingerprint_SameInputs_SameOutput()
    {
        var date = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var amount = new Money(42.99m, "CAD");
        var description = "GROCERY STORE #123";
        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var fp1 = _service.GenerateFingerprint(accountId, date, amount, description);
        var fp2 = _service.GenerateFingerprint(accountId, date, amount, description);

        fp1.Should().Be(fp2);
    }

    [Fact]
    public void GenerateFingerprint_DifferentDescription_DifferentOutput()
    {
        var date = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var amount = new Money(42.99m, "CAD");
        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var fp1 = _service.GenerateFingerprint(accountId, date, amount, "GROCERY STORE #123");
        var fp2 = _service.GenerateFingerprint(accountId, date, amount, "GROCERY STORE #456");

        fp1.Should().NotBe(fp2);
    }

    [Fact]
    public void GenerateFingerprint_DifferentAmount_DifferentOutput()
    {
        var date = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var fp1 = _service.GenerateFingerprint(
            accountId,
            date,
            new Money(42.99m, "CAD"),
            "GROCERY"
        );
        var fp2 = _service.GenerateFingerprint(
            accountId,
            date,
            new Money(43.00m, "CAD"),
            "GROCERY"
        );

        fp1.Should().NotBe(fp2);
    }

    [Fact]
    public void GenerateFingerprint_DifferentDate_DifferentOutput()
    {
        var amount = new Money(42.99m, "CAD");
        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var fp1 = _service.GenerateFingerprint(
            accountId,
            new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            amount,
            "GROCERY"
        );
        var fp2 = _service.GenerateFingerprint(
            accountId,
            new DateTime(2025, 1, 16, 0, 0, 0, DateTimeKind.Utc),
            amount,
            "GROCERY"
        );

        fp1.Should().NotBe(fp2);
    }

    [Fact]
    public void GenerateFingerprint_DifferentAccount_DifferentOutput()
    {
        var date = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var amount = new Money(42.99m, "CAD");

        var fp1 = _service.GenerateFingerprint(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            date,
            amount,
            "GROCERY"
        );
        var fp2 = _service.GenerateFingerprint(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            date,
            amount,
            "GROCERY"
        );

        fp1.Should().NotBe(fp2);
    }

    [Fact]
    public void GenerateFingerprint_CaseInsensitiveDescription()
    {
        var date = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var amount = new Money(42.99m, "CAD");
        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var fp1 = _service.GenerateFingerprint(accountId, date, amount, "Grocery Store");
        var fp2 = _service.GenerateFingerprint(accountId, date, amount, "GROCERY STORE");

        fp1.Should().Be(fp2);
    }

    [Fact]
    public void GenerateFingerprint_TrimsWhitespace()
    {
        var date = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var amount = new Money(42.99m, "CAD");
        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var fp1 = _service.GenerateFingerprint(accountId, date, amount, "  Grocery Store  ");
        var fp2 = _service.GenerateFingerprint(accountId, date, amount, "Grocery Store");

        fp1.Should().Be(fp2);
    }

    [Fact]
    public void GenerateFingerprint_WithExternalId_IncorporatesIt()
    {
        var date = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var amount = new Money(42.99m, "CAD");
        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var fp1 = _service.GenerateFingerprint(
            accountId,
            date,
            amount,
            "GROCERY",
            externalId: "TXN-001"
        );
        var fp2 = _service.GenerateFingerprint(
            accountId,
            date,
            amount,
            "GROCERY",
            externalId: "TXN-002"
        );

        fp1.Should().NotBe(fp2);
    }

    [Fact]
    public void GenerateFingerprint_ProducesConsistentLengthHash()
    {
        var date = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var amount = new Money(42.99m, "CAD");
        var accountId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var fp = _service.GenerateFingerprint(accountId, date, amount, "GROCERY");

        // SHA-256 hex string = 64 characters
        fp.Should().HaveLength(64);
    }
}
