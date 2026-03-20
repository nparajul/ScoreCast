using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ScoreCast.Ws.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskPlayTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "risk_play",
                schema: "scorecast",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    season_id = table.Column<long>(type: "bigint", nullable: false),
                    gameweek_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    match_id = table.Column<long>(type: "bigint", nullable: false),
                    risk_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    selection = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bonus_points = table.Column<int>(type: "integer", nullable: true),
                    is_resolved = table.Column<bool>(type: "boolean", nullable: true),
                    is_won = table.Column<bool>(type: "boolean", nullable: true),
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
                    table.PrimaryKey("PK_risk_play", x => x.id);
                    table.ForeignKey(
                        name: "FK_risk_play_gameweek_gameweek_id",
                        column: x => x.gameweek_id,
                        principalSchema: "scorecast",
                        principalTable: "gameweek",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_risk_play_match_match_id",
                        column: x => x.match_id,
                        principalSchema: "scorecast",
                        principalTable: "match",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_risk_play_season_season_id",
                        column: x => x.season_id,
                        principalSchema: "scorecast",
                        principalTable: "season",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_risk_play_user_master_user_id",
                        column: x => x.user_id,
                        principalSchema: "scorecast",
                        principalTable: "user_master",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_risk_play_gameweek_id",
                schema: "scorecast",
                table: "risk_play",
                column: "gameweek_id");

            migrationBuilder.CreateIndex(
                name: "IX_risk_play_match_id",
                schema: "scorecast",
                table: "risk_play",
                column: "match_id");

            migrationBuilder.CreateIndex(
                name: "IX_risk_play_season_id",
                schema: "scorecast",
                table: "risk_play",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "IX_risk_play_user_id_match_id_risk_type",
                schema: "scorecast",
                table: "risk_play",
                columns: new[] { "user_id", "match_id", "risk_type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "risk_play",
                schema: "scorecast");
        }
    }
}
