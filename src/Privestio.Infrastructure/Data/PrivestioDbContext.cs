using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Privestio.Domain.Entities;
using Privestio.Infrastructure.Identity;

namespace Privestio.Infrastructure.Data;

/// <summary>
/// The main EF Core DbContext for Privestio.
/// Combines Identity and domain entities in a single PostgreSQL database.
/// </summary>
public class PrivestioDbContext : IdentityDbContext<ApplicationUser>
{
    public PrivestioDbContext(DbContextOptions<PrivestioDbContext> options)
        : base(options) { }

    // Domain entities
    public DbSet<User> DomainUsers => Set<User>();
    public DbSet<Household> Households => Set<Household>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<TransactionSplit> TransactionSplits => Set<TransactionSplit>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TransactionTag> TransactionTags => Set<TransactionTag>();
    public DbSet<TransactionSplitTag> TransactionSplitTags => Set<TransactionSplitTag>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Payee> Payees => Set<Payee>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();
    public DbSet<Valuation> Valuations => Set<Valuation>();
    public DbSet<ImportMapping> ImportMappings => Set<ImportMapping>();
    public DbSet<CategorizationRule> CategorizationRules => Set<CategorizationRule>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<SinkingFund> SinkingFunds => Set<SinkingFund>();
    public DbSet<RecurringTransaction> RecurringTransactions => Set<RecurringTransaction>();

    // Phase 4 entities
    public DbSet<ForecastScenario> ForecastScenarios => Set<ForecastScenario>();
    public DbSet<ReconciliationPeriod> ReconciliationPeriods => Set<ReconciliationPeriod>();
    public DbSet<ContributionRoom> ContributionRooms => Set<ContributionRoom>();
    public DbSet<AmortizationEntry> AmortizationEntries => Set<AmortizationEntry>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<FxConversion> FxConversions => Set<FxConversion>();

    // Sync engine entities
    public DbSet<SyncTombstone> SyncTombstones => Set<SyncTombstone>();
    public DbSet<SyncCheckpoint> SyncCheckpoints => Set<SyncCheckpoint>();
    public DbSet<SyncConflict> SyncConflicts => Set<SyncConflict>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrivestioDbContext).Assembly);

        // Global soft-delete query filters
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Household>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Account>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Transaction>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TransactionSplit>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Category>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Tag>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TransactionTag>().HasQueryFilter(e => !e.Tag!.IsDeleted);
        modelBuilder.Entity<TransactionSplitTag>().HasQueryFilter(e => !e.Tag!.IsDeleted);
        modelBuilder.Entity<Payee>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ImportBatch>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Notification>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PriceHistory>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Valuation>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ImportMapping>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CategorizationRule>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Budget>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SinkingFund>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RecurringTransaction>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ForecastScenario>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ReconciliationPeriod>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ContributionRoom>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AmortizationEntry>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ExchangeRate>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<FxConversion>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SyncTombstone>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SyncCheckpoint>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SyncConflict>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<IdempotencyRecord>().HasQueryFilter(e => !e.IsDeleted);

        // Configure Version as concurrency token for all BaseEntity types
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder
                    .Entity(entityType.ClrType)
                    .Property(nameof(BaseEntity.Version))
                    .IsConcurrencyToken();
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<Domain.Entities.BaseEntity>();
        var utcNow = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }
    }
}
