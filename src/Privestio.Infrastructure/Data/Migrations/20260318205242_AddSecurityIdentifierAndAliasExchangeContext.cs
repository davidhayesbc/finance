using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Privestio.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityIdentifierAndAliasExchangeContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SecurityAliases_SecurityId_Symbol_Source",
                table: "SecurityAliases");

            migrationBuilder.AddColumn<string>(
                name: "Exchange",
                table: "SecurityAliases",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SecurityIdentifiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SecurityId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdentifierType = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityIdentifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityIdentifiers_Securities_SecurityId",
                        column: x => x.SecurityId,
                        principalTable: "Securities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAliases_SecurityId_Symbol_Source_Exchange",
                table: "SecurityAliases",
                columns: new[] { "SecurityId", "Symbol", "Source", "Exchange" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAliases_Symbol_Source_Exchange",
                table: "SecurityAliases",
                columns: new[] { "Symbol", "Source", "Exchange" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIdentifiers_IdentifierType_Value",
                table: "SecurityIdentifiers",
                columns: new[] { "IdentifierType", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityIdentifiers_SecurityId_IdentifierType_Value",
                table: "SecurityIdentifiers",
                columns: new[] { "SecurityId", "IdentifierType", "Value" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecurityIdentifiers");

            migrationBuilder.DropIndex(
                name: "IX_SecurityAliases_SecurityId_Symbol_Source_Exchange",
                table: "SecurityAliases");

            migrationBuilder.DropIndex(
                name: "IX_SecurityAliases_Symbol_Source_Exchange",
                table: "SecurityAliases");

            migrationBuilder.DropColumn(
                name: "Exchange",
                table: "SecurityAliases");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAliases_SecurityId_Symbol_Source",
                table: "SecurityAliases",
                columns: new[] { "SecurityId", "Symbol", "Source" },
                unique: true);
        }
    }
}
