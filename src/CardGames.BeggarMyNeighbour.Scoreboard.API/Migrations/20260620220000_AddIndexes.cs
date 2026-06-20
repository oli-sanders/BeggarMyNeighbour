using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CardGames.BeggarMyNeighbour.Scoreboard.API.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Scores_Submitted",
                table: "Scores",
                column: "Submitted");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_Deck_Players",
                table: "Scores",
                columns: new[] { "Deck", "Players" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Scores_Submitted",
                table: "Scores");

            migrationBuilder.DropIndex(
                name: "IX_Scores_Deck_Players",
                table: "Scores");
        }
    }
}
