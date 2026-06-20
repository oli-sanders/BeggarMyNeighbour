using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardGames.BeggarMyNeighbour.Scoreboard.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Team",
                table: "Scores",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Team",
                table: "Scores");
        }
    }
}
