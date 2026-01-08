using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.Configuration.Migrations
{
    /// <inheritdoc />
    public partial class AddUserApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "approved_at",
                schema: "configuration",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "approved_by",
                schema: "configuration",
                table: "AspNetUsers",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_approved",
                schema: "configuration",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_is_approved",
                schema: "configuration",
                table: "AspNetUsers",
                column: "is_approved");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_is_approved",
                schema: "configuration",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "approved_at",
                schema: "configuration",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "approved_by",
                schema: "configuration",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "is_approved",
                schema: "configuration",
                table: "AspNetUsers");
        }
    }
}
