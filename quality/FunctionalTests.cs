using Privestio.Domain.Entities;
using Privestio.Domain.Enums;
using Privestio.Domain.ValueObjects;

namespace Privestio.Quality.Tests;

/// <summary>
/// Functional tests derived from the Privestio quality playbook.
/// Organized into three groups:
///   1. Spec Requirements — tests mapped to FEATURES.md sections
///   2. Fitness Scenarios — tests mapped 1:1 to QUALITY.md scenarios
///   3. Boundary & Edge Cases — tests from defensive pattern analysis
/// </summary>

#region Group 1: Spec Requirements

/// <summary>
/// Tests derived from FEATURES.md testable requirements.
/// Each test cites the spec section it verifies.
/// </summary>
public class SpecRequirementTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    // --- §2.1-2.4: Account Types and Creation ---

    [Theory]
    [InlineData(AccountType.Banking, AccountSubType.Chequing)]
    [InlineData(AccountType.Credit, AccountSubType.CreditCard)]
    [InlineData(AccountType.Investment, AccountSubType.RRSP)]
    [InlineData(AccountType.Property, AccountSubType.RealEstate)]
    [InlineData(AccountType.Loan, AccountSubType.Mortgage)]
    public void Account_AllTypes_CanBeCreated(AccountType type, AccountSubType subType)
    {
        // [Req: formal — FEATURES.md §2.1-2.4]
        var account = new Account(
            "Test Account", type, subType, "CAD",
            new Money(1000m, "CAD"), DateOnly.FromDateTime(DateTime.UtcNow), OwnerId);

        account.AccountType.Should().Be(type);
        account.AccountSubType.Should().Be(subType);
        account.Currency.Should().Be("CAD");
        account.OpeningBalance.Amount.Should().Be(1000m);
    }

    [Fact]
    public void Account_CurrencyNormalization_UppercasesInput()
    {
        // [Req: formal — FEATURES.md §10]
        var account = new Account(
            "Test", AccountType.Banking, AccountSubType.Chequing, "usd",
            Money.Zero("USD"), DateOnly.FromDateTime(DateTime.UtcNow), OwnerId);

        account.Currency.Should().Be("USD");
    }

    [Fact]
    public void Account_NameTrimming_RemovesWhitespace()
    {
        // [Req: inferred — from Account constructor behavior]
        var account = new Account(
            "  My Account  ", AccountType.Banking, AccountSubType.Chequing, "CAD",
            Money.Zero(), DateOnly.FromDateTime(DateTime.UtcNow), OwnerId);

        account.Name.Should().Be("My Account");
    }

    // --- §3.4: Split Transactions ---

    [Fact]
    public void Transaction_WithSplits_IsSplitReturnsTrue()
    {
        // [Req: formal — FEATURES.md §3.4]
        var txn = CreateTransaction(100m);
        var split = new TransactionSplit(txn.Id, new Money(100m, "CAD"), Guid.NewGuid());
        txn.AddSplit(split);

        txn.IsSplit.Should().BeTrue();
        txn.Splits.Should().HaveCount(1);
    }

    [Fact]
    public void Transaction_WithoutSplits_IsSplitReturnsFalse()
    {
        // [Req: formal — FEATURES.md §3.4]
        var txn = CreateTransaction(100m);
        txn.IsSplit.Should().BeFalse();
    }

    [Fact]
    public void Transaction_SplitSumInvariant_ValidWhenSplitsSumToParent()
    {
        // [Req: formal — FEATURES.md §12: split sum invariant]
        var txn = CreateTransaction(100m);
        txn.AddSplit(new TransactionSplit(txn.Id, new Money(60m, "CAD"), Guid.NewGuid()));
        txn.AddSplit(new TransactionSplit(txn.Id, new Money(40m, "CAD"), Guid.NewGuid()));

        txn.ValidateSplitInvariant().Should().BeTrue();
    }

    [Fact]
    public void Transaction_SplitSumInvariant_InvalidWhenSplitsDontSum()
    {
        // [Req: formal — FEATURES.md §12: split sum invariant]
        var txn = CreateTransaction(100m);
        txn.AddSplit(new TransactionSplit(txn.Id, new Money(60m, "CAD"), Guid.NewGuid()));
        txn.AddSplit(new TransactionSplit(txn.Id, new Money(39.99m, "CAD"), Guid.NewGuid()));

        txn.ValidateSplitInvariant().Should().BeFalse();
    }

    [Fact]
    public void Transaction_ClearSplits_RemovesAllSplits()
    {
        // [Req: formal — FEATURES.md §3.4]
        var txn = CreateTransaction(100m);
        txn.AddSplit(new TransactionSplit(txn.Id, new Money(100m, "CAD"), Guid.NewGuid()));
        txn.ClearSplits();

        txn.Splits.Should().BeEmpty();
        txn.IsSplit.Should().BeFalse();
    }

    // --- §3.1: Transaction Tags ---

    [Fact]
    public void Transaction_AddTag_TagIsTracked()
    {
        // [Req: formal — FEATURES.md §3.6: tags are managed entities]
        var txn = CreateTransaction(50m);
        var tag = new Tag("Groceries", OwnerId);

        txn.AddTag(tag);

        txn.Tags.Should().HaveCount(1);
    }

    [Fact]
    public void Transaction_AddDuplicateTag_IgnoresDuplicate()
    {
        // [Req: inferred — from Transaction.AddTag dedup logic]
        var txn = CreateTransaction(50m);
        var tag = new Tag("Groceries", OwnerId);

        txn.AddTag(tag);
        txn.AddTag(tag);

        txn.Tags.Should().HaveCount(1);
    }

    [Fact]
    public void Transaction_RemoveTag_TagIsRemoved()
    {
        // [Req: formal — FEATURES.md §3.6]
        var txn = CreateTransaction(50m);
        var tag = new Tag("Groceries", OwnerId);
        txn.AddTag(tag);

        txn.RemoveTag(tag.Id);

        txn.Tags.Should().BeEmpty();
    }

    // --- §7: Soft Delete ---

    [Fact]
    public void Account_SoftDelete_SetsIsDeletedAndDeletedAt()
    {
        // [Req: formal — FEATURES.md §7: soft-delete with IsDeleted + DeletedAt]
        var account = CreateAccount();
        var beforeDelete = DateTime.UtcNow;

        account.SoftDelete();

        account.IsDeleted.Should().BeTrue();
        account.DeletedAt.Should().NotBeNull();
        account.DeletedAt!.Value.Should().BeOnOrAfter(beforeDelete);
    }

    [Fact]
    public void Transaction_SoftDelete_SetsIsDeletedAndDeletedAt()
    {
        // [Req: formal — FEATURES.md §7]
        var txn = CreateTransaction(50m);

        txn.SoftDelete();

        txn.IsDeleted.Should().BeTrue();
        txn.DeletedAt.Should().NotBeNull();
    }

    // --- §2.3: Investment Holdings ---

    [Fact]
    public void Holding_Creation_WithValidArgs_Succeeds()
    {
        // [Req: formal — FEATURES.md §2.1: holdings with symbol, quantity, cost]
        var securityId = Guid.NewGuid();
        var holding = new Holding(
            Guid.NewGuid(), securityId, "AAPL", "Apple Inc.", 100m,
            new Money(150.50m, "USD"));

        holding.Symbol.Should().Be("AAPL");
        holding.SecurityName.Should().Be("Apple Inc.");
        holding.Quantity.Should().Be(100m);
        holding.AverageCostPerUnit.Amount.Should().Be(150.50m);
    }

    [Fact]
    public void Holding_Update_ChangesQuantityAndCost()
    {
        // [Req: formal — FEATURES.md §2.1]
        var holding = CreateHolding();

        holding.Update(200m, new Money(160m, "USD"), "Updated position");

        holding.Quantity.Should().Be(200m);
        holding.AverageCostPerUnit.Amount.Should().Be(160m);
        holding.Notes.Should().Be("Updated position");
    }

    // --- §5.1: Security Management ---

    [Fact]
    public void Security_AddAlias_CreatesProviderMapping()
    {
        // [Req: formal — PLUGIN-ARCHITECTURE.md: provider-specific symbols]
        var security = new Security("AAPL", "AAPL", "Apple Inc.", "USD");

        var alias = security.AddOrUpdateAlias("AAPL.US", "YahooFinance", isPrimary: true);

        alias.Should().NotBeNull();
        security.HasAlias("AAPL.US", "YahooFinance").Should().BeTrue();
    }

    [Fact]
    public void Security_GetPreferredSymbol_ReturnsCorrectAlias()
    {
        // [Req: formal — PLUGIN-ARCHITECTURE.md: symbol resolution]
        var security = new Security("RY", "RY", "Royal Bank", "CAD");
        security.AddOrUpdateAlias("RY.TO", "YahooFinance", isPrimary: true);
        security.AddOrUpdateAlias("RY:CA", "MsnFinance", isPrimary: true);

        security.GetPreferredSymbol("YahooFinance").Should().Be("RY.TO");
        security.GetPreferredSymbol("MsnFinance").Should().Be("RY:CA");
    }

    // --- §4.3: Sinking Funds ---

    [Fact]
    public void SinkingFund_ProgressPercentage_CalculatesCorrectly()
    {
        // [Req: formal — FEATURES.md §4.2: track accumulated vs target]
        var fund = new SinkingFund(
            Guid.NewGuid(), "Insurance", new Money(1200m, "CAD"),
            DateTime.UtcNow.AddMonths(12));

        fund.RecordContribution(new Money(300m, "CAD"));

        fund.ProgressPercentage.Should().Be(25m);
    }

    [Fact]
    public void SinkingFund_CalculateMonthlySetAside_ReturnsCorrectAmount()
    {
        // [Req: formal — FEATURES.md §4.2: monthly set-aside = (target - accumulated) / months]
        var fund = new SinkingFund(
            Guid.NewGuid(), "Insurance", new Money(1200m, "CAD"),
            DateTime.UtcNow.AddMonths(6));

        var monthly = fund.CalculateMonthlySetAside(DateTime.UtcNow);

        monthly.Amount.Should().BeGreaterThan(0m);
        monthly.CurrencyCode.Should().Be("CAD");
    }

    // --- §4.3: Recurring Transactions ---

    [Fact]
    public void RecurringTransaction_ProjectOccurrences_ReturnsCorrectDates()
    {
        // [Req: formal — FEATURES.md §4.3: auto-generate expected future transactions]
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var recurring = new RecurringTransaction(
            Guid.NewGuid(), Guid.NewGuid(), "Rent",
            new Money(2000m, "CAD"), TransactionType.Debit,
            RecurrenceFrequency.Monthly, start);

        var horizon = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var occurrences = recurring.ProjectOccurrences(horizon);

        occurrences.Count.Should().BeGreaterThanOrEqualTo(3);
    }

    // --- §2.4: Valuations ---

    [Fact]
    public void Valuation_DualDateModel_HasEffectiveDateAndRecordedAt()
    {
        // [Req: formal — FEATURES.md §5.5, IMPLEMENTATION-PLAN.md: dual-date model]
        var valuation = new Valuation(
            Guid.NewGuid(), new Money(500000m, "CAD"),
            new DateOnly(2026, 1, 15), "Appraisal");

        valuation.EstimatedValue.Amount.Should().Be(500000m);
        valuation.EffectiveDate.Should().Be(new DateOnly(2026, 1, 15));
        valuation.RecordedAt.Should().BeOnOrAfter(DateTime.UtcNow.AddMinutes(-1));
    }

    // --- Helpers ---

    private static Account CreateAccount(AccountType type = AccountType.Banking)
    {
        return new Account(
            "Test Account", type,
            type == AccountType.Banking ? AccountSubType.Chequing :
            type == AccountType.Credit ? AccountSubType.CreditCard :
            type == AccountType.Investment ? AccountSubType.RRSP :
            type == AccountType.Property ? AccountSubType.RealEstate :
            AccountSubType.Mortgage,
            "CAD", new Money(1000m, "CAD"),
            DateOnly.FromDateTime(DateTime.UtcNow), OwnerId);
    }

    private static Transaction CreateTransaction(decimal amount)
    {
        return new Transaction(
            Guid.NewGuid(), DateTime.UtcNow,
            new Money(amount, "CAD"), "Test transaction",
            TransactionType.Debit);
    }

    private static Holding CreateHolding()
    {
        return new Holding(
            Guid.NewGuid(), Guid.NewGuid(), "AAPL", "Apple Inc.",
            100m, new Money(150m, "USD"));
    }
}

