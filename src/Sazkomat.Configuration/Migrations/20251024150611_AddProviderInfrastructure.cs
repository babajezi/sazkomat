using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderInfrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "data_providers",
                schema: "configuration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    base_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    type = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_data_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "country_providers",
                schema: "configuration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    provider_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_country_providers", x => x.id);
                    table.ForeignKey(
                        name: "FK_country_providers_countries_country_id",
                        column: x => x.country_id,
                        principalSchema: "configuration",
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_country_providers_data_providers_provider_id",
                        column: x => x.provider_id,
                        principalSchema: "configuration",
                        principalTable: "data_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "league_providers",
                schema: "configuration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    league_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    provider_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    provider_league_id = table.Column<int>(type: "integer", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_league_providers", x => x.id);
                    table.ForeignKey(
                        name: "FK_league_providers_data_providers_provider_id",
                        column: x => x.provider_id,
                        principalSchema: "configuration",
                        principalTable: "data_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_league_providers_leagues_league_id",
                        column: x => x.league_id,
                        principalSchema: "configuration",
                        principalTable: "leagues",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_country_providers_country_provider",
                schema: "configuration",
                table: "country_providers",
                columns: new[] { "country_id", "provider_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_country_providers_is_active",
                schema: "configuration",
                table: "country_providers",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_country_providers_provider_code",
                schema: "configuration",
                table: "country_providers",
                column: "provider_code");

            migrationBuilder.CreateIndex(
                name: "IX_country_providers_provider_id",
                schema: "configuration",
                table: "country_providers",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_data_providers_code",
                schema: "configuration",
                table: "data_providers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_data_providers_is_active",
                schema: "configuration",
                table: "data_providers",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_league_providers_is_active",
                schema: "configuration",
                table: "league_providers",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_league_providers_league_provider",
                schema: "configuration",
                table: "league_providers",
                columns: new[] { "league_id", "provider_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_league_providers_provider_id",
                schema: "configuration",
                table: "league_providers",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_league_providers_provider_slug",
                schema: "configuration",
                table: "league_providers",
                column: "provider_slug");

            // Data Migration: Create BetExplorer provider
            migrationBuilder.Sql(@"
                INSERT INTO configuration.data_providers (id, name, code, base_url, is_active, priority, type, created_at, updated_at)
                VALUES (
                    'a0000000-0000-0000-0000-000000000001'::uuid,
                    'BetExplorer',
                    'betexplorer',
                    'https://www.betexplorer.com',
                    true,
                    10,
                    1, -- Scraper
                    NOW(),
                    NOW()
                );
            ");

            // Data Migration: Create CountryProvider mappings for existing countries
            migrationBuilder.Sql(@"
                INSERT INTO configuration.country_providers (id, country_id, provider_id, provider_code, provider_name, is_active, created_at, updated_at)
                SELECT
                    gen_random_uuid(),
                    c.id,
                    'a0000000-0000-0000-0000-000000000001'::uuid,
                    c.code, -- Using country code as provider code
                    c.name,
                    true,
                    NOW(),
                    NOW()
                FROM configuration.countries c;
            ");

            // Data Migration: Create LeagueProvider mappings from existing leagues
            migrationBuilder.Sql(@"
                INSERT INTO configuration.league_providers (id, league_id, provider_id, provider_slug, provider_name, is_active, created_at, updated_at)
                SELECT
                    gen_random_uuid(),
                    l.id,
                    'a0000000-0000-0000-0000-000000000001'::uuid,
                    l.bet_explorer_slug,
                    l.name,
                    l.is_enabled,
                    NOW(),
                    NOW()
                FROM configuration.leagues l
                WHERE l.bet_explorer_slug IS NOT NULL AND l.bet_explorer_slug <> '';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "country_providers",
                schema: "configuration");

            migrationBuilder.DropTable(
                name: "league_providers",
                schema: "configuration");

            migrationBuilder.DropTable(
                name: "data_providers",
                schema: "configuration");
        }
    }
}
