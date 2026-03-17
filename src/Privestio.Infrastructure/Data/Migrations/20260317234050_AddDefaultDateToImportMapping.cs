using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Privestio.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultDateToImportMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "DefaultDate",
                table: "ImportMappings",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultDate",
                table: "ImportMappings");
        }
    }
}
