using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEventIngestedIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Scope_Ingested",
                schema: "dbo",
                table: "AuditEvents",
                columns: new[] { "ApplicationId", "TenantId", "IngestedAtUtc", "Id" },
                descending: new[] { false, false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditEvents_Scope_Ingested",
                schema: "dbo",
                table: "AuditEvents");
        }
    }
}
