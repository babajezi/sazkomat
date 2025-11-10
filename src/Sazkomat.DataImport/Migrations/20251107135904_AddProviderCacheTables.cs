using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.DataImport.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderCacheTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "provider_countries",
                schema: "data_import",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    provider_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    iso_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    flag_emoji = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    scraped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    raw_data = table.Column<string>(type: "jsonb", nullable: true),
                    is_imported = table.Column<bool>(type: "boolean", nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: true),
                    imported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "provider_leagues",
                schema: "data_import",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_slug = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    provider_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    display_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    is_bettable = table.Column<bool>(type: "boolean", nullable: false),
                    scraped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    raw_data = table.Column<string>(type: "jsonb", nullable: true),
                    is_imported = table.Column<bool>(type: "boolean", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: true),
                    imported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_leagues", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "provider_seasons",
                schema: "data_import",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    start_year = table.Column<int>(type: "integer", nullable: false),
                    end_year = table.Column<int>(type: "integer", nullable: true),
                    is_current_season = table.Column<bool>(type: "boolean", nullable: false),
                    scraped_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    raw_data = table.Column<string>(type: "jsonb", nullable: true),
                    is_imported = table.Column<bool>(type: "boolean", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: true),
                    imported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_provider_seasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sync_jobs",
                schema: "data_import",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    entity_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    scheduled_for = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    max_retries = table.Column<int>(type: "integer", nullable: false),
                    progress_data = table.Column<string>(type: "jsonb", nullable: true),
                    country_ids = table.Column<string>(type: "jsonb", nullable: false),
                    league_ids = table.Column<string>(type: "jsonb", nullable: false),
                    season_ids = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_jobs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_provider_countries_country_id",
                schema: "data_import",
                table: "provider_countries",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_countries_is_imported",
                schema: "data_import",
                table: "provider_countries",
                column: "is_imported");

            migrationBuilder.CreateIndex(
                name: "IX_provider_countries_provider_code",
                schema: "data_import",
                table: "provider_countries",
                column: "provider_code");

            migrationBuilder.CreateIndex(
                name: "IX_provider_countries_provider_id",
                schema: "data_import",
                table: "provider_countries",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_countries_scraped_at",
                schema: "data_import",
                table: "provider_countries",
                column: "scraped_at");

            migrationBuilder.CreateIndex(
                name: "IX_provider_leagues_is_imported",
                schema: "data_import",
                table: "provider_leagues",
                column: "is_imported");

            migrationBuilder.CreateIndex(
                name: "IX_provider_leagues_league_id",
                schema: "data_import",
                table: "provider_leagues",
                column: "league_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_leagues_provider_country_id",
                schema: "data_import",
                table: "provider_leagues",
                column: "provider_country_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_leagues_provider_id",
                schema: "data_import",
                table: "provider_leagues",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_leagues_provider_slug",
                schema: "data_import",
                table: "provider_leagues",
                column: "provider_slug");

            migrationBuilder.CreateIndex(
                name: "IX_provider_leagues_scraped_at",
                schema: "data_import",
                table: "provider_leagues",
                column: "scraped_at");

            migrationBuilder.CreateIndex(
                name: "IX_provider_seasons_is_current_season",
                schema: "data_import",
                table: "provider_seasons",
                column: "is_current_season");

            migrationBuilder.CreateIndex(
                name: "IX_provider_seasons_is_imported",
                schema: "data_import",
                table: "provider_seasons",
                column: "is_imported");

            migrationBuilder.CreateIndex(
                name: "IX_provider_seasons_provider_id",
                schema: "data_import",
                table: "provider_seasons",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_seasons_provider_league_id",
                schema: "data_import",
                table: "provider_seasons",
                column: "provider_league_id");

            migrationBuilder.CreateIndex(
                name: "IX_provider_seasons_scraped_at",
                schema: "data_import",
                table: "provider_seasons",
                column: "scraped_at");

            migrationBuilder.CreateIndex(
                name: "IX_provider_seasons_season_id",
                schema: "data_import",
                table: "provider_seasons",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "IX_sync_jobs_entity_type",
                schema: "data_import",
                table: "sync_jobs",
                column: "entity_type");

            migrationBuilder.CreateIndex(
                name: "IX_sync_jobs_priority",
                schema: "data_import",
                table: "sync_jobs",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "IX_sync_jobs_provider_id",
                schema: "data_import",
                table: "sync_jobs",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "IX_sync_jobs_scheduled_for",
                schema: "data_import",
                table: "sync_jobs",
                column: "scheduled_for");

            migrationBuilder.CreateIndex(
                name: "IX_sync_jobs_status",
                schema: "data_import",
                table: "sync_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_sync_jobs_status_priority",
                schema: "data_import",
                table: "sync_jobs",
                columns: new[] { "status", "priority" });

            migrationBuilder.CreateIndex(
                name: "IX_sync_jobs_type",
                schema: "data_import",
                table: "sync_jobs",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "provider_countries",
                schema: "data_import");

            migrationBuilder.DropTable(
                name: "provider_leagues",
                schema: "data_import");

            migrationBuilder.DropTable(
                name: "provider_seasons",
                schema: "data_import");

            migrationBuilder.DropTable(
                name: "sync_jobs",
                schema: "data_import");
        }
    }
}
