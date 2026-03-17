using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Privestio.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImportMappingClassificationConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AmountSignFlipped",
                table: "ImportMappings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BuyKeywords",
                table: "ImportMappings",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "CashEquivalentSymbols",
                table: "ImportMappings",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "IgnoreRowPatterns",
                table: "ImportMappings",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "IncomeKeywords",
                table: "ImportMappings",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "SellKeywords",
                table: "ImportMappings",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountSignFlipped",
                table: "ImportMappings");

            migrationBuilder.DropColumn(
                name: "BuyKeywords",
                table: "ImportMappings");

            migrationBuilder.DropColumn(
                name: "CashEquivalentSymbols",
                table: "ImportMappings");

            migrationBuilder.DropColumn(
                name: "IgnoreRowPatterns",
                table: "ImportMappings");

            migrationBuilder.DropColumn(
                name: "IncomeKeywords",
                table: "ImportMappings");

            migrationBuilder.DropColumn(
                name: "SellKeywords",
                table: "ImportMappings");
        }
    }
}
