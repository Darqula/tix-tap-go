using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TixTapGo.EventService.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultSoftDeleteIndexFilter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AttendeeGroups_EventId",
                table: "AttendeeGroups");

            migrationBuilder.CreateIndex(
                name: "IX_AttendeeGroups_EventId",
                table: "AttendeeGroups",
                column: "EventId",
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AttendeeGroups_EventId",
                table: "AttendeeGroups");

            migrationBuilder.CreateIndex(
                name: "IX_AttendeeGroups_EventId",
                table: "AttendeeGroups",
                column: "EventId");
        }
    }
}
