using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuditService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuditStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "AuditEventReceipts",
                schema: "dbo",
                columns: table => new
                {
                    SourceService = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayloadHash = table.Column<byte[]>(type: "binary(32)", fixedLength: true, maxLength: 32, nullable: false),
                    FirstIngestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEventReceipts", x => new { x.SourceService, x.EventId });
                    table.CheckConstraint("CK_AuditEventReceipts_Identity", "LEN([SourceService]) > 0 AND [EventId] <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("CK_AuditEventReceipts_Utc", "DATEPART(TZOFFSET, [FirstIngestedAtUtc]) = 0 AND [FirstIngestedAtUtc] > CAST('0001-01-01T00:00:00+00:00' AS datetimeoffset)");
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceService = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ApplicationId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false, collation: "Latin1_General_100_BIN2"),
                    TenantId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, collation: "Latin1_General_100_BIN2"),
                    ActorKind = table.Column<byte>(type: "tinyint", nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true, collation: "Latin1_General_100_BIN2"),
                    ActorUserNameSnapshot = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ActorSystemId = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: true, collation: "Latin1_General_100_BIN2"),
                    Action = table.Column<string>(type: "varchar(160)", unicode: false, maxLength: 160, nullable: false, collation: "Latin1_General_100_BIN2"),
                    EntityType = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false, collation: "Latin1_General_100_BIN2"),
                    EntityId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true, collation: "Latin1_General_100_BIN2"),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    IngestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", precision: 7, nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(45)", unicode: false, maxLength: 45, nullable: true),
                    DeviceName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    CorrelationId = table.Column<string>(type: "varchar(128)", unicode: false, maxLength: 128, nullable: false, collation: "Latin1_General_100_BIN2"),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                    table.CheckConstraint("CK_AuditEvents_Actor", "([ActorKind] = 1 AND [ActorUserId] IS NOT NULL AND LEN([ActorUserId]) > 0 AND [ActorSystemId] IS NULL) OR ([ActorKind] = 2 AND [ActorSystemId] IS NOT NULL AND LEN([ActorSystemId]) > 0 AND [ActorUserId] IS NULL AND [ActorUserNameSnapshot] IS NULL) OR ([ActorKind] = 3 AND [ActorUserId] IS NULL AND [ActorSystemId] IS NULL AND [ActorUserNameSnapshot] IS NULL)");
                    table.CheckConstraint("CK_AuditEvents_EventId", "[EventId] <> '00000000-0000-0000-0000-000000000000'");
                    table.CheckConstraint("CK_AuditEvents_Identifiers", "LEN([SourceService]) > 0 AND LEN([ApplicationId]) > 0 AND LEN([Action]) > 0 AND LEN([EntityType]) > 0 AND LEN([CorrelationId]) > 0 AND ([TenantId] IS NULL OR LEN([TenantId]) > 0) AND ([EntityId] IS NULL OR LEN([EntityId]) > 0)");
                    table.CheckConstraint("CK_AuditEvents_NewValues", "[NewValues] IS NULL OR ISJSON([NewValues], OBJECT) = 1");
                    table.CheckConstraint("CK_AuditEvents_OldValues", "[OldValues] IS NULL OR ISJSON([OldValues], OBJECT) = 1");
                    table.CheckConstraint("CK_AuditEvents_Status", "[Status] IN (1, 2, 3) AND ([Status] <> 1 OR [EntityId] IS NOT NULL)");
                    table.CheckConstraint("CK_AuditEvents_Utc", "DATEPART(TZOFFSET, [TimestampUtc]) = 0 AND DATEPART(TZOFFSET, [IngestedAtUtc]) = 0 AND [TimestampUtc] > CAST('0001-01-01T00:00:00+00:00' AS datetimeoffset) AND [IngestedAtUtc] > CAST('0001-01-01T00:00:00+00:00' AS datetimeoffset)");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Scope_Actor_Time",
                schema: "dbo",
                table: "AuditEvents",
                columns: new[] { "ApplicationId", "TenantId", "ActorUserId", "TimestampUtc", "Id" },
                descending: new[] { false, false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Scope_Correlation",
                schema: "dbo",
                table: "AuditEvents",
                columns: new[] { "ApplicationId", "TenantId", "CorrelationId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Scope_Entity_Time",
                schema: "dbo",
                table: "AuditEvents",
                columns: new[] { "ApplicationId", "TenantId", "EntityType", "EntityId", "TimestampUtc", "Id" },
                descending: new[] { false, false, false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Scope_Source_Time",
                schema: "dbo",
                table: "AuditEvents",
                columns: new[] { "ApplicationId", "TenantId", "SourceService", "TimestampUtc", "Id" },
                descending: new[] { false, false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_Scope_Time",
                schema: "dbo",
                table: "AuditEvents",
                columns: new[] { "ApplicationId", "TenantId", "TimestampUtc", "Id" },
                descending: new[] { false, false, true, true });

            migrationBuilder.CreateIndex(
                name: "UX_AuditEvents_Source_EventId",
                schema: "dbo",
                table: "AuditEvents",
                columns: new[] { "SourceService", "EventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEventReceipts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AuditEvents",
                schema: "dbo");
        }
    }
}
