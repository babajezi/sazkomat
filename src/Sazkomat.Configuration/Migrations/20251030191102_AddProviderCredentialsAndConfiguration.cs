using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderCredentialsAndConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "configuration",
                schema: "configuration",
                table: "data_providers",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "credentials",
                schema: "configuration",
                table: "data_providers",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "configuration",
                schema: "configuration",
                table: "data_providers");

            migrationBuilder.DropColumn(
                name: "credentials",
                schema: "configuration",
                table: "data_providers");
        }
    }
}
