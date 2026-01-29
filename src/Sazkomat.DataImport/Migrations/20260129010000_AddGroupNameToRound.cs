using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.DataImport.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupNameToRound : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the group_name column for leagues with groups (e.g., "East", "West", "GROUP 1")
            migrationBuilder.AddColumn<string>(
                name: "group_name",
                schema: "data_import",
                table: "rounds",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Create index for group-based queries
            migrationBuilder.CreateIndex(
                name: "ix_rounds_league_season_group_round",
                schema: "data_import",
                table: "rounds",
                columns: new[] { "league_id", "season_id", "group_name", "round_number" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_rounds_league_season_group_round",
                schema: "data_import",
                table: "rounds");

            migrationBuilder.DropColumn(
                name: "group_name",
                schema: "data_import",
                table: "rounds");
        }
    }
}
