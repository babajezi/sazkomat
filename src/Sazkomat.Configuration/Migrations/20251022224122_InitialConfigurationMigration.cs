using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class InitialConfigurationMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "configuration");

            migrationBuilder.CreateTable(
                name: "countries",
                schema: "configuration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    flag_emoji = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sports",
                schema: "configuration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sports", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leagues",
                schema: "configuration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sport_id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    display_name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    bet_explorer_slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_bettable = table.Column<bool>(type: "boolean", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leagues", x => x.id);
                    table.ForeignKey(
                        name: "FK_leagues_countries_country_id",
                        column: x => x.country_id,
                        principalSchema: "configuration",
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_leagues_sports_sport_id",
                        column: x => x.sport_id,
                        principalSchema: "configuration",
                        principalTable: "sports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_countries_code",
                schema: "configuration",
                table: "countries",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_countries_name",
                schema: "configuration",
                table: "countries",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leagues_country_id",
                schema: "configuration",
                table: "leagues",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_leagues_is_bettable",
                schema: "configuration",
                table: "leagues",
                column: "is_bettable");

            migrationBuilder.CreateIndex(
                name: "IX_leagues_is_enabled",
                schema: "configuration",
                table: "leagues",
                column: "is_enabled");

            migrationBuilder.CreateIndex(
                name: "IX_leagues_sport_id",
                schema: "configuration",
                table: "leagues",
                column: "sport_id");

            migrationBuilder.CreateIndex(
                name: "IX_leagues_sport_id_country_id_name",
                schema: "configuration",
                table: "leagues",
                columns: new[] { "sport_id", "country_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sports_code",
                schema: "configuration",
                table: "sports",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sports_name",
                schema: "configuration",
                table: "sports",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "leagues",
                schema: "configuration");

            migrationBuilder.DropTable(
                name: "countries",
                schema: "configuration");

            migrationBuilder.DropTable(
                name: "sports",
                schema: "configuration");
        }
    }
}
