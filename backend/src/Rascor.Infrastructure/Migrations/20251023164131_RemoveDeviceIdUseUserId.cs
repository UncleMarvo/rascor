using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rascor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDeviceIdUseUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The user_id column already exists, so this migration does nothing
            // Just marking the model change in EF's history
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to revert
        }
    }
}
