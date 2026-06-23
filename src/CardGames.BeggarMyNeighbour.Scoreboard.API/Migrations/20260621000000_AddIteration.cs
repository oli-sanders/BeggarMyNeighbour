using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardGames.BeggarMyNeighbour.Scoreboard.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIteration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Iteration",
                table: "Scores",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Iteration",
                table: "Scores");
        }
    }
}
