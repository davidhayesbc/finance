using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Privestio.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHoldingSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HoldingSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SecurityName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    AsOfDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MarketValueAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MarketValueCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    UnitPriceAmount = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    UnitPriceCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoldingSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HoldingSnapshots_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HoldingSnapshots_Securities_SecurityId",
                        column: x => x.SecurityId,
                        principalTable: "Securities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HoldingSnapshots_AccountId_AsOfDate",
                table: "HoldingSnapshots",
                columns: new[] { "AccountId", "AsOfDate" });

            migrationBuilder.CreateIndex(
                name: "IX_HoldingSnapshots_AccountId_SecurityId_AsOfDate",
                table: "HoldingSnapshots",
                columns: new[] { "AccountId", "SecurityId", "AsOfDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HoldingSnapshots_SecurityId",
                table: "HoldingSnapshots",
                column: "SecurityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HoldingSnapshots");
        }
    }
}
