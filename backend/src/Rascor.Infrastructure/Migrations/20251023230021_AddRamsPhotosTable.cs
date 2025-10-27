using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rascor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRamsPhotosTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Table already exists from manual SQL, so check if exists first
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS rams_photos (
                    id VARCHAR(50) PRIMARY KEY,
                    user_id VARCHAR(255) NOT NULL,
                    site_id VARCHAR(50) NOT NULL,
                    captured_at TIMESTAMP NOT NULL,
                    uploaded_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    file_path VARCHAR(500) NOT NULL,
                    file_size_bytes BIGINT NOT NULL,
                    original_filename VARCHAR(255)
                );
        
                CREATE INDEX IF NOT EXISTS idx_rams_photos_user ON rams_photos(user_id);
                CREATE INDEX IF NOT EXISTS idx_rams_photos_site ON rams_photos(site_id);
                CREATE INDEX IF NOT EXISTS idx_rams_photos_captured ON rams_photos(captured_at);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("rams_photos");
        }
    }
}
