using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sazkomat.DataImport.Migrations
{
    /// <inheritdoc />
    public partial class FixResolutionTypeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Change resolution_type from integer to character varying(20)
            // First, we need to convert existing integer values to string if any
            migrationBuilder.Sql(@"
                ALTER TABLE data_import.unmatched_leagues
                ALTER COLUMN resolution_type TYPE character varying(20)
                USING CASE
                    WHEN resolution_type = 0 THEN 'Mapped'
                    WHEN resolution_type = 1 THEN 'Ignored'
                    ELSE NULL
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert back to integer
            migrationBuilder.Sql(@"
                ALTER TABLE data_import.unmatched_leagues
                ALTER COLUMN resolution_type TYPE integer
                USING CASE
                    WHEN resolution_type = 'Mapped' THEN 0
                    WHEN resolution_type = 'Ignored' THEN 1
                    ELSE NULL
                END;
            ");
        }
    }
}
