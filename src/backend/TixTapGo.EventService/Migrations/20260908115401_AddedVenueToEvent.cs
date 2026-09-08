using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TixTapGo.EventService.Migrations
{
    /// <inheritdoc />
    public partial class AddedVenueToEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing test data does not have a venue. The simplest solution is to delete it 
            migrationBuilder.Sql("DELETE FROM \"Events\";");
            
            migrationBuilder.AddColumn<Guid>(
                name: "VenueId",
                table: "Events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<bool>(
                name: "VenuePendingResolution",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Events_VenueId",
                table: "Events",
                column: "VenueId",
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_VenueId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "VenueId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "VenuePendingResolution",
                table: "Events");
        }
    }
}
