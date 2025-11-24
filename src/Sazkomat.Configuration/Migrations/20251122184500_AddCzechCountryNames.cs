using Microsoft.EntityFrameworkCore.Migrations;
using System.Text.Json;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class AddCzechCountryNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add name_cs column
            migrationBuilder.AddColumn<string>(
                name: "name_cs",
                schema: "configuration",
                table: "countries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // Migration seed - download and populate Czech names
            PopulateCzechNames(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name_cs",
                schema: "configuration",
                table: "countries");
        }

        private void PopulateCzechNames(MigrationBuilder migrationBuilder)
        {
            // Download Czech country names from GitHub
            var czechNames = DownloadCzechNamesAsync().GetAwaiter().GetResult();

            // Update each country with Czech name
            foreach (var (isoCode, czechName) in czechNames)
            {
                // Escape single quotes in Czech names
                var escapedName = czechName.Replace("'", "''");

                migrationBuilder.Sql($@"
                    UPDATE configuration.countries
                    SET name_cs = '{escapedName}'
                    WHERE LOWER(""IsoCode"") = '{isoCode.ToLower()}'
                ");
            }
        }

        private async Task<Dictionary<string, string>> DownloadCzechNamesAsync()
        {
            using var client = new HttpClient();
            var json = await client.GetStringAsync(
                "https://raw.githubusercontent.com/stefangabos/world_countries/master/data/countries/cs/countries.json");

            var czechNames = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return czechNames ?? new Dictionary<string, string>();
        }
    }
}
