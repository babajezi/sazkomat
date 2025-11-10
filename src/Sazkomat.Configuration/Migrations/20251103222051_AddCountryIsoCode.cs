using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryIsoCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IsoCode",
                schema: "configuration",
                table: "countries",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsoCode",
                schema: "configuration",
                table: "countries");
        }
    }
}
