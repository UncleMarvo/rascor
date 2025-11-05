using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rascor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRamsAndAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop RAMS-related tables (order matters due to foreign keys)
            migrationBuilder.DropTable(name: "rams_acceptances");
            migrationBuilder.DropTable(name: "rams_photos");
            migrationBuilder.DropTable(name: "rams_checklist_items");
            migrationBuilder.DropTable(name: "work_assignments");
            migrationBuilder.DropTable(name: "rams_documents");
            migrationBuilder.DropTable(name: "work_types");

            // Drop assignments table
            migrationBuilder.DropTable(name: "assignments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "assignments",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    site_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignments", x => new { x.user_id, x.site_id });
                });
        }
    }
}
