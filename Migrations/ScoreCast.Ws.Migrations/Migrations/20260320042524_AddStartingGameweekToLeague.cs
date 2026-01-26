using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScoreCast.Ws.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddStartingGameweekToLeague : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "starting_gameweek_id",
                schema: "scorecast",
                table: "prediction_league",
                type: "bigint",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 6);

            migrationBuilder.CreateIndex(
                name: "IX_prediction_league_starting_gameweek_id",
                schema: "scorecast",
                table: "prediction_league",
                column: "starting_gameweek_id");

            migrationBuilder.AddForeignKey(
                name: "FK_prediction_league_gameweek_starting_gameweek_id",
                schema: "scorecast",
                table: "prediction_league",
                column: "starting_gameweek_id",
                principalSchema: "scorecast",
                principalTable: "gameweek",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_prediction_league_gameweek_starting_gameweek_id",
                schema: "scorecast",
                table: "prediction_league");

            migrationBuilder.DropIndex(
                name: "IX_prediction_league_starting_gameweek_id",
                schema: "scorecast",
                table: "prediction_league");

            migrationBuilder.DropColumn(
                name: "starting_gameweek_id",
                schema: "scorecast",
                table: "prediction_league");
        }
    }
}
