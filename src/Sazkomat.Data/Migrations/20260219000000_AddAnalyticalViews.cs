using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticalViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analytical_views",
                schema: "data_import",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    spec_json = table.Column<string>(type: "jsonb", nullable: false),
                    tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_favorite = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    execution_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_executed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_execution_ms = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analytical_views", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_analytical_views_name",
                schema: "data_import",
                table: "analytical_views",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_analytical_views_is_favorite",
                schema: "data_import",
                table: "analytical_views",
                column: "is_favorite");

            migrationBuilder.CreateIndex(
                name: "ix_analytical_views_last_executed_at",
                schema: "data_import",
                table: "analytical_views",
                column: "last_executed_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analytical_views",
                schema: "data_import");
        }
    }
}
