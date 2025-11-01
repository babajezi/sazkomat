using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class AddLogSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "log_settings",
                schema: "configuration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sub_category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    log_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    log_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "Information"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    retention_days = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    max_file_size_bytes = table.Column<long>(type: "bigint", nullable: false, defaultValue: 104857600L),
                    output_template = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_log_settings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_log_settings_category",
                schema: "configuration",
                table: "log_settings",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_log_settings_category_subcategory",
                schema: "configuration",
                table: "log_settings",
                columns: new[] { "category", "sub_category" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "log_settings",
                schema: "configuration");
        }
    }
}
