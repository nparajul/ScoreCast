using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ScoreCast.Ws.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryLeagueAndTeamMasters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "country",
                schema: "scorecast",
                table: "league_master");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                schema: "scorecast",
                table: "team_master",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true)
                .Annotation("Relational:ColumnOrder", 6)
                .OldAnnotation("Relational:ColumnOrder", 5);

            migrationBuilder.AddColumn<long>(
                name: "country_id",
                schema: "scorecast",
                table: "team_master",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Relational:ColumnOrder", 5);

            migrationBuilder.AddColumn<long>(
                name: "country_id",
                schema: "scorecast",
                table: "league_master",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Relational:ColumnOrder", 2);

            migrationBuilder.CreateTable(
                name: "country_master",
                schema: "scorecast",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    flag_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_country_master", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_team_master_country_id",
                schema: "scorecast",
                table: "team_master",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_league_master_country_id",
                schema: "scorecast",
                table: "league_master",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_country_master_code",
                schema: "scorecast",
                table: "country_master",
                column: "code",
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_country_master_name",
                schema: "scorecast",
                table: "country_master",
                column: "name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_league_master_country_master_country_id",
                schema: "scorecast",
                table: "league_master",
                column: "country_id",
                principalSchema: "scorecast",
                principalTable: "country_master",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_team_master_country_master_country_id",
                schema: "scorecast",
                table: "team_master",
                column: "country_id",
                principalSchema: "scorecast",
                principalTable: "country_master",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_league_master_country_master_country_id",
                schema: "scorecast",
                table: "league_master");

            migrationBuilder.DropForeignKey(
                name: "FK_team_master_country_master_country_id",
                schema: "scorecast",
                table: "team_master");

            migrationBuilder.DropTable(
                name: "country_master",
                schema: "scorecast");

            migrationBuilder.DropIndex(
                name: "IX_team_master_country_id",
                schema: "scorecast",
                table: "team_master");

            migrationBuilder.DropIndex(
                name: "IX_league_master_country_id",
                schema: "scorecast",
                table: "league_master");

            migrationBuilder.DropColumn(
                name: "country_id",
                schema: "scorecast",
                table: "team_master");

            migrationBuilder.DropColumn(
                name: "country_id",
                schema: "scorecast",
                table: "league_master");

            migrationBuilder.AlterColumn<bool>(
                name: "is_active",
                schema: "scorecast",
                table: "team_master",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true)
                .Annotation("Relational:ColumnOrder", 5)
                .OldAnnotation("Relational:ColumnOrder", 6);

            migrationBuilder.AddColumn<string>(
                name: "country",
                schema: "scorecast",
                table: "league_master",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("Relational:ColumnOrder", 2);
        }
    }
}
