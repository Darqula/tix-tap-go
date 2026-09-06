using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TixTapGo.VenueService.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultSoftDeleteIndexFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VenueSeats_CategoryId",
                table: "VenueSeats");

            migrationBuilder.DropIndex(
                name: "IX_VenueSeatingMapVersions_VenueId",
                table: "VenueSeatingMapVersions");

            migrationBuilder.CreateIndex(
                name: "IX_VenueSeats_CategoryId",
                table: "VenueSeats",
                column: "CategoryId",
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_VenueSeatingMapVersions_VenueId",
                table: "VenueSeatingMapVersions",
                column: "VenueId",
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_VenueSeats_CategoryId",
                table: "VenueSeats");

            migrationBuilder.DropIndex(
                name: "IX_VenueSeatingMapVersions_VenueId",
                table: "VenueSeatingMapVersions");

            migrationBuilder.CreateIndex(
                name: "IX_VenueSeats_CategoryId",
                table: "VenueSeats",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueSeatingMapVersions_VenueId",
                table: "VenueSeatingMapVersions",
                column: "VenueId");
        }
    }
}
