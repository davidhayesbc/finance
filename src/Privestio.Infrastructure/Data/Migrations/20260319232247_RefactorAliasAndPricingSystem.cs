using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Privestio.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorAliasAndPricingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove null-Source display aliases before making Source NOT NULL
            migrationBuilder.Sql("""DELETE FROM "SecurityAliases" WHERE "Source" IS NULL""");

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "SecurityAliases",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "PricingProviderOrder",
                table: "Securities",
                type: "jsonb",
                nullable: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PricingProviderOrder", table: "Securities");

            migrationBuilder.AlterColumn<string>(
                name: "Source",
                table: "SecurityAliases",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100
            );
        }
    }
}
