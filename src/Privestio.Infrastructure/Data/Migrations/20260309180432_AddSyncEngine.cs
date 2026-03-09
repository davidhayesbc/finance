using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Privestio.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Valuations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "TransactionSplits",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Transactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Tags",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "SinkingFunds",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "RecurringTransactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "ReconciliationPeriods",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "PriceHistories",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Payees",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Notifications",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "ImportMappings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "ImportBatches",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Households",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "FxConversions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "ForecastScenarios",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "ExchangeRates",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "DomainUsers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "ContributionRooms",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "CategorizationRules",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Categories",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Budgets",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "AuditEvents",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "AmortizationEntries",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Accounts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L
            );

            migrationBuilder.CreateTable(
                name: "IdempotencyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationId = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    ResponseData = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdempotencyRecords", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "SyncCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    LastSyncToken = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncCheckpoints", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "SyncConflicts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalData = table.Column<string>(type: "text", nullable: false),
                    ServerData = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    DetectedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    ResolvedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    Resolution = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: true
                    ),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncConflicts", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "SyncTombstones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAtUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    SyncedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncTombstones", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_ExpiresAt",
                table: "IdempotencyRecords",
                column: "ExpiresAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_OperationId",
                table: "IdempotencyRecords",
                column: "OperationId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_SyncCheckpoints_UserId_DeviceId",
                table: "SyncCheckpoints",
                columns: new[] { "UserId", "DeviceId" },
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_SyncConflicts_EntityType_EntityId",
                table: "SyncConflicts",
                columns: new[] { "EntityType", "EntityId" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_SyncConflicts_Status",
                table: "SyncConflicts",
                column: "Status"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SyncTombstones_DeletedAtUtc",
                table: "SyncTombstones",
                column: "DeletedAtUtc"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SyncTombstones_EntityId",
                table: "SyncTombstones",
                column: "EntityId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_SyncTombstones_EntityType_EntityId",
                table: "SyncTombstones",
                columns: new[] { "EntityType", "EntityId" }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "IdempotencyRecords");

            migrationBuilder.DropTable(name: "SyncCheckpoints");

            migrationBuilder.DropTable(name: "SyncConflicts");

            migrationBuilder.DropTable(name: "SyncTombstones");

            migrationBuilder.DropColumn(name: "Version", table: "Valuations");

            migrationBuilder.DropColumn(name: "Version", table: "TransactionSplits");

            migrationBuilder.DropColumn(name: "Version", table: "Transactions");

            migrationBuilder.DropColumn(name: "Version", table: "Tags");

            migrationBuilder.DropColumn(name: "Version", table: "SinkingFunds");

            migrationBuilder.DropColumn(name: "Version", table: "RecurringTransactions");

            migrationBuilder.DropColumn(name: "Version", table: "ReconciliationPeriods");

            migrationBuilder.DropColumn(name: "Version", table: "PriceHistories");

            migrationBuilder.DropColumn(name: "Version", table: "Payees");

            migrationBuilder.DropColumn(name: "Version", table: "Notifications");

            migrationBuilder.DropColumn(name: "Version", table: "ImportMappings");

            migrationBuilder.DropColumn(name: "Version", table: "ImportBatches");

            migrationBuilder.DropColumn(name: "Version", table: "Households");

            migrationBuilder.DropColumn(name: "Version", table: "FxConversions");

            migrationBuilder.DropColumn(name: "Version", table: "ForecastScenarios");

            migrationBuilder.DropColumn(name: "Version", table: "ExchangeRates");

            migrationBuilder.DropColumn(name: "Version", table: "DomainUsers");

            migrationBuilder.DropColumn(name: "Version", table: "ContributionRooms");

            migrationBuilder.DropColumn(name: "Version", table: "CategorizationRules");

            migrationBuilder.DropColumn(name: "Version", table: "Categories");

            migrationBuilder.DropColumn(name: "Version", table: "Budgets");

            migrationBuilder.DropColumn(name: "Version", table: "AuditEvents");

            migrationBuilder.DropColumn(name: "Version", table: "AmortizationEntries");

            migrationBuilder.DropColumn(name: "Version", table: "Accounts");
        }
    }
}
