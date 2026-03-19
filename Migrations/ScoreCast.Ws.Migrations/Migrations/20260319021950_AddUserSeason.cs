using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ScoreCast.Ws.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSeason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_season",
                schema: "scorecast",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    season_id = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_user_season", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_season_season_season_id",
                        column: x => x.season_id,
                        principalSchema: "scorecast",
                        principalTable: "season",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_season_user_master_user_id",
                        column: x => x.user_id,
                        principalSchema: "scorecast",
                        principalTable: "user_master",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_season_season_id",
                schema: "scorecast",
                table: "user_season",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_season_user_id_season_id",
                schema: "scorecast",
                table: "user_season",
                columns: new[] { "user_id", "season_id" },
                unique: true);

            // Backfill: create user_season rows for users who already have predictions
            migrationBuilder.Sql("""
                INSERT INTO scorecast.user_season (user_id, season_id, created_by, created_date, created_by_app, modified_by, modified_date, is_deleted)
                SELECT DISTINCT p.user_id, p.season_id, 'migration', now(), 'AddUserSeason', 'migration', now(), false
                FROM scorecast.prediction p
                WHERE NOT EXISTS (
                    SELECT 1 FROM scorecast.user_season us
                    WHERE us.user_id = p.user_id AND us.season_id = p.season_id
                )
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_season",
                schema: "scorecast");
        }
    }
}
