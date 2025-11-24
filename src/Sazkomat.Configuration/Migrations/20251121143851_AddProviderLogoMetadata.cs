using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderLogoMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "has_logo",
                schema: "configuration",
                table: "data_providers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "logo_uploaded_at",
                schema: "configuration",
                table: "data_providers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "has_logo",
                schema: "configuration",
                table: "data_providers");

            migrationBuilder.DropColumn(
                name: "logo_uploaded_at",
                schema: "configuration",
                table: "data_providers");
        }
    }
}
