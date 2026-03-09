using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Privestio.Domain.ValueObjects;

#nullable disable

namespace Privestio.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase4Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AmortizationEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentNumber = table.Column<int>(type: "integer", nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InterestAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    InterestCurrency = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
                        nullable: false
                    ),
                    PaymentAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PaymentCurrency = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
                        nullable: false
                    ),
                    PrincipalAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PrincipalCurrency = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
                        nullable: false
                    ),
                    RemainingBalanceAmount = table.Column<decimal>(
                        type: "numeric(18,4)",
                        nullable: false
                    ),
                    RemainingBalanceCurrency = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
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
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AmortizationEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AmortizationEntries_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ContributionRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    AnnualLimitAmount = table.Column<decimal>(
                        type: "numeric(18,4)",
                        nullable: false
                    ),
                    AnnualLimitCurrency = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
                        nullable: false
                    ),
                    CarryForwardAmount = table.Column<decimal>(
                        type: "numeric(18,4)",
                        nullable: false
                    ),
                    CarryForwardCurrency = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
                        nullable: false
                    ),
                    ContributionsYtdAmount = table.Column<decimal>(
                        type: "numeric(18,4)",
                        nullable: false
                    ),
                    ContributionsYtdCurrency = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
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
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContributionRooms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContributionRooms_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromCurrency = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
                        nullable: false
                    ),
                    ToCurrency = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
                        nullable: false
                    ),
                    Rate = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    AsOfDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RecordedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    Source = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
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
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "ForecastScenarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Description = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000,
                        nullable: true
                    ),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    GrowthAssumptions = table.Column<IReadOnlyList<GrowthAssumption>>(
                        type: "jsonb",
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
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastScenarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForecastScenarios_DomainUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "DomainUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ReconciliationPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    StatementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    LockedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    LockedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnlockReason = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    Notes = table.Column<string>(
                        type: "character varying(1000)",
                        maxLength: 1000,
                        nullable: true
                    ),
                    StatementBalanceAmount = table.Column<decimal>(
                        type: "numeric(18,4)",
                        nullable: false
                    ),
                    StatementBalanceCurrency = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
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
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReconciliationPeriods_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "FxConversions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExchangeRateId = table.Column<Guid>(type: "uuid", nullable: false),
                    AppliedRate = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    ConvertedAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ConvertedCurrency = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
                        nullable: false
                    ),
                    OriginalAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    OriginalCurrency = table.Column<string>(
                        type: "character varying(3)",
                        maxLength: 3,
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
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FxConversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FxConversions_ExchangeRates_ExchangeRateId",
                        column: x => x.ExchangeRateId,
                        principalTable: "ExchangeRates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict
                    );
                    table.ForeignKey(
                        name: "FK_FxConversions_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_AmortizationEntries_AccountId_PaymentNumber",
                table: "AmortizationEntries",
                columns: new[] { "AccountId", "PaymentNumber" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ContributionRooms_AccountId_Year",
                table: "ContributionRooms",
                columns: new[] { "AccountId", "Year" },
                unique: true,
                filter: "\"IsDeleted\" = false"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_AsOfDate",
                table: "ExchangeRates",
                column: "AsOfDate"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_FromCurrency_ToCurrency_AsOfDate",
                table: "ExchangeRates",
                columns: new[] { "FromCurrency", "ToCurrency", "AsOfDate" },
                unique: true,
                filter: "\"IsDeleted\" = false"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ForecastScenarios_UserId",
                table: "ForecastScenarios",
                column: "UserId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ForecastScenarios_UserId_IsDefault",
                table: "ForecastScenarios",
                columns: new[] { "UserId", "IsDefault" },
                filter: "\"IsDefault\" = true AND \"IsDeleted\" = false"
            );

            migrationBuilder.CreateIndex(
                name: "IX_FxConversions_ExchangeRateId",
                table: "FxConversions",
                column: "ExchangeRateId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_FxConversions_TransactionId",
                table: "FxConversions",
                column: "TransactionId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationPeriods_AccountId_StatementDate",
                table: "ReconciliationPeriods",
                columns: new[] { "AccountId", "StatementDate" }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationPeriods_AccountId_Status",
                table: "ReconciliationPeriods",
                columns: new[] { "AccountId", "Status" },
                filter: "\"IsDeleted\" = false"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AmortizationEntries");

            migrationBuilder.DropTable(name: "ContributionRooms");

            migrationBuilder.DropTable(name: "ForecastScenarios");

            migrationBuilder.DropTable(name: "FxConversions");

            migrationBuilder.DropTable(name: "ReconciliationPeriods");

            migrationBuilder.DropTable(name: "ExchangeRates");
        }
    }
}
