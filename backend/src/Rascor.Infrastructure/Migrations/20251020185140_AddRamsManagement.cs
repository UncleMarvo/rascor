using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rascor.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRamsManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "work_types",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rams_documents",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    work_type_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    pdf_blob_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    effective_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rams_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_rams_documents_work_types_work_type_id",
                        column: x => x.work_type_id,
                        principalTable: "work_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_assignments",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    site_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    work_type_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    assigned_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expected_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expected_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_assignments_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_assignments_work_types_work_type_id",
                        column: x => x.work_type_id,
                        principalTable: "work_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rams_checklist_items",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    rams_document_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    section = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    item_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    label = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    validation_rules = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rams_checklist_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_rams_checklist_items_rams_documents_rams_document_id",
                        column: x => x.rams_document_id,
                        principalTable: "rams_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rams_acceptances",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    site_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    work_assignment_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    rams_document_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    signature_data = table.Column<string>(type: "text", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    device_info = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    latitude = table.Column<double>(type: "double precision", precision: 10, scale: 8, nullable: true),
                    longitude = table.Column<double>(type: "double precision", precision: 11, scale: 8, nullable: true),
                    accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    checklist_responses = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rams_acceptances", x => x.id);
                    table.ForeignKey(
                        name: "FK_rams_acceptances_rams_documents_rams_document_id",
                        column: x => x.rams_document_id,
                        principalTable: "rams_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rams_acceptances_sites_site_id",
                        column: x => x.site_id,
                        principalTable: "sites",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rams_acceptances_work_assignments_work_assignment_id",
                        column: x => x.work_assignment_id,
                        principalTable: "work_assignments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rams_acceptances_rams_document_id",
                table: "rams_acceptances",
                column: "rams_document_id");

            migrationBuilder.CreateIndex(
                name: "IX_rams_acceptances_site_id",
                table: "rams_acceptances",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "IX_rams_acceptances_user_id_site_id_accepted_at",
                table: "rams_acceptances",
                columns: new[] { "user_id", "site_id", "accepted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_rams_acceptances_user_id_work_assignment_id_rams_document_id",
                table: "rams_acceptances",
                columns: new[] { "user_id", "work_assignment_id", "rams_document_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rams_acceptances_work_assignment_id",
                table: "rams_acceptances",
                column: "work_assignment_id");

            migrationBuilder.CreateIndex(
                name: "IX_rams_checklist_items_rams_document_id_display_order",
                table: "rams_checklist_items",
                columns: new[] { "rams_document_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "IX_rams_documents_is_active",
                table: "rams_documents",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "IX_rams_documents_work_type_id_version",
                table: "rams_documents",
                columns: new[] { "work_type_id", "version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_assignments_site_id",
                table: "work_assignments",
                column: "site_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_assignments_status",
                table: "work_assignments",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_work_assignments_user_id_site_id",
                table: "work_assignments",
                columns: new[] { "user_id", "site_id" });

            migrationBuilder.CreateIndex(
                name: "IX_work_assignments_work_type_id",
                table: "work_assignments",
                column: "work_type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rams_acceptances");

            migrationBuilder.DropTable(
                name: "rams_checklist_items");

            migrationBuilder.DropTable(
                name: "work_assignments");

            migrationBuilder.DropTable(
                name: "rams_documents");

            migrationBuilder.DropTable(
                name: "work_types");
        }
    }
}
