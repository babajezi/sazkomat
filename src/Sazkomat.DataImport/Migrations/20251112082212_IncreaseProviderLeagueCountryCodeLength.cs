using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.DataImport.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseProviderLeagueCountryCodeLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "country_code",
                schema: "data_import",
                table: "provider_leagues",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "country_code",
                schema: "data_import",
                table: "provider_leagues",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);
        }
    }
}
