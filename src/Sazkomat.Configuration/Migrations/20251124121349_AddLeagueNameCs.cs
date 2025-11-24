using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class AddLeagueNameCs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add name_cs column to leagues table
            migrationBuilder.AddColumn<string>(
                name: "name_cs",
                schema: "configuration",
                table: "leagues",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name_cs",
                schema: "configuration",
                table: "leagues");
        }
    }
}
