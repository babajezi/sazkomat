using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsSyncEnabledColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_leagues_is_enabled",
                schema: "configuration",
                table: "leagues");

            migrationBuilder.DropColumn(
                name: "is_enabled",
                schema: "configuration",
                table: "leagues");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_enabled",
                schema: "configuration",
                table: "leagues",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_leagues_is_enabled",
                schema: "configuration",
                table: "leagues",
                column: "is_enabled");
        }
    }
}
