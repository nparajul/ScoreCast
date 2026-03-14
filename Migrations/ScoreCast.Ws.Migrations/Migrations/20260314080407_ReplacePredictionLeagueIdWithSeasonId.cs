using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScoreCast.Ws.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePredictionLeagueIdWithSeasonId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_prediction_prediction_league_prediction_league_id",
                schema: "scorecast",
                table: "prediction");

            migrationBuilder.RenameColumn(
                name: "prediction_league_id",
                schema: "scorecast",
                table: "prediction",
                newName: "season_id");

            migrationBuilder.RenameIndex(
                name: "IX_prediction_prediction_league_id_user_id_match_id",
                schema: "scorecast",
                table: "prediction",
                newName: "IX_prediction_season_id_user_id_match_id");

            migrationBuilder.AddForeignKey(
                name: "FK_prediction_season_season_id",
                schema: "scorecast",
                table: "prediction",
                column: "season_id",
                principalSchema: "scorecast",
                principalTable: "season",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_prediction_season_season_id",
                schema: "scorecast",
                table: "prediction");

            migrationBuilder.RenameColumn(
                name: "season_id",
                schema: "scorecast",
                table: "prediction",
                newName: "prediction_league_id");

            migrationBuilder.RenameIndex(
                name: "IX_prediction_season_id_user_id_match_id",
                schema: "scorecast",
                table: "prediction",
                newName: "IX_prediction_prediction_league_id_user_id_match_id");

            migrationBuilder.AddForeignKey(
                name: "FK_prediction_prediction_league_prediction_league_id",
                schema: "scorecast",
                table: "prediction",
                column: "prediction_league_id",
                principalSchema: "scorecast",
                principalTable: "prediction_league",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
