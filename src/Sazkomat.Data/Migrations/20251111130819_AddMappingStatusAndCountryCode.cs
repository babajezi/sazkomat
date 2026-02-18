using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMappingStatusAndCountryCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                schema: "data_import",
                table: "provider_leagues",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MappingStatus",
                schema: "data_import",
                table: "provider_leagues",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryCode",
                schema: "data_import",
                table: "provider_leagues");

            migrationBuilder.DropColumn(
                name: "MappingStatus",
                schema: "data_import",
                table: "provider_leagues");
        }
    }
}
