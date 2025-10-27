using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rascor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncGeofenceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL to handle columns that might already exist
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    -- Drop old column if exists
                    IF EXISTS (SELECT 1 FROM information_schema.columns 
                              WHERE table_name='sites' AND column_name='radius_meters') 
                    THEN
                        ALTER TABLE sites DROP COLUMN radius_meters;
                    END IF;

                    -- Add auto_trigger_radius_meters if not exists
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                  WHERE table_name='sites' AND column_name='auto_trigger_radius_meters') 
                    THEN
                        ALTER TABLE sites ADD COLUMN auto_trigger_radius_meters integer NOT NULL DEFAULT 50;
                    END IF;

                    -- Add manual_trigger_radius_meters if not exists
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                  WHERE table_name='sites' AND column_name='manual_trigger_radius_meters') 
                    THEN
                        ALTER TABLE sites ADD COLUMN manual_trigger_radius_meters integer NOT NULL DEFAULT 150;
                    END IF;

                    -- Add trigger_method if not exists
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                  WHERE table_name='geofence_events' AND column_name='trigger_method') 
                    THEN
                        ALTER TABLE geofence_events ADD COLUMN trigger_method varchar(10) DEFAULT 'auto';
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "auto_trigger_radius_meters",
                table: "sites");

            migrationBuilder.DropColumn(
                name: "manual_trigger_radius_meters",
                table: "sites");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "geofence_events");

            migrationBuilder.DropColumn(
                name: "TriggerMethod",
                table: "geofence_events");

            migrationBuilder.AddColumn<int>(
                name: "radius_meters",
                table: "sites",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<double>(
                name: "longitude",
                table: "geofence_events",
                type: "double precision",
                precision: 11,
                scale: 8,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldPrecision: 11,
                oldScale: 8);

            migrationBuilder.AlterColumn<double>(
                name: "latitude",
                table: "geofence_events",
                type: "double precision",
                precision: 10,
                scale: 8,
                nullable: true,
                oldClrType: typeof(double),
                oldType: "double precision",
                oldPrecision: 10,
                oldScale: 8);
        }
    }
}
