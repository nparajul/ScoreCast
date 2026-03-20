using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ScoreCast.Ws.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchHighlight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "display_order",
                schema: "scorecast",
                table: "user_season",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Relational:ColumnOrder", 3);

            migrationBuilder.AddColumn<long>(
                name: "away_coach_id",
                schema: "scorecast",
                table: "match",
                type: "bigint",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 16);

            migrationBuilder.AddColumn<long>(
                name: "home_coach_id",
                schema: "scorecast",
                table: "match",
                type: "bigint",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 15);

            migrationBuilder.AddColumn<DateOnly>(
                name: "valid_from",
                schema: "scorecast",
                table: "coach",
                type: "date",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 6);

            migrationBuilder.CreateTable(
                name: "match_highlight",
                schema: "scorecast",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    match_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    embed_html = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "current_user"),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    created_by_app = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    modified_by = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValueSql: "current_user"),
                    modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    modified_by_app = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_highlight", x => x.id);
                    table.ForeignKey(
                        name: "FK_match_highlight_match_match_id",
                        column: x => x.match_id,
                        principalSchema: "scorecast",
                        principalTable: "match",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_match_away_coach_id",
                schema: "scorecast",
                table: "match",
                column: "away_coach_id");

            migrationBuilder.CreateIndex(
                name: "IX_match_home_coach_id",
                schema: "scorecast",
                table: "match",
                column: "home_coach_id");

            migrationBuilder.CreateIndex(
                name: "IX_match_highlight_match_id_title",
                schema: "scorecast",
                table: "match_highlight",
                columns: new[] { "match_id", "title" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_match_coach_away_coach_id",
                schema: "scorecast",
                table: "match",
                column: "away_coach_id",
                principalSchema: "scorecast",
                principalTable: "coach",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_match_coach_home_coach_id",
                schema: "scorecast",
                table: "match",
                column: "home_coach_id",
                principalSchema: "scorecast",
                principalTable: "coach",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_match_coach_away_coach_id",
                schema: "scorecast",
                table: "match");

            migrationBuilder.DropForeignKey(
                name: "FK_match_coach_home_coach_id",
                schema: "scorecast",
                table: "match");

            migrationBuilder.DropTable(
                name: "match_highlight",
                schema: "scorecast");

            migrationBuilder.DropIndex(
                name: "IX_match_away_coach_id",
                schema: "scorecast",
                table: "match");

            migrationBuilder.DropIndex(
                name: "IX_match_home_coach_id",
                schema: "scorecast",
                table: "match");

            migrationBuilder.DropColumn(
                name: "display_order",
                schema: "scorecast",
                table: "user_season");

            migrationBuilder.DropColumn(
                name: "away_coach_id",
                schema: "scorecast",
                table: "match");

            migrationBuilder.DropColumn(
                name: "home_coach_id",
                schema: "scorecast",
                table: "match");

            migrationBuilder.DropColumn(
                name: "valid_from",
                schema: "scorecast",
                table: "coach");
        }
    }
}
