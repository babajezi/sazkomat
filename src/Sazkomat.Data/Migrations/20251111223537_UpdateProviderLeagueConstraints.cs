using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProviderLeagueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MappingStatus",
                schema: "data_import",
                table: "provider_leagues",
                newName: "mapping_status");

            migrationBuilder.RenameColumn(
                name: "CountryCode",
                schema: "data_import",
                table: "provider_leagues",
                newName: "country_code");

            migrationBuilder.AlterColumn<Guid>(
                name: "provider_country_id",
                schema: "data_import",
                table: "provider_leagues",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "country_code",
                schema: "data_import",
                table: "provider_leagues",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_provider_leagues_country_code",
                schema: "data_import",
                table: "provider_leagues",
                column: "country_code");

            migrationBuilder.CreateIndex(
                name: "IX_provider_leagues_mapping_status",
                schema: "data_import",
                table: "provider_leagues",
                column: "mapping_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_provider_leagues_country_code",
                schema: "data_import",
                table: "provider_leagues");

            migrationBuilder.DropIndex(
                name: "IX_provider_leagues_mapping_status",
                schema: "data_import",
                table: "provider_leagues");

            migrationBuilder.RenameColumn(
                name: "mapping_status",
                schema: "data_import",
                table: "provider_leagues",
                newName: "MappingStatus");

            migrationBuilder.RenameColumn(
                name: "country_code",
                schema: "data_import",
                table: "provider_leagues",
                newName: "CountryCode");

            migrationBuilder.AlterColumn<Guid>(
                name: "provider_country_id",
                schema: "data_import",
                table: "provider_leagues",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CountryCode",
                schema: "data_import",
                table: "provider_leagues",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);
        }
    }
}
