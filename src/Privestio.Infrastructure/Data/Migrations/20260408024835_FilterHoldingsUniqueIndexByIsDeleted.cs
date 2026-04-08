using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Privestio.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FilterHoldingsUniqueIndexByIsDeleted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Holdings_AccountId_SecurityId",
                table: "Holdings");

            migrationBuilder.CreateIndex(
                name: "IX_Holdings_AccountId_SecurityId",
                table: "Holdings",
                columns: new[] { "AccountId", "SecurityId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Holdings_AccountId_SecurityId",
                table: "Holdings");

            migrationBuilder.CreateIndex(
                name: "IX_Holdings_AccountId_SecurityId",
                table: "Holdings",
                columns: new[] { "AccountId", "SecurityId" },
                unique: true);
        }
    }
}
