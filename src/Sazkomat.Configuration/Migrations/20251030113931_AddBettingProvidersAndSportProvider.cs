using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class AddBettingProvidersAndSportProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sport_providers",
                schema: "configuration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sport_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sport_providers", x => x.id);
                    table.ForeignKey(
                        name: "FK_sport_providers_data_providers_provider_id",
                        column: x => x.provider_id,
                        principalSchema: "configuration",
                        principalTable: "data_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sport_providers_sports_sport_id",
                        column: x => x.sport_id,
                        principalSchema: "configuration",
                        principalTable: "sports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sport_providers_is_active",
                schema: "configuration",
                table: "sport_providers",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_sport_providers_provider_code",
                schema: "configuration",
                table: "sport_providers",
                column: "provider_code");

            migrationBuilder.CreateIndex(
                name: "IX_sport_providers_provider_id",
                schema: "configuration",
                table: "sport_providers",
                column: "provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_sport_providers_sport_provider",
                schema: "configuration",
                table: "sport_providers",
                columns: new[] { "sport_id", "provider_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sport_providers",
                schema: "configuration");
        }
    }
}
