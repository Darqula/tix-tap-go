using System;
using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TixTapGo.EventService.Migrations
{
    /// <inheritdoc />
    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public partial class AddEndAndStatusToEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "End",
                table: "Events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.Sql("""UPDATE "Events" SET "End" = "Start" + INTERVAL '1 hour';""");
            
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Events_Status_End",
                table: "Events",
                columns: new[] { "Status", "End" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Event_End_After_Start",
                table: "Events",
                sql: "\"End\" >= \"Start\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_Status_End",
                table: "Events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Event_End_After_Start",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "End",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Events");
        }
    }
}
