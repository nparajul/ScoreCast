using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ScoreCast.Ws.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiFormatSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_prediction_scoring_rule_outcome",
                schema: "scorecast",
                table: "prediction_scoring_rule");

            migrationBuilder.DropIndex(
                name: "IX_prediction_season_id_user_id_match_id",
                schema: "scorecast",
                table: "prediction");

            migrationBuilder.RenameColumn(
                name: "type",
                schema: "scorecast",
                table: "competition",
                newName: "format");

            migrationBuilder.AlterColumn<int>(
                name: "points",
                schema: "scorecast",
                table: "prediction_scoring_rule",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 2);

            migrationBuilder.AlterColumn<string>(
                name: "outcome",
                schema: "scorecast",
                table: "prediction_scoring_rule",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50)
                .Annotation("Relational:ColumnOrder", 3)
                .OldAnnotation("Relational:ColumnOrder", 1);

            migrationBuilder.AlterColumn<int>(
                name: "display_order",
                schema: "scorecast",
                table: "prediction_scoring_rule",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "scorecast",
                table: "prediction_scoring_rule",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200)
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 3);

            migrationBuilder.AddColumn<string>(
                name: "prediction_type",
                schema: "scorecast",
                table: "prediction_scoring_rule",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Score")
                .Annotation("Relational:ColumnOrder", 1);

            migrationBuilder.AddColumn<string>(
                name: "stage_type",
                schema: "scorecast",
                table: "prediction_scoring_rule",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 2);

            migrationBuilder.AlterColumn<int>(
                name: "predicted_home_score",
                schema: "scorecast",
                table: "prediction",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<int>(
                name: "predicted_away_score",
                schema: "scorecast",
                table: "prediction",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AlterColumn<string>(
                name: "outcome",
                schema: "scorecast",
                table: "prediction",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 9)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AlterColumn<long>(
                name: "match_id",
                schema: "scorecast",
                table: "prediction",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "predicted_player_id",
                schema: "scorecast",
                table: "prediction",
                type: "bigint",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 8);

            migrationBuilder.AddColumn<long>(
                name: "predicted_team_id",
                schema: "scorecast",
                table: "prediction",
                type: "bigint",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 7);

            migrationBuilder.AddColumn<string>(
                name: "prediction_type",
                schema: "scorecast",
                table: "prediction",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Score")
                .Annotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<string>(
                name: "venue",
                schema: "scorecast",
                table: "match",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 11)
                .OldAnnotation("Relational:ColumnOrder", 9);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "scorecast",
                table: "match",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Scheduled",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Scheduled")
                .Annotation("Relational:ColumnOrder", 10)
                .OldAnnotation("Relational:ColumnOrder", 8);

            migrationBuilder.AlterColumn<string>(
                name: "referee",
                schema: "scorecast",
                table: "match",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 12)
                .OldAnnotation("Relational:ColumnOrder", 10);

            migrationBuilder.AlterColumn<string>(
                name: "minute",
                schema: "scorecast",
                table: "match",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 13)
                .OldAnnotation("Relational:ColumnOrder", 11);

            migrationBuilder.AlterColumn<DateTime>(
                name: "kickoff_time",
                schema: "scorecast",
                table: "match",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 7)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AlterColumn<int>(
                name: "home_score",
                schema: "scorecast",
                table: "match",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 8)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AlterColumn<string>(
                name: "external_id",
                schema: "scorecast",
                table: "match",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<int>(
                name: "away_score",
                schema: "scorecast",
                table: "match",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 9)
                .OldAnnotation("Relational:ColumnOrder", 7);

            migrationBuilder.AddColumn<long>(
                name: "first_leg_match_id",
                schema: "scorecast",
                table: "match",
                type: "bigint",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 5);

            migrationBuilder.AddColumn<int>(
                name: "leg",
                schema: "scorecast",
                table: "match",
                type: "integer",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 14);

            migrationBuilder.AddColumn<long>(
                name: "match_group_id",
                schema: "scorecast",
                table: "match",
                type: "bigint",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "scorecast",
                table: "gameweek",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Upcoming",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Upcoming")
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "start_date",
                schema: "scorecast",
                table: "gameweek",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 3);

            migrationBuilder.AlterColumn<int>(
                name: "number",
                schema: "scorecast",
                table: "gameweek",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Relational:ColumnOrder", 3)
                .OldAnnotation("Relational:ColumnOrder", 2);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "end_date",
                schema: "scorecast",
                table: "gameweek",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AddColumn<long>(
                name: "stage_id",
                schema: "scorecast",
                table: "gameweek",
                type: "bigint",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 2);

            migrationBuilder.CreateTable(
                name: "stage",
                schema: "scorecast",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    season_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    stage_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "League"),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_stage", x => x.id);
                    table.ForeignKey(
                        name: "FK_stage_season_season_id",
                        column: x => x.season_id,
                        principalSchema: "scorecast",
                        principalTable: "season",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "match_group",
                schema: "scorecast",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    stage_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("PK_match_group", x => x.id);
                    table.ForeignKey(
                        name: "FK_match_group_stage_stage_id",
                        column: x => x.stage_id,
                        principalSchema: "scorecast",
                        principalTable: "stage",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_prediction_scoring_rule_prediction_type_stage_type_outcome",
                schema: "scorecast",
                table: "prediction_scoring_rule",
                columns: new[] { "prediction_type", "stage_type", "outcome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prediction_predicted_player_id",
                schema: "scorecast",
                table: "prediction",
                column: "predicted_player_id");

            migrationBuilder.CreateIndex(
                name: "IX_prediction_predicted_team_id",
                schema: "scorecast",
                table: "prediction",
                column: "predicted_team_id");

            migrationBuilder.CreateIndex(
                name: "IX_prediction_season_id_user_id_match_id",
                schema: "scorecast",
                table: "prediction",
                columns: new[] { "season_id", "user_id", "match_id" },
                unique: true,
                filter: "match_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_prediction_season_id_user_id_prediction_type",
                schema: "scorecast",
                table: "prediction",
                columns: new[] { "season_id", "user_id", "prediction_type" },
                unique: true,
                filter: "match_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_match_first_leg_match_id",
                schema: "scorecast",
                table: "match",
                column: "first_leg_match_id");

            migrationBuilder.CreateIndex(
                name: "IX_match_match_group_id",
                schema: "scorecast",
                table: "match",
                column: "match_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_gameweek_stage_id",
                schema: "scorecast",
                table: "gameweek",
                column: "stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_match_group_stage_id_name",
                schema: "scorecast",
                table: "match_group",
                columns: new[] { "stage_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stage_season_id_name",
                schema: "scorecast",
                table: "stage",
                columns: new[] { "season_id", "name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_gameweek_stage_stage_id",
                schema: "scorecast",
                table: "gameweek",
                column: "stage_id",
                principalSchema: "scorecast",
                principalTable: "stage",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_match_match_first_leg_match_id",
                schema: "scorecast",
                table: "match",
                column: "first_leg_match_id",
                principalSchema: "scorecast",
                principalTable: "match",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_match_match_group_match_group_id",
                schema: "scorecast",
                table: "match",
                column: "match_group_id",
                principalSchema: "scorecast",
                principalTable: "match_group",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_prediction_player_predicted_player_id",
                schema: "scorecast",
                table: "prediction",
                column: "predicted_player_id",
                principalSchema: "scorecast",
                principalTable: "player",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_prediction_team_predicted_team_id",
                schema: "scorecast",
                table: "prediction",
                column: "predicted_team_id",
                principalSchema: "scorecast",
                principalTable: "team",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_gameweek_stage_stage_id",
                schema: "scorecast",
                table: "gameweek");

            migrationBuilder.DropForeignKey(
                name: "FK_match_match_first_leg_match_id",
                schema: "scorecast",
                table: "match");

            migrationBuilder.DropForeignKey(
                name: "FK_match_match_group_match_group_id",
                schema: "scorecast",
                table: "match");

            migrationBuilder.DropForeignKey(
                name: "FK_prediction_player_predicted_player_id",
                schema: "scorecast",
                table: "prediction");

            migrationBuilder.DropForeignKey(
                name: "FK_prediction_team_predicted_team_id",
                schema: "scorecast",
                table: "prediction");

            migrationBuilder.DropTable(
                name: "match_group",
                schema: "scorecast");

            migrationBuilder.DropTable(
                name: "stage",
                schema: "scorecast");

            migrationBuilder.DropIndex(
                name: "IX_prediction_scoring_rule_prediction_type_stage_type_outcome",
                schema: "scorecast",
                table: "prediction_scoring_rule");

            migrationBuilder.DropIndex(
                name: "IX_prediction_predicted_player_id",
                schema: "scorecast",
                table: "prediction");

            migrationBuilder.DropIndex(
                name: "IX_prediction_predicted_team_id",
                schema: "scorecast",
                table: "prediction");

            migrationBuilder.DropIndex(
                name: "IX_prediction_season_id_user_id_match_id",
                schema: "scorecast",
                table: "prediction");

            migrationBuilder.DropIndex(
                name: "IX_prediction_season_id_user_id_prediction_type",
                schema: "scorecast",
                table: "prediction");

            migrationBuilder.DropIndex(
                name: "IX_match_first_leg_match_id",
                schema: "scorecast",
                table: "match");

            migrationBuilder.DropIndex(
                name: "IX_match_match_group_id",
                schema: "scorecast",
                table: "match");

            migrationBuilder.DropIndex(
                name: "IX_gameweek_stage_id",
                schema: "scorecast",
                table: "gameweek");

            migrationBuilder.DropColumn(
                name: "prediction_type",
                schema: "scorecast",
                table: "prediction_scoring_rule");

            migrationBuilder.DropColumn(
                name: "stage_type",
                schema: "scorecast",
                table: "prediction_scoring_rule");

            migrationBuilder.DropColumn(
                name: "predicted_player_id",
                schema: "scorecast",
                table: "prediction");

            migrationBuilder.DropColumn(
                name: "predicted_team_id",
                schema: "scorecast",
                table: "prediction");

            migrationBuilder.DropColumn(
                name: "prediction_type",
                schema: "scorecast",
                table: "prediction");

            migrationBuilder.DropColumn(
                name: "first_leg_match_id",
                schema: "scorecast",
                table: "match");

            migrationBuilder.DropColumn(
                name: "leg",
                schema: "scorecast",
                table: "match");

            migrationBuilder.DropColumn(
                name: "match_group_id",
                schema: "scorecast",
                table: "match");

            migrationBuilder.DropColumn(
                name: "stage_id",
                schema: "scorecast",
                table: "gameweek");

            migrationBuilder.RenameColumn(
                name: "format",
                schema: "scorecast",
                table: "competition",
                newName: "type");

            migrationBuilder.AlterColumn<int>(
                name: "points",
                schema: "scorecast",
                table: "prediction_scoring_rule",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Relational:ColumnOrder", 2)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<string>(
                name: "outcome",
                schema: "scorecast",
                table: "prediction_scoring_rule",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50)
                .Annotation("Relational:ColumnOrder", 1)
                .OldAnnotation("Relational:ColumnOrder", 3);

            migrationBuilder.AlterColumn<int>(
                name: "display_order",
                schema: "scorecast",
                table: "prediction_scoring_rule",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AlterColumn<string>(
                name: "description",
                schema: "scorecast",
                table: "prediction_scoring_rule",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200)
                .Annotation("Relational:ColumnOrder", 3)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AlterColumn<int>(
                name: "predicted_home_score",
                schema: "scorecast",
                table: "prediction",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AlterColumn<int>(
                name: "predicted_away_score",
                schema: "scorecast",
                table: "prediction",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AlterColumn<string>(
                name: "outcome",
                schema: "scorecast",
                table: "prediction",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 9);

            migrationBuilder.AlterColumn<long>(
                name: "match_id",
                schema: "scorecast",
                table: "prediction",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "venue",
                schema: "scorecast",
                table: "match",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 9)
                .OldAnnotation("Relational:ColumnOrder", 11);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "scorecast",
                table: "match",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Scheduled",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Scheduled")
                .Annotation("Relational:ColumnOrder", 8)
                .OldAnnotation("Relational:ColumnOrder", 10);

            migrationBuilder.AlterColumn<string>(
                name: "referee",
                schema: "scorecast",
                table: "match",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 10)
                .OldAnnotation("Relational:ColumnOrder", 12);

            migrationBuilder.AlterColumn<string>(
                name: "minute",
                schema: "scorecast",
                table: "match",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 11)
                .OldAnnotation("Relational:ColumnOrder", 13);

            migrationBuilder.AlterColumn<DateTime>(
                name: "kickoff_time",
                schema: "scorecast",
                table: "match",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 7);

            migrationBuilder.AlterColumn<int>(
                name: "home_score",
                schema: "scorecast",
                table: "match",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 8);

            migrationBuilder.AlterColumn<string>(
                name: "external_id",
                schema: "scorecast",
                table: "match",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AlterColumn<int>(
                name: "away_score",
                schema: "scorecast",
                table: "match",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 7)
                .OldAnnotation("Relational:ColumnOrder", 9);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "scorecast",
                table: "gameweek",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Upcoming",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Upcoming")
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "start_date",
                schema: "scorecast",
                table: "gameweek",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 3)
                .OldAnnotation("Relational:ColumnOrder", 4);

            migrationBuilder.AlterColumn<int>(
                name: "number",
                schema: "scorecast",
                table: "gameweek",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Relational:ColumnOrder", 2)
                .OldAnnotation("Relational:ColumnOrder", 3);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "end_date",
                schema: "scorecast",
                table: "gameweek",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true)
                .Annotation("Relational:ColumnOrder", 4)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.CreateIndex(
                name: "IX_prediction_scoring_rule_outcome",
                schema: "scorecast",
                table: "prediction_scoring_rule",
                column: "outcome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prediction_season_id_user_id_match_id",
                schema: "scorecast",
                table: "prediction",
                columns: new[] { "season_id", "user_id", "match_id" },
                unique: true);
        }
    }
}
