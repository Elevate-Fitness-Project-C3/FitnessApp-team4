using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Active",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ExpiresOn" },
                filter: "[RevokedOn] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresOn",
                table: "RefreshTokens",
                column: "ExpiresOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Active",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_ExpiresOn",
                table: "RefreshTokens");
        }
    }
}
