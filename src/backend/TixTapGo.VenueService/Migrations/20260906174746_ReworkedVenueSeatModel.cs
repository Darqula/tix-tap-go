using System;
using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TixTapGo.VenueService.Migrations
{
    /// <inheritdoc />
    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public partial class ReworkedVenueSeatModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VenueSeats_VenueSeatingMapVersions_VenueSeatingMapVersionId",
                table: "VenueSeats");

            migrationBuilder.DropIndex(
                name: "IX_VenueSeats_VenueSeatingMapVersionId",
                table: "VenueSeats");

            migrationBuilder.DropIndex(
                name: "IX_VenueSeats_VenueSeatMapVersionId_MapPositionX_MapPositionY",
                table: "VenueSeats");

            migrationBuilder.DropIndex(
                name: "IX_VenueSeats_VenueSeatMapVersionId_RowNumber_SeatNumber",
                table: "VenueSeats");

            migrationBuilder.DropIndex(
                name: "IX_SeatCategories_VenueId",
                table: "SeatCategories");

            migrationBuilder.DropColumn(
                name: "VenueSeatMapVersionId",
                table: "VenueSeats");

            migrationBuilder.RenameColumn(
                name: "VenueSeatingMapVersionId",
                table: "VenueSeats",
                newName: "SeatingMapVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueSeats_SeatingMapVersionId_MapPositionX_MapPositionY",
                table: "VenueSeats",
                columns: new[] { "SeatingMapVersionId", "MapPositionX", "MapPositionY" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_VenueSeats_SeatingMapVersionId_RowNumber_SeatNumber",
                table: "VenueSeats",
                columns: new[] { "SeatingMapVersionId", "RowNumber", "SeatNumber" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_SeatCategories_VenueId_Title",
                table: "SeatCategories",
                columns: new[] { "VenueId", "Title" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_VenueSeats_VenueSeatingMapVersions_SeatingMapVersionId",
                table: "VenueSeats",
                column: "SeatingMapVersionId",
                principalTable: "VenueSeatingMapVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VenueSeats_VenueSeatingMapVersions_SeatingMapVersionId",
                table: "VenueSeats");

            migrationBuilder.DropIndex(
                name: "IX_VenueSeats_SeatingMapVersionId_MapPositionX_MapPositionY",
                table: "VenueSeats");

            migrationBuilder.DropIndex(
                name: "IX_VenueSeats_SeatingMapVersionId_RowNumber_SeatNumber",
                table: "VenueSeats");

            migrationBuilder.DropIndex(
                name: "IX_SeatCategories_VenueId_Title",
                table: "SeatCategories");

            migrationBuilder.RenameColumn(
                name: "SeatingMapVersionId",
                table: "VenueSeats",
                newName: "VenueSeatingMapVersionId");

            migrationBuilder.AddColumn<Guid>(
                name: "VenueSeatMapVersionId",
                table: "VenueSeats",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_VenueSeats_VenueSeatingMapVersionId",
                table: "VenueSeats",
                column: "VenueSeatingMapVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueSeats_VenueSeatMapVersionId_MapPositionX_MapPositionY",
                table: "VenueSeats",
                columns: new[] { "VenueSeatMapVersionId", "MapPositionX", "MapPositionY" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_VenueSeats_VenueSeatMapVersionId_RowNumber_SeatNumber",
                table: "VenueSeats",
                columns: new[] { "VenueSeatMapVersionId", "RowNumber", "SeatNumber" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_SeatCategories_VenueId",
                table: "SeatCategories",
                column: "VenueId");

            migrationBuilder.AddForeignKey(
                name: "FK_VenueSeats_VenueSeatingMapVersions_VenueSeatingMapVersionId",
                table: "VenueSeats",
                column: "VenueSeatingMapVersionId",
                principalTable: "VenueSeatingMapVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
