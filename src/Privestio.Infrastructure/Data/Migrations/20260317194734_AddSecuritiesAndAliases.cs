using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Privestio.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSecuritiesAndAliases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PriceHistories_Symbol_AsOfDate",
                table: "PriceHistories");

            migrationBuilder.DropIndex(
                name: "IX_Holdings_AccountId_Symbol",
                table: "Holdings");

            migrationBuilder.AddColumn<string>(
                name: "ProviderSymbol",
                table: "PriceHistories",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SecurityId",
                table: "PriceHistories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SecurityId",
                table: "Holdings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Securities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CanonicalSymbol = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DisplaySymbol = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Exchange = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsCashEquivalent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Securities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityAliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Symbol = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityAliases_Securities_SecurityId",
                        column: x => x.SecurityId,
                        principalTable: "Securities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_SecurityId_AsOfDate",
                table: "PriceHistories",
                columns: new[] { "SecurityId", "AsOfDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Holdings_AccountId_SecurityId",
                table: "Holdings",
                columns: new[] { "AccountId", "SecurityId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Holdings_SecurityId",
                table: "Holdings",
                column: "SecurityId");

            migrationBuilder.CreateIndex(
                name: "IX_Securities_CanonicalSymbol",
                table: "Securities",
                column: "CanonicalSymbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Securities_DisplaySymbol",
                table: "Securities",
                column: "DisplaySymbol");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAliases_SecurityId_Symbol_Source",
                table: "SecurityAliases",
                columns: new[] { "SecurityId", "Symbol", "Source" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAliases_Symbol",
                table: "SecurityAliases",
                column: "Symbol");

            migrationBuilder.AddForeignKey(
                name: "FK_Holdings_Securities_SecurityId",
                table: "Holdings",
                column: "SecurityId",
                principalTable: "Securities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PriceHistories_Securities_SecurityId",
                table: "PriceHistories",
                column: "SecurityId",
                principalTable: "Securities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Holdings_Securities_SecurityId",
                table: "Holdings");

            migrationBuilder.DropForeignKey(
                name: "FK_PriceHistories_Securities_SecurityId",
                table: "PriceHistories");

            migrationBuilder.DropTable(
                name: "SecurityAliases");

            migrationBuilder.DropTable(
                name: "Securities");

            migrationBuilder.DropIndex(
                name: "IX_PriceHistories_SecurityId_AsOfDate",
                table: "PriceHistories");

            migrationBuilder.DropIndex(
                name: "IX_Holdings_AccountId_SecurityId",
                table: "Holdings");

            migrationBuilder.DropIndex(
                name: "IX_Holdings_SecurityId",
                table: "Holdings");

            migrationBuilder.DropColumn(
                name: "ProviderSymbol",
                table: "PriceHistories");

            migrationBuilder.DropColumn(
                name: "SecurityId",
                table: "PriceHistories");

            migrationBuilder.DropColumn(
                name: "SecurityId",
                table: "Holdings");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_Symbol_AsOfDate",
                table: "PriceHistories",
                columns: new[] { "Symbol", "AsOfDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Holdings_AccountId_Symbol",
                table: "Holdings",
                columns: new[] { "AccountId", "Symbol" },
                unique: true);
        }
    }
}
