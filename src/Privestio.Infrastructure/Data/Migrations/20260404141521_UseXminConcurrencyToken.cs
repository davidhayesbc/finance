using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Privestio.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UseXminConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Valuations",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "TransactionSplits",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Transactions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Tags",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "SyncTombstones",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "SyncConflicts",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "SyncCheckpoints",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "SinkingFunds",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "SecurityIdentifiers",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "SecurityAliases",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Securities",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "RecurringTransactions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ReconciliationPeriods",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "PriceHistories",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Payees",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Notifications",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Lots",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ImportMappings",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ImportBatches",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "IdempotencyRecords",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Households",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "HoldingSnapshots",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Holdings",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "FxConversions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ForecastScenarios",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ExchangeRates",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "DomainUsers",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "ContributionRooms",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "CategorizationRules",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Categories",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Budgets",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "AuditEvents",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "AmortizationEntries",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Accounts",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Valuations");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "TransactionSplits");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "SyncTombstones");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "SyncConflicts");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "SyncCheckpoints");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "SinkingFunds");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "SecurityIdentifiers");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "SecurityAliases");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Securities");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "RecurringTransactions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ReconciliationPeriods");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "PriceHistories");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Payees");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Lots");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ImportMappings");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ImportBatches");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "IdempotencyRecords");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Households");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "HoldingSnapshots");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Holdings");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "FxConversions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ForecastScenarios");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ExchangeRates");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "DomainUsers");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "ContributionRooms");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "CategorizationRules");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Budgets");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "AmortizationEntries");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Accounts");
        }
    }
}
