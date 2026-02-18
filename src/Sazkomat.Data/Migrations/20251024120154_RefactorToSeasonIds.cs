using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorToSeasonIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add season_id column (nullable for now to allow data migration)
            migrationBuilder.AddColumn<Guid>(
                name: "season_id",
                schema: "data_import",
                table: "rounds",
                type: "uuid",
                nullable: true);

            // Step 2: Data migration - Create Season entities and populate season_id
            migrationBuilder.Sql(@"
                -- Insert unique seasons from existing rounds into configuration.seasons
                INSERT INTO configuration.seasons (id, name, start_year, end_year, created_at, updated_at)
                SELECT
                    gen_random_uuid() as id,
                    season as name,
                    CASE
                        WHEN season ~ '^\d{4}[-/]\d{4}$' THEN
                            CAST(split_part(replace(season, '/', '-'), '-', 1) AS INTEGER)
                        WHEN season ~ '^\d{4}$' THEN
                            CAST(season AS INTEGER)
                        ELSE 0
                    END as start_year,
                    CASE
                        WHEN season ~ '^\d{4}[-/]\d{4}$' THEN
                            CAST(split_part(replace(season, '/', '-'), '-', 2) AS INTEGER)
                        ELSE NULL
                    END as end_year,
                    NOW() as created_at,
                    NOW() as updated_at
                FROM (
                    SELECT DISTINCT season
                    FROM data_import.rounds
                    WHERE season IS NOT NULL AND season != ''
                ) unique_seasons
                ON CONFLICT (name) DO NOTHING;

                -- Create LeagueSeason entries for each league-season combination
                INSERT INTO configuration.league_seasons
                    (id, league_id, season_id, is_available_on_betexplorer, has_data, has_odds,
                     rounds_count, matches_count, created_at, updated_at)
                SELECT
                    gen_random_uuid() as id,
                    r.league_id,
                    s.id as season_id,
                    true as is_available_on_betexplorer,
                    true as has_data,
                    BOOL_OR(r.odds_complete = 'Yes') as has_odds,
                    COUNT(DISTINCT r.id) as rounds_count,
                    SUM(r.matches_count) as matches_count,
                    NOW() as created_at,
                    NOW() as updated_at
                FROM data_import.rounds r
                INNER JOIN configuration.seasons s ON s.name = r.season
                WHERE r.season IS NOT NULL AND r.season != ''
                GROUP BY r.league_id, s.id
                ON CONFLICT (league_id, season_id) DO UPDATE SET
                    has_data = true,
                    rounds_count = EXCLUDED.rounds_count,
                    matches_count = EXCLUDED.matches_count,
                    updated_at = NOW();

                -- Update rounds.season_id from the season name
                UPDATE data_import.rounds r
                SET season_id = s.id
                FROM configuration.seasons s
                WHERE s.name = r.season;
            ");

            // Step 3: Make season_id NOT NULL after data migration
            migrationBuilder.AlterColumn<Guid>(
                name: "season_id",
                schema: "data_import",
                table: "rounds",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // Step 4: Drop old indexes related to season column
            migrationBuilder.DropIndex(
                name: "IX_rounds_league_id_season",
                schema: "data_import",
                table: "rounds");

            migrationBuilder.DropIndex(
                name: "IX_rounds_league_id_season_round_number",
                schema: "data_import",
                table: "rounds");

            migrationBuilder.DropIndex(
                name: "IX_rounds_season",
                schema: "data_import",
                table: "rounds");

            // Step 5: Drop the old season column
            migrationBuilder.DropColumn(
                name: "season",
                schema: "data_import",
                table: "rounds");

            // Step 6: Rename seasons to season_ids in import_jobs
            migrationBuilder.RenameColumn(
                name: "seasons",
                schema: "data_import",
                table: "import_jobs",
                newName: "season_ids");

            // Step 7: Migrate import_jobs.seasons to season_ids
            migrationBuilder.Sql(@"
                -- Convert season names in import_jobs to season IDs
                UPDATE data_import.import_jobs ij
                SET season_ids = COALESCE((
                    SELECT jsonb_agg(s.id)
                    FROM jsonb_array_elements_text(ij.season_ids) season_name
                    INNER JOIN configuration.seasons s ON s.name = season_name::text
                ), '[]'::jsonb)
                WHERE season_ids IS NOT NULL;
            ");

            // Step 8: Create new indexes for season_id
            migrationBuilder.CreateIndex(
                name: "IX_rounds_league_id_season_id",
                schema: "data_import",
                table: "rounds",
                columns: new[] { "league_id", "season_id" });

            migrationBuilder.CreateIndex(
                name: "IX_rounds_league_id_season_id_round_number",
                schema: "data_import",
                table: "rounds",
                columns: new[] { "league_id", "season_id", "round_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rounds_season_id",
                schema: "data_import",
                table: "rounds",
                column: "season_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rounds_league_id_season_id",
                schema: "data_import",
                table: "rounds");

            migrationBuilder.DropIndex(
                name: "IX_rounds_league_id_season_id_round_number",
                schema: "data_import",
                table: "rounds");

            migrationBuilder.DropIndex(
                name: "IX_rounds_season_id",
                schema: "data_import",
                table: "rounds");

            migrationBuilder.DropColumn(
                name: "season_id",
                schema: "data_import",
                table: "rounds");

            migrationBuilder.RenameColumn(
                name: "season_ids",
                schema: "data_import",
                table: "import_jobs",
                newName: "seasons");

            migrationBuilder.AddColumn<string>(
                name: "season",
                schema: "data_import",
                table: "rounds",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_rounds_league_id_season",
                schema: "data_import",
                table: "rounds",
                columns: new[] { "league_id", "season" });

            migrationBuilder.CreateIndex(
                name: "IX_rounds_league_id_season_round_number",
                schema: "data_import",
                table: "rounds",
                columns: new[] { "league_id", "season", "round_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rounds_season",
                schema: "data_import",
                table: "rounds",
                column: "season");
        }
    }
}
