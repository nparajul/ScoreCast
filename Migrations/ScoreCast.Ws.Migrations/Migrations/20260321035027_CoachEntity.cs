using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ScoreCast.Ws.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class CoachEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Coach",
                schema: "scorecast",
                table: "team");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                schema: "scorecast",
                table: "team",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true)
                .Annotation("Relational:ColumnOrder", 11)
                .OldAnnotation("Relational:ColumnOrder", 10);

            migrationBuilder.AddColumn<long>(
                name: "coach_id",
                schema: "scorecast",
                table: "team",
                type: "bigint",
                nullable: true)
                .Annotation("Relational:ColumnOrder", 10);

            migrationBuilder.CreateTable(
                name: "coach",
                schema: "scorecast",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    nationality = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    photo_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    external_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_coach", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_team_coach_id",
                schema: "scorecast",
                table: "team",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "IX_coach_external_id",
                schema: "scorecast",
                table: "coach",
                column: "external_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_team_coach_coach_id",
                schema: "scorecast",
                table: "team",
                column: "coach_id",
                principalSchema: "scorecast",
                principalTable: "coach",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_team_coach_coach_id",
                schema: "scorecast",
                table: "team");

            migrationBuilder.DropTable(
                name: "coach",
                schema: "scorecast");

            migrationBuilder.DropIndex(
                name: "IX_team_coach_id",
                schema: "scorecast",
                table: "team");

            migrationBuilder.DropColumn(
                name: "coach_id",
                schema: "scorecast",
                table: "team");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                schema: "scorecast",
                table: "team",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true)
                .Annotation("Relational:ColumnOrder", 10)
                .OldAnnotation("Relational:ColumnOrder", 11);

            migrationBuilder.AddColumn<string>(
                name: "Coach",
                schema: "scorecast",
                table: "team",
                type: "text",
                nullable: true);
        }
    }
}
