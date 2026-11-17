using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace IdentityService.Persistence.Migrations;
public partial class AddEmailOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("EmailOutbox", table => new
        {
            Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
            UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
            Purpose = table.Column<int>(type: "int", nullable: false),
            CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
            NextAttemptOn = table.Column<DateTime>(type: "datetime2", nullable: false),
            Attempts = table.Column<int>(type: "int", nullable: false),
            LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
            LockToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
            ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_EmailOutbox", x => x.Id);
            table.ForeignKey("FK_EmailOutbox_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_EmailOutbox_Due", "EmailOutbox", new[] { "ProcessedOn", "NextAttemptOn" });
        migrationBuilder.CreateIndex("UX_EmailOutbox_Pending", "EmailOutbox", new[] { "UserId", "Purpose" }, unique: true, filter: "[ProcessedOn] IS NULL");
        migrationBuilder.Sql("DELETE FROM [UserTokens] WHERE [LoginProvider] = N'FitnessApp.AuthEmailQueue';");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("EmailOutbox");
    }
}
