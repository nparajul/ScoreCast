using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScoreCast.Ws.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchEventMinute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_match_event_match_id_player_id_event_type",
                schema: "scorecast",
                table: "match_event");

            migrationBuilder.AddColumn<string>(
                name: "minute",
                schema: "scorecast",
                table: "match_event",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 5);

            migrationBuilder.CreateIndex(
                name: "IX_match_event_match_id_player_id_event_type_minute",
                schema: "scorecast",
                table: "match_event",
                columns: new[] { "match_id", "player_id", "event_type", "minute" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_match_event_match_id_player_id_event_type_minute",
                schema: "scorecast",
                table: "match_event");

            migrationBuilder.DropColumn(
                name: "minute",
                schema: "scorecast",
                table: "match_event");

            migrationBuilder.CreateIndex(
                name: "IX_match_event_match_id_player_id_event_type",
                schema: "scorecast",
                table: "match_event",
                columns: new[] { "match_id", "player_id", "event_type" },
                unique: true);
        }
    }
}
