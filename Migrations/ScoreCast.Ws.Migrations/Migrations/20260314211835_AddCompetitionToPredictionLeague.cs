using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScoreCast.Ws.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionToPredictionLeague : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "season_id",
                schema: "scorecast",
                table: "prediction_league",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 3);

            migrationBuilder.AlterColumn<long>(
                name: "created_by_user_id",
                schema: "scorecast",
                table: "prediction_league",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AddColumn<long>(
                name: "competition_id",
                schema: "scorecast",
                table: "prediction_league",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Relational:ColumnOrder", 3);

            // Backfill existing leagues: set competition_id from season.competition_id
            migrationBuilder.Sql("""
                UPDATE scorecast.prediction_league pl
                SET competition_id = s.competition_id
                FROM scorecast.season s
                WHERE s.id = pl.season_id
                """);

            migrationBuilder.CreateIndex(
                name: "IX_prediction_league_competition_id",
                schema: "scorecast",
                table: "prediction_league",
                column: "competition_id");

            migrationBuilder.AddForeignKey(
                name: "FK_prediction_league_competition_competition_id",
                schema: "scorecast",
                table: "prediction_league",
                column: "competition_id",
                principalSchema: "scorecast",
                principalTable: "competition",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_prediction_league_competition_competition_id",
                schema: "scorecast",
                table: "prediction_league");

            migrationBuilder.DropIndex(
                name: "IX_prediction_league_competition_id",
                schema: "scorecast",
                table: "prediction_league");

            migrationBuilder.DropColumn(
                name: "competition_id",
                schema: "scorecast",
                table: "prediction_league");

            migrationBuilder.AlterColumn<long>(
                name: "season_id",
                schema: "scorecast",
                table: "prediction_league",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Relational:ColumnOrder", 3)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<long>(
                name: "created_by_user_id",
                schema: "scorecast",
                table: "prediction_league",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 5);
        }
    }
}
