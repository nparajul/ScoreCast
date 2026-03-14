using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ScoreCast.Ws.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchVenueAndReferee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "referee",
                schema: "scorecast",
                table: "match",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 10);

            migrationBuilder.AddColumn<string>(
                name: "venue",
                schema: "scorecast",
                table: "match",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("Relational:ColumnOrder", 9);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "referee",
                schema: "scorecast",
                table: "match");

            migrationBuilder.DropColumn(
                name: "venue",
                schema: "scorecast",
                table: "match");
        }
    }
}
