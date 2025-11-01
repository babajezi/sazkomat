using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncWorkflowState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sync_workflow_state",
                schema: "configuration",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    countries_synced = table.Column<bool>(type: "boolean", nullable: false),
                    countries_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    leagues_synced = table.Column<bool>(type: "boolean", nullable: false),
                    leagues_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    seasons_synced = table.Column<bool>(type: "boolean", nullable: false),
                    countries_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    leagues_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    seasons_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_workflow_state", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sync_workflow_state",
                schema: "configuration");
        }
    }
}
