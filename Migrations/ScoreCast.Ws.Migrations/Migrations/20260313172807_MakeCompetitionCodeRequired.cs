using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScoreCast.Ws.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class MakeCompetitionCodeRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_competition_code",
                schema: "scorecast",
                table: "competition");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                schema: "scorecast",
                table: "competition",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_competition_code",
                schema: "scorecast",
                table: "competition",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_competition_code",
                schema: "scorecast",
                table: "competition");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                schema: "scorecast",
                table: "competition",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.CreateIndex(
                name: "IX_competition_code",
                schema: "scorecast",
                table: "competition",
                column: "code",
                unique: true,
                filter: "code IS NOT NULL");
        }
    }
}
