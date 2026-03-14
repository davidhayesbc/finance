using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Privestio.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestmentTradeMetadataToTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActivitySubType",
                table: "Transactions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivityType",
                table: "Transactions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "Transactions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "Transactions",
                type: "numeric(18,8)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecurityName",
                table: "Transactions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "SettlementDate",
                table: "Transactions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Symbol",
                table: "Transactions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "Transactions",
                type: "numeric(18,8)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActivitySubType",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ActivityType",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SecurityName",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SettlementDate",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "Symbol",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "Transactions");
        }
    }
}
