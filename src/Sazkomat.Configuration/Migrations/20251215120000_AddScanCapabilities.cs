using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class AddScanCapabilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "scan_capabilities",
                schema: "configuration",
                table: "data_providers",
                type: "jsonb",
                nullable: false,
                defaultValue: "{\"canScanCountries\":true,\"canScanLeagues\":true,\"canScanSeasons\":true}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "scan_capabilities",
                schema: "configuration",
                table: "data_providers");
        }
    }
}
