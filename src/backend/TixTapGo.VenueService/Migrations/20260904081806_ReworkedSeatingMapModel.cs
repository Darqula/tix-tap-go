using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TixTapGo.VenueService.Migrations
{
    /// <inheritdoc />
    public partial class ReworkedSeatingMapModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Venues_VenueSeatingMapVersions_CurrentSeatingMapId",
                table: "Venues");

            migrationBuilder.DropIndex(
                name: "IX_Venues_CurrentSeatingMapId",
                table: "Venues");

            migrationBuilder.DropColumn(
                name: "CurrentSeatingMapId",
                table: "Venues");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ValidFrom",
                table: "VenueSeatingMapVersions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                table: "VenueSeatingMapVersions",
                type: "boolean",
                nullable: false,
                defaultValue: true);
            
            migrationBuilder.Sql(
                """
                ALTER TABLE "VenueSeatingMapVersions"
                DROP CONSTRAINT "EX_VenueSeatingMapVersions_NoOverlap";
                """);
            
            migrationBuilder.Sql(
                """
                ALTER TABLE "VenueSeatingMapVersions"
                ADD CONSTRAINT "EX_VenueSeatingMapVersions_NoOverlap"
                EXCLUDE USING gist (
                    "VenueId" WITH =,
                    tstzrange("ValidFrom", "ValidToExclusive", '[)') WITH &&
                )
                WHERE ("IsDeleted" = false AND "IsDraft" = false)
                DEFERRABLE INITIALLY DEFERRED;
                """);
            
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "VenueSeatingMapVersions"
                DROP CONSTRAINT "EX_VenueSeatingMapVersions_NoOverlap";
                """);
            
            migrationBuilder.DropColumn(
                name: "IsDraft",
                table: "VenueSeatingMapVersions");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ValidFrom",
                table: "VenueSeatingMapVersions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);
            
            migrationBuilder.Sql(
                """
                ALTER TABLE "VenueSeatingMapVersions"
                ADD CONSTRAINT "EX_VenueSeatingMapVersions_NoOverlap"
                EXCLUDE USING gist (
                    "VenueId" WITH =,
                    tstzrange("ValidFrom", "ValidToExclusive", '[)') WITH &&
                )
                WHERE ("IsDeleted" = false);
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentSeatingMapId",
                table: "Venues",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Venues_CurrentSeatingMapId",
                table: "Venues",
                column: "CurrentSeatingMapId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Venues_VenueSeatingMapVersions_CurrentSeatingMapId",
                table: "Venues",
                column: "CurrentSeatingMapId",
                principalTable: "VenueSeatingMapVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
