using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityAdministrationAndAuditing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Stop rather than truncate existing claims or silently delete duplicate grants.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM [RoleClaims] WHERE DATALENGTH([ClaimType]) > 256 OR DATALENGTH([ClaimValue]) > 512)
                    THROW 51003, 'Role claims exceed the new bounds. Review the data before deployment.', 1;
                IF EXISTS (SELECT 1 FROM [RoleClaims] WHERE [ClaimType] COLLATE Latin1_General_100_BIN2 = N'permission' AND ([ClaimValue] IS NULL OR LEN([ClaimValue]) = 0))
                    THROW 51004, 'An invalid permission grant requires manual review.', 1;
                IF EXISTS (SELECT 1 FROM [RoleClaims] WHERE [ClaimType] COLLATE Latin1_General_100_BIN2 = N'permission' GROUP BY [RoleId], [ClaimValue] COLLATE Latin1_General_100_BIN2 HAVING COUNT(*) > 1)
                    THROW 51005, 'Duplicate permission grants require manual review.', 1;
                """);
            migrationBuilder.DropIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Users",
                type: "datetimeoffset(7)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Users",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModifiedAt",
                table: "Users",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "Users",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Roles",
                type: "datetimeoffset(7)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Roles",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModifiedAt",
                table: "Roles",
                type: "datetimeoffset(7)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastModifiedBy",
                table: "Roles",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ClaimValue",
                table: "RoleClaims",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            // Historical creation times are unknown. Attribute this one-time backfill explicitly.
            migrationBuilder.Sql("""
                UPDATE [Users] SET [IsActive] = 1, [CreatedAt] = TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00'), [CreatedBy] = N'system:migration';
                UPDATE [Roles] SET [CreatedAt] = TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00'), [CreatedBy] = N'system:migration';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ClaimType",
                table: "RoleClaims",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AuditReportGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ApplicationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, collation: "Latin1_General_100_BIN2"),
                    TenantId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true, collation: "Latin1_General_100_BIN2"),
                    SourceService = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditReportGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditReportGrants_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PermissionGroups",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false, collation: "Latin1_General_100_BIN2"),
                    ParentCode = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true, collation: "Latin1_General_100_BIN2"),
                    Owner = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayNameAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionGroups", x => x.Code);
                    table.ForeignKey(
                        name: "FK_PermissionGroups_PermissionGroups_ParentCode",
                        column: x => x.ParentCode,
                        principalTable: "PermissionGroups",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityAuditLogs",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EventPayload = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAuditLogs", x => x.EventId);
                    table.CheckConstraint("CK_SecurityAuditLogs_Json", "ISJSON([EventPayload]) = 1");
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Code = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false, collation: "Latin1_General_100_BIN2"),
                    GroupCode = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false, collation: "Latin1_General_100_BIN2"),
                    DisplayNameAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Code);
                    table.ForeignKey(
                        name: "FK_Permissions_PermissionGroups_GroupCode",
                        column: x => x.GroupCode,
                        principalTable: "PermissionGroups",
                        principalColumn: "Code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_RoleClaims_Permission",
                table: "RoleClaims",
                columns: new[] { "RoleId", "ClaimValue" },
                unique: true,
                filter: "[ClaimType] = N'permission'");

            migrationBuilder.CreateIndex(
                name: "IX_AuditReportGrants_UserId_ApplicationId_TenantId_SourceService",
                table: "AuditReportGrants",
                columns: new[] { "UserId", "ApplicationId", "TenantId", "SourceService" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionGroups_ParentCode",
                table: "PermissionGroups",
                column: "ParentCode");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_GroupCode_IsActive_DisplayOrder",
                table: "Permissions",
                columns: new[] { "GroupCode", "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_ActorUserId_TimestampUtc",
                table: "SecurityAuditLogs",
                columns: new[] { "ActorUserId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditLogs_TimestampUtc_EventId",
                table: "SecurityAuditLogs",
                columns: new[] { "TimestampUtc", "EventId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditReportGrants");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "SecurityAuditLogs");

            migrationBuilder.DropTable(
                name: "PermissionGroups");

            migrationBuilder.DropIndex(
                name: "UX_RoleClaims_Permission",
                table: "RoleClaims");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "LastModifiedAt",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "LastModifiedBy",
                table: "Roles");

            migrationBuilder.AlterColumn<string>(
                name: "ClaimValue",
                table: "RoleClaims",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true,
                oldCollation: "Latin1_General_100_BIN2");

            migrationBuilder.AlterColumn<string>(
                name: "ClaimType",
                table: "RoleClaims",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128,
                oldNullable: true,
                oldCollation: "Latin1_General_100_BIN2");

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");
        }
    }
}
