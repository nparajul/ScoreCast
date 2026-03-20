using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScoreCast.Ws.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class RenameDbColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "current_streak",
                schema: "scorecast",
                table: "user_master",
                newName: "best_gameweek");

            migrationBuilder.RenameColumn(
                name: "keycloak_user_id",
                schema: "scorecast",
                table: "user_master",
                newName: "firebase_uid");

            migrationBuilder.RenameIndex(
                name: "IX_user_master_keycloak_user_id",
                schema: "scorecast",
                table: "user_master",
                newName: "IX_user_master_firebase_uid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "best_gameweek",
                schema: "scorecast",
                table: "user_master",
                newName: "current_streak");

            migrationBuilder.RenameColumn(
                name: "firebase_uid",
                schema: "scorecast",
                table: "user_master",
                newName: "keycloak_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_user_master_firebase_uid",
                schema: "scorecast",
                table: "user_master",
                newName: "IX_user_master_keycloak_user_id");
        }
    }
}
