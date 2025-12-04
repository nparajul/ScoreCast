using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ScoreCast.Ws.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionLeagues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_prediction_user_id_match_id",
                schema: "scorecast",
                table: "prediction");

            migrationBuilder.AlterColumn<long>(
                name: "user_id",
                schema: "scorecast",
                table: "prediction",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Relational:ColumnOrder", 2)
                .OldAnnotation("Relational:ColumnOrder", 1);

            migrationBuilder.AlterColumn<int>(
                name: "predicted_home_score",
                schema: "scorecast",
                table: "prediction",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 3);

            migrationBuilder.AlterColumn<int>(
                name: "predicted_away_score",
                schema: "scorecast",
                table: "prediction",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<int>(
                name: "points_awarded",
                schema: "scorecast",
                table: "prediction",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0)
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AlterColumn<long>(
                name: "match_id",
                schema: "scorecast",
                table: "prediction",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Relational:ColumnOrder", 3)
                .OldAnnotation("Relational:ColumnOrder", 2);

            migrationBuilder.AddColumn<long>(
                name: "prediction_league_id",
                schema: "scorecast",
                table: "prediction",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Relational:ColumnOrder", 1);

            migrationBuilder.CreateTable(
                name: "prediction_league",
                schema: "scorecast",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    invite_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    season_id = table.Column<long>(type: "bigint", nullable: false),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_prediction_league", x => x.id);
                    table.ForeignKey(
                        name: "FK_prediction_league_season_season_id",
                        column: x => x.season_id,
                        principalSchema: "scorecast",
                        principalTable: "season",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_prediction_league_user_master_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalSchema: "scorecast",
                        principalTable: "user_master",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prediction_league_member",
                schema: "scorecast",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    prediction_league_id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_prediction_league_member", x => x.id);
                    table.ForeignKey(
                        name: "FK_prediction_league_member_prediction_league_prediction_leagu~",
                        column: x => x.prediction_league_id,
                        principalSchema: "scorecast",
                        principalTable: "prediction_league",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prediction_league_member_user_master_user_id",
                        column: x => x.user_id,
                        principalSchema: "scorecast",
                        principalTable: "user_master",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_prediction_prediction_league_id_user_id_match_id",
                schema: "scorecast",
                table: "prediction",
                columns: new[] { "prediction_league_id", "user_id", "match_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prediction_user_id",
                schema: "scorecast",
                table: "prediction",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_prediction_league_created_by_user_id",
                schema: "scorecast",
                table: "prediction_league",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_prediction_league_invite_code",
                schema: "scorecast",
                table: "prediction_league",
                column: "invite_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prediction_league_season_id",
                schema: "scorecast",
                table: "prediction_league",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "IX_prediction_league_member_prediction_league_id_user_id",
                schema: "scorecast",
                table: "prediction_league_member",
                columns: new[] { "prediction_league_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prediction_league_member_user_id",
                schema: "scorecast",
                table: "prediction_league_member",
                column: "user_id");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_prediction_prediction_league_prediction_league_id",
                schema: "scorecast",
                table: "prediction");

            migrationBuilder.DropTable(
                name: "prediction_league_member",
                schema: "scorecast");

            migrationBuilder.DropTable(
                name: "prediction_league",
                schema: "scorecast");

            migrationBuilder.DropIndex(
                name: "IX_prediction_prediction_league_id_user_id_match_id",
                schema: "scorecast",
                table: "prediction");

            migrationBuilder.DropIndex(
                name: "IX_prediction_user_id",
                schema: "scorecast",
                table: "prediction");

            migrationBuilder.DropColumn(
                name: "prediction_league_id",
                schema: "scorecast",
                table: "prediction");

            migrationBuilder.AlterColumn<long>(
                name: "user_id",
                schema: "scorecast",
                table: "prediction",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Relational:ColumnOrder", 1)
                .OldAnnotation("Relational:ColumnOrder", 2);

            migrationBuilder.AlterColumn<int>(
                name: "predicted_home_score",
                schema: "scorecast",
                table: "prediction",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Relational:ColumnOrder", 3)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<int>(
                name: "predicted_away_score",
                schema: "scorecast",
                table: "prediction",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AlterColumn<int>(
                name: "points_awarded",
                schema: "scorecast",
                table: "prediction",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0)
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AlterColumn<long>(
                name: "match_id",
                schema: "scorecast",
                table: "prediction",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Relational:ColumnOrder", 2)
                .OldAnnotation("Relational:ColumnOrder", 3);

            migrationBuilder.CreateIndex(
                name: "IX_prediction_user_id_match_id",
                schema: "scorecast",
                table: "prediction",
                columns: new[] { "user_id", "match_id" },
                unique: true);
        }
    }
}
