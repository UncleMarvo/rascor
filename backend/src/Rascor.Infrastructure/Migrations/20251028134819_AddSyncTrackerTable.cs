using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rascor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncTrackerTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncTrackers",
                columns: table => new
                {
                    EntityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastSyncTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSuccessfulSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncTrackers", x => x.EntityName);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncTrackers");
        }
    }
}
