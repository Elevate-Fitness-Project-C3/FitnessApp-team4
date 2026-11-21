using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityAuditAppendOnlyTrigger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TRIGGER [TR_SecurityAuditLogs_AppendOnly] ON [SecurityAuditLogs]
                INSTEAD OF UPDATE, DELETE AS
                BEGIN
                    THROW 51010, 'SecurityAuditLogs is append-only.', 1;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER [TR_SecurityAuditLogs_AppendOnly];");
        }
    }
}
