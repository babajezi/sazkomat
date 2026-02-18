using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderIdToDataImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "provider_id",
                schema: "data_import",
                table: "rounds",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "provider_id",
                schema: "data_import",
                table: "matches",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "provider_url",
                schema: "data_import",
                table: "matches",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "provider_id",
                schema: "data_import",
                table: "import_jobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_rounds_provider_id",
                schema: "data_import",
                table: "rounds",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_matches_provider_id",
                schema: "data_import",
                table: "matches",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_import_jobs_provider_id",
                schema: "data_import",
                table: "import_jobs",
                column: "provider_id");

            // Data Migration: Set ProviderId to BetExplorer for existing records
            migrationBuilder.Sql(@"
                UPDATE data_import.rounds
                SET provider_id = 'a0000000-0000-0000-0000-000000000001'::uuid
                WHERE provider_id = '00000000-0000-0000-0000-000000000000'::uuid;
            ");

            migrationBuilder.Sql(@"
                UPDATE data_import.matches
                SET provider_id = 'a0000000-0000-0000-0000-000000000001'::uuid
                WHERE provider_id = '00000000-0000-0000-0000-000000000000'::uuid;
            ");

            migrationBuilder.Sql(@"
                UPDATE data_import.import_jobs
                SET provider_id = 'a0000000-0000-0000-0000-000000000001'::uuid
                WHERE provider_id = '00000000-0000-0000-0000-000000000000'::uuid;
            ");

            // Data Migration: Copy BetExplorerUrl to ProviderUrl for existing matches
            migrationBuilder.Sql(@"
                UPDATE data_import.matches
                SET provider_url = betexplorer_url
                WHERE betexplorer_url IS NOT NULL AND provider_url IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rounds_provider_id",
                schema: "data_import",
                table: "rounds");

            migrationBuilder.DropIndex(
                name: "IX_matches_provider_id",
                schema: "data_import",
                table: "matches");

            migrationBuilder.DropIndex(
                name: "IX_import_jobs_provider_id",
                schema: "data_import",
                table: "import_jobs");

            migrationBuilder.DropColumn(
                name: "provider_id",
                schema: "data_import",
                table: "rounds");

            migrationBuilder.DropColumn(
                name: "provider_id",
                schema: "data_import",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "provider_url",
                schema: "data_import",
                table: "matches");

            migrationBuilder.DropColumn(
                name: "provider_id",
                schema: "data_import",
                table: "import_jobs");
        }
    }
}
