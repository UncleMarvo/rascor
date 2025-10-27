using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rascor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MapTriggerMethodColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No changes needed - just mapping existing column names
            // The trigger_method column already exists in the database
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to revert
        }
    }
}
