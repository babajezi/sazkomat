using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryNameMappingPatternSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns for pattern matching support
            migrationBuilder.AddColumn<string>(
                name: "match_type",
                schema: "data_import",
                table: "country_name_mappings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "substring");

            migrationBuilder.AddColumn<bool>(
                name: "is_case_sensitive",
                schema: "data_import",
                table: "country_name_mappings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_special_case",
                schema: "data_import",
                table: "country_name_mappings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "localized_name",
                schema: "data_import",
                table: "country_name_mappings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Add index for special cases - checked first during pattern matching
            migrationBuilder.CreateIndex(
                name: "ix_country_name_mappings_special_cases",
                schema: "data_import",
                table: "country_name_mappings",
                columns: new[] { "provider_code", "is_special_case", "is_active", "priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_country_name_mappings_special_cases",
                schema: "data_import",
                table: "country_name_mappings");

            migrationBuilder.DropColumn(
                name: "match_type",
                schema: "data_import",
                table: "country_name_mappings");

            migrationBuilder.DropColumn(
                name: "is_case_sensitive",
                schema: "data_import",
                table: "country_name_mappings");

            migrationBuilder.DropColumn(
                name: "is_special_case",
                schema: "data_import",
                table: "country_name_mappings");

            migrationBuilder.DropColumn(
                name: "localized_name",
                schema: "data_import",
                table: "country_name_mappings");
        }
    }
}
