using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedLeagueNameColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the normalized_provider_league_name column
            migrationBuilder.AddColumn<string>(
                name: "normalized_provider_league_name",
                schema: "data_import",
                table: "league_name_mappings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // Populate existing records with normalized values
            // (lowercase, trimmed, collapsed whitespace)
            migrationBuilder.Sql(@"
                UPDATE data_import.league_name_mappings
                SET normalized_provider_league_name = LOWER(TRIM(REGEXP_REPLACE(provider_league_name, '\s+', ' ', 'g')));
            ");

            // Create index for normalized lookup (used for global rule fallback)
            migrationBuilder.CreateIndex(
                name: "ix_league_name_mappings_normalized_lookup",
                schema: "data_import",
                table: "league_name_mappings",
                columns: new[] { "country_code", "normalized_provider_league_name", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_league_name_mappings_normalized_lookup",
                schema: "data_import",
                table: "league_name_mappings");

            migrationBuilder.DropColumn(
                name: "normalized_provider_league_name",
                schema: "data_import",
                table: "league_name_mappings");
        }
    }
}