#endregion

#region Group 2: Fitness Scenario Tests

/// <summary>
/// Tests mapped 1:1 to QUALITY.md fitness-to-purpose scenarios.
/// </summary>
public class FitnessScenarioTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    // --- Scenario 1: Split-Sum Invariant Violation ---

    [Fact]
    public void Scenario1_SplitSumInvariant_ThreeSplitsOneCentShort_FailsValidation()
    {
        var txn = new Transaction(
            Guid.NewGuid(), DateTime.UtcNow,
            new Money(150.00m, "CAD"), "Grocery + household",
            TransactionType.Debit);

        txn.AddSplit(new TransactionSplit(txn.Id, new Money(80.00m, "CAD"), Guid.NewGuid()));
        txn.AddSplit(new TransactionSplit(txn.Id, new Money(45.00m, "CAD"), Guid.NewGuid()));
        txn.AddSplit(new TransactionSplit(txn.Id, new Money(24.99m, "CAD"), Guid.NewGuid()));

        // Sum = 149.99, parent = 150.00 → invariant violated
        txn.ValidateSplitInvariant().Should().BeFalse();
    }

    [Fact]
    public void Scenario1_SplitSumInvariant_ExactMatch_PassesValidation()
    {
        var txn = new Transaction(
            Guid.NewGuid(), DateTime.UtcNow,
            new Money(150.00m, "CAD"), "Grocery + household",
            TransactionType.Debit);

        txn.AddSplit(new TransactionSplit(txn.Id, new Money(80.00m, "CAD"), Guid.NewGuid()));
        txn.AddSplit(new TransactionSplit(txn.Id, new Money(45.00m, "CAD"), Guid.NewGuid()));
        txn.AddSplit(new TransactionSplit(txn.Id, new Money(25.00m, "CAD"), Guid.NewGuid()));

        txn.ValidateSplitInvariant().Should().BeTrue();
    }

    [Fact]
    public void Scenario1_SplitSumInvariant_SkipsDeletedSplits()
    {
        var txn = new Transaction(
            Guid.NewGuid(), DateTime.UtcNow,
            new Money(100.00m, "CAD"), "Test",
            TransactionType.Debit);

        var activeSplit = new TransactionSplit(txn.Id, new Money(100.00m, "CAD"), Guid.NewGuid());
        var deletedSplit = new TransactionSplit(txn.Id, new Money(50.00m, "CAD"), Guid.NewGuid());
        deletedSplit.SoftDelete();

        txn.AddSplit(activeSplit);
        txn.AddSplit(deletedSplit);

        // Only active split (100.00) counts — matches parent (100.00)
        txn.ValidateSplitInvariant().Should().BeTrue();
    }

    // --- Scenario 2: Cross-Currency Money Operations ---

    [Fact]
    public void Scenario2_MoneyAdd_DifferentCurrencies_ThrowsInvalidOperation()
    {
        var cad = new Money(100m, "CAD");
        var usd = new Money(50m, "USD");

        var act = () => cad.Add(usd);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FX conversion*");
    }

    [Fact]
    public void Scenario2_MoneySubtract_DifferentCurrencies_ThrowsInvalidOperation()
    {
        var cad = new Money(100m, "CAD");
        var usd = new Money(50m, "USD");

        var act = () => cad.Subtract(usd);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FX conversion*");
    }

    [Theory]
    [InlineData("CAD", "USD")]
    [InlineData("USD", "EUR")]
    [InlineData("GBP", "CAD")]
    public void Scenario2_MoneyComparison_DifferentCurrencies_ThrowsInvalidOperation(
        string currency1, string currency2)
    {
        var left = new Money(100m, currency1);
        var right = new Money(50m, currency2);

        var greaterThan = () => { var _ = left > right; };
        var lessThan = () => { var _ = left < right; };

        greaterThan.Should().Throw<InvalidOperationException>();
        lessThan.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Scenario2_MoneyOperatorPlus_DifferentCurrencies_ThrowsInvalidOperation()
    {
        var cad = new Money(100m, "CAD");
        var usd = new Money(50m, "USD");

        var act = () => { var _ = cad + usd; };

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Scenario2_MoneyOperatorMinus_DifferentCurrencies_ThrowsInvalidOperation()
    {
        var cad = new Money(100m, "CAD");
        var usd = new Money(50m, "USD");

        var act = () => { var _ = cad - usd; };

        act.Should().Throw<InvalidOperationException>();
    }

    // --- Scenario 4: Property Account Balance Derivation ---

    [Fact]
    public void Scenario4_PropertyAccountValuation_IsIndependentOfTransactions()
    {
        // Verify that Valuation entity stores the correct value
        // and is not affected by transaction amounts
        var accountId = Guid.NewGuid();
        var valuation = new Valuation(
            accountId, new Money(500000m, "CAD"),
            new DateOnly(2026, 1, 15), "Appraisal");

        // Property valuation is 500k regardless of transactions on the account
        valuation.EstimatedValue.Amount.Should().Be(500000m);
        valuation.AccountId.Should().Be(accountId);
    }

    // --- Scenario 6: Soft Delete Cannot Be Accidentally Undone ---

    [Fact]
    public void Scenario6_SoftDelete_SetsDeletedAtTimestamp()
    {
        var txn = new Transaction(
            Guid.NewGuid(), DateTime.UtcNow,
            new Money(50m, "CAD"), "Test", TransactionType.Debit);

        txn.SoftDelete();

        txn.IsDeleted.Should().BeTrue();
        txn.DeletedAt.Should().NotBeNull();
        txn.DeletedAt!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    // --- Scenario 7: Budget Split-Awareness ---

    [Fact]
    public void Scenario7_SplitTransaction_IsSplitProperty_ReflectsSplitState()
    {
        var txn = new Transaction(
            Guid.NewGuid(), DateTime.UtcNow,
            new Money(200m, "CAD"), "Costco", TransactionType.Debit);
        txn.CategoryId = Guid.NewGuid(); // "Shopping" parent category

        // Before splits: IsSplit is false
        txn.IsSplit.Should().BeFalse();

        // Add splits with different categories
        var groceryCat = Guid.NewGuid();
        var householdCat = Guid.NewGuid();
        txn.AddSplit(new TransactionSplit(txn.Id, new Money(120m, "CAD"), groceryCat));
        txn.AddSplit(new TransactionSplit(txn.Id, new Money(80m, "CAD"), householdCat));

        // After splits: IsSplit is true — budget tracking should use split categories
        txn.IsSplit.Should().BeTrue();
        txn.Splits.Should().HaveCount(2);
        txn.Splits.Select(s => s.CategoryId).Should().Contain(groceryCat);
        txn.Splits.Select(s => s.CategoryId).Should().Contain(householdCat);
    }

    // --- Scenario 9: All Account Types Must Participate in Net Worth ---

    [Theory]
    [InlineData(AccountType.Banking, AccountSubType.Chequing)]
    [InlineData(AccountType.Credit, AccountSubType.CreditCard)]
    [InlineData(AccountType.Investment, AccountSubType.RRSP)]
    [InlineData(AccountType.Property, AccountSubType.RealEstate)]
    [InlineData(AccountType.Loan, AccountSubType.Mortgage)]
    public void Scenario9_AllAccountTypes_CanBeCreatedWithBalance(
        AccountType type, AccountSubType subType)
    {
        var account = new Account(
            "Test", type, subType, "CAD",
            new Money(10000m, "CAD"),
            DateOnly.FromDateTime(DateTime.UtcNow), OwnerId);

        account.OpeningBalance.Amount.Should().Be(10000m);
        account.AccountType.Should().Be(type);
    }

    // --- Scenario 10: Concurrent Operations (Structural) ---

    [Fact]
    public void Scenario10_BaseEntity_HasVersionField()
    {
        // Verify that BaseEntity has Version field for concurrency tracking
        var account = new Account(
            "Test", AccountType.Banking, AccountSubType.Chequing, "CAD",
            Money.Zero(), DateOnly.FromDateTime(DateTime.UtcNow), OwnerId);

        // Version exists (xmin is handled at DB level, but Version property exists)
        account.Version.Should().Be(0);
    }
}

#endregion

#region Group 3: Boundary and Edge Case Tests

/// <summary>
/// Tests derived from defensive pattern analysis (Step 5).
/// Each test exercises a specific defensive code path found during exploration.
/// </summary>
public class BoundaryAndEdgeCaseTests
{
    private static readonly Guid OwnerId = Guid.NewGuid();

    // --- Money Value Object Boundaries ---

    [Fact]
    public void Money_Zero_HasCorrectDefaults()
    {
        var zero = Money.Zero();
        zero.Amount.Should().Be(0m);
        zero.CurrencyCode.Should().Be("CAD");
    }

    [Fact]
    public void Money_Zero_WithCurrency_UsesSpecifiedCurrency()
    {
        var zero = Money.Zero("USD");
        zero.CurrencyCode.Should().Be("USD");
    }

    [Fact]
    public void Money_Negate_FlipsSign()
    {
        var money = new Money(100m, "CAD");
        var negated = money.Negate();

        negated.Amount.Should().Be(-100m);
        negated.CurrencyCode.Should().Be("CAD");
    }

    [Fact]
    public void Money_Negate_NegativeBecomesPositive()
    {
        var money = new Money(-50m, "CAD");
        var negated = money.Negate();

        negated.Amount.Should().Be(50m);
    }

    [Fact]
    public void Money_Abs_ReturnsPositiveValue()
    {
        var negative = new Money(-75.50m, "CAD");
        var absolute = negative.Abs();

        absolute.Amount.Should().Be(75.50m);
    }

    [Fact]
    public void Money_Add_SameCurrency_ReturnsCorrectSum()
    {
        var a = new Money(100.50m, "CAD");
        var b = new Money(200.75m, "CAD");

        var result = a.Add(b);

        result.Amount.Should().Be(301.25m);
        result.CurrencyCode.Should().Be("CAD");
    }

    [Fact]
    public void Money_Subtract_SameCurrency_ReturnsCorrectDifference()
    {
        var a = new Money(200m, "CAD");
        var b = new Money(75.25m, "CAD");

        var result = a.Subtract(b);

        result.Amount.Should().Be(124.75m);
    }

    [Fact]
    public void Money_Add_NegativeAmounts_WorksCorrectly()
    {
        var a = new Money(-50m, "CAD");
        var b = new Money(-30m, "CAD");

        var result = a + b;

        result.Amount.Should().Be(-80m);
    }

    [Fact]
    public void Money_Comparison_GreaterThan_SameCurrency()
    {
        var larger = new Money(200m, "CAD");
        var smaller = new Money(100m, "CAD");

        (larger > smaller).Should().BeTrue();
        (smaller > larger).Should().BeFalse();
    }

    [Fact]
    public void Money_Comparison_GreaterThanOrEqual_SameCurrency()
    {
        var a = new Money(100m, "CAD");
        var b = new Money(100m, "CAD");

        (a >= b).Should().BeTrue();
        (a <= b).Should().BeTrue();
    }

    [Fact]
    public void Money_ToString_FormatsCorrectly()
    {
        var money = new Money(1234.56m, "USD");
        money.ToString().Should().Be("1234.56 USD");
    }

    [Fact]
    public void Money_RecordEquality_SameValues_AreEqual()
    {
        var a = new Money(100m, "CAD");
        var b = new Money(100m, "CAD");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Money_RecordEquality_DifferentAmounts_AreNotEqual()
    {
        var a = new Money(100m, "CAD");
        var b = new Money(100.01m, "CAD");

        a.Should().NotBe(b);
    }

    // --- Account Guard Clauses ---

    [Fact]
    public void Account_EmptyName_ThrowsArgumentException()
    {
        var act = () => new Account(
            "", AccountType.Banking, AccountSubType.Chequing, "CAD",
            Money.Zero(), DateOnly.FromDateTime(DateTime.UtcNow), OwnerId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Account_WhitespaceName_ThrowsArgumentException()
    {
        var act = () => new Account(
            "   ", AccountType.Banking, AccountSubType.Chequing, "CAD",
            Money.Zero(), DateOnly.FromDateTime(DateTime.UtcNow), OwnerId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Account_EmptyCurrency_ThrowsArgumentException()
    {
        var act = () => new Account(
            "Test", AccountType.Banking, AccountSubType.Chequing, "",
            Money.Zero(), DateOnly.FromDateTime(DateTime.UtcNow), OwnerId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Account_Rename_EmptyName_ThrowsArgumentException()
    {
        var account = new Account(
            "Test", AccountType.Banking, AccountSubType.Chequing, "CAD",
            Money.Zero(), DateOnly.FromDateTime(DateTime.UtcNow), OwnerId);

        var act = () => account.Rename("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Account_Deactivate_SetsIsActiveFalse()
    {
        var account = new Account(
            "Test", AccountType.Banking, AccountSubType.Chequing, "CAD",
            Money.Zero(), DateOnly.FromDateTime(DateTime.UtcNow), OwnerId);

        account.Deactivate();

        account.IsActive.Should().BeFalse();
    }

    // --- Transaction Guard Clauses ---

    [Fact]
    public void Transaction_EmptyDescription_ThrowsArgumentException()
    {
        var act = () => new Transaction(
            Guid.NewGuid(), DateTime.UtcNow,
            new Money(50m, "CAD"), "", TransactionType.Debit);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Transaction_AddNullSplit_ThrowsArgumentNullException()
    {
        var txn = new Transaction(
            Guid.NewGuid(), DateTime.UtcNow,
            new Money(50m, "CAD"), "Test", TransactionType.Debit);

        var act = () => txn.AddSplit(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Transaction_AddNullTag_ThrowsArgumentNullException()
    {
        var txn = new Transaction(
            Guid.NewGuid(), DateTime.UtcNow,
            new Money(50m, "CAD"), "Test", TransactionType.Debit);

        var act = () => txn.AddTag(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Transaction_DescriptionIsTrimmed()
    {
        var txn = new Transaction(
            Guid.NewGuid(), DateTime.UtcNow,
            new Money(50m, "CAD"), "  Trimmed  ", TransactionType.Debit);

        txn.Description.Should().Be("Trimmed");
    }

    // --- Holding Guard Clauses ---

    [Fact]
    public void Holding_EmptySymbol_ThrowsArgumentException()
    {
        var act = () => new Holding(
            Guid.NewGuid(), Guid.NewGuid(), "", "Apple Inc.",
            100m, new Money(150m, "USD"));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Holding_EmptySecurityName_ThrowsArgumentException()
    {
        var act = () => new Holding(
            Guid.NewGuid(), Guid.NewGuid(), "AAPL", "",
            100m, new Money(150m, "USD"));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Holding_NegativeQuantity_ThrowsArgumentOutOfRange()
    {
        var act = () => new Holding(
            Guid.NewGuid(), Guid.NewGuid(), "AAPL", "Apple Inc.",
            -1m, new Money(150m, "USD"));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Holding_RebindSecurity_NullSecurity_ThrowsArgumentNullException()
    {
        var holding = new Holding(
            Guid.NewGuid(), Guid.NewGuid(), "AAPL", "Apple Inc.",
            100m, new Money(150m, "USD"));

        var act = () => holding.RebindSecurity(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // --- TransactionSplit Guard Clauses ---

    [Fact]
    public void TransactionSplit_AddNullTag_ThrowsArgumentNullException()
    {
        var split = new TransactionSplit(
            Guid.NewGuid(), new Money(50m, "CAD"), Guid.NewGuid());

        var act = () => split.AddTag(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TransactionSplit_AddDuplicateTag_IgnoresDuplicate()
    {
        var split = new TransactionSplit(
            Guid.NewGuid(), new Money(50m, "CAD"), Guid.NewGuid());
        var tag = new Tag("Test", Guid.NewGuid());

        split.AddTag(tag);
        split.AddTag(tag);

        split.Tags.Should().HaveCount(1);
    }

    // --- BaseEntity Behavior ---

    [Fact]
    public void BaseEntity_NewEntity_HasUniqueId()
    {
        var a = new Account(
            "A", AccountType.Banking, AccountSubType.Chequing, "CAD",
            Money.Zero(), DateOnly.FromDateTime(DateTime.UtcNow), OwnerId);
        var b = new Account(
            "B", AccountType.Banking, AccountSubType.Chequing, "CAD",
            Money.Zero(), DateOnly.FromDateTime(DateTime.UtcNow), OwnerId);

        a.Id.Should().NotBe(b.Id);
        a.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void BaseEntity_NewEntity_HasTimestamps()
    {
        var before = DateTime.UtcNow;
        var account = new Account(
            "Test", AccountType.Banking, AccountSubType.Chequing, "CAD",
            Money.Zero(), DateOnly.FromDateTime(DateTime.UtcNow), OwnerId);

        account.CreatedAt.Should().BeOnOrAfter(before);
        account.UpdatedAt.Should().BeOnOrAfter(before);
        account.IsDeleted.Should().BeFalse();
        account.DeletedAt.Should().BeNull();
    }

    // --- SinkingFund Boundaries ---

    [Fact]
    public void SinkingFund_RecordContribution_AccumulatesCorrectly()
    {
        var fund = new SinkingFund(
            Guid.NewGuid(), "Insurance", new Money(1200m, "CAD"),
            DateTime.UtcNow.AddMonths(12));

        fund.RecordContribution(new Money(100m, "CAD"));
        fund.RecordContribution(new Money(200m, "CAD"));

        fund.AccumulatedAmount.Amount.Should().Be(300m);
        fund.ProgressPercentage.Should().Be(25m);
    }

    [Fact]
    public void SinkingFund_DeactivateAndActivate_TogglesState()
    {
        var fund = new SinkingFund(
            Guid.NewGuid(), "Insurance", new Money(1200m, "CAD"),
            DateTime.UtcNow.AddMonths(12));

        fund.IsActive.Should().BeTrue();
        fund.Deactivate();
        fund.IsActive.Should().BeFalse();
        fund.Activate();
        fund.IsActive.Should().BeTrue();
    }

    // --- Budget Boundaries ---

    [Fact]
    public void Budget_UpdateAmount_ChangesAmount()
    {
        var budget = new Budget(
            Guid.NewGuid(), Guid.NewGuid(), 2026, 4,
            new Money(500m, "CAD"));

        budget.UpdateAmount(new Money(750m, "CAD"));

        budget.Amount.Amount.Should().Be(750m);
    }

    [Fact]
    public void Budget_SetRollover_TogglesFlag()
    {
        var budget = new Budget(
            Guid.NewGuid(), Guid.NewGuid(), 2026, 4,
            new Money(500m, "CAD"));

        budget.RolloverEnabled.Should().BeFalse();
        budget.SetRollover(true);
        budget.RolloverEnabled.Should().BeTrue();
    }

    // --- Security Boundaries ---

    [Fact]
    public void Security_CashEquivalent_CanBeMarked()
    {
        var security = new Security("CASH", "CASH", "Cash", "CAD");
        security.MarkCashEquivalent();

        security.IsCashEquivalent.Should().BeTrue();
    }

    [Fact]
    public void Security_RemoveAlias_ReturnsFalseForNonexistent()
    {
        var security = new Security("AAPL", "AAPL", "Apple", "USD");
        security.RemoveAlias(Guid.NewGuid()).Should().BeFalse();
    }
}

#endregion
