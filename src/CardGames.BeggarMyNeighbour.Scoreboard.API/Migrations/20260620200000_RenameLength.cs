using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardGames.BeggarMyNeighbour.Scoreboard.API.Migrations
{
    /// <inheritdoc />
    public partial class RenameLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Lenght",
                table: "Scores",
                newName: "Length");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Length",
                table: "Scores",
                newName: "Lenght");
        }
    }
}
