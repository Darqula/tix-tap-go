using System;
using System.Diagnostics.CodeAnalysis;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TixTapGo.VenueService.Migrations
{
    /// <inheritdoc />
    [SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]
    public partial class AddedCoreVenueModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:btree_gist", ",,");

            migrationBuilder.CreateTable(
                name: "SeatCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VenueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    Color = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Venues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Address = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CurrentSeatingMapId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Venues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VenueSeatingMapVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VenueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MapUrl = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ValidFrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ValidToExclusive = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueSeatingMapVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenueSeatingMapVersions_Venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "Venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VenueSeats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VenueSeatMapVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    VenueSeatingMapVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowNumber = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    SeatNumber = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    MapPositionX = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    MapPositionY = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VenueSeats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VenueSeats_SeatCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "SeatCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VenueSeats_VenueSeatingMapVersions_VenueSeatingMapVersionId",
                        column: x => x.VenueSeatingMapVersionId,
                        principalTable: "VenueSeatingMapVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeatCategories_VenueId",
                table: "SeatCategories",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_Venues_CurrentSeatingMapId",
                table: "Venues",
                column: "CurrentSeatingMapId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VenueSeatingMapVersions_VenueId",
                table: "VenueSeatingMapVersions",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_VenueSeats_CategoryId",
                table: "VenueSeats",
                column: "CategoryId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_SeatCategories_Venues_VenueId",
                table: "SeatCategories",
                column: "VenueId",
                principalTable: "Venues",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Venues_VenueSeatingMapVersions_CurrentSeatingMapId",
                table: "Venues",
                column: "CurrentSeatingMapId",
                principalTable: "VenueSeatingMapVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
            
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "VenueSeatingMapVersions"
                DROP CONSTRAINT "EX_VenueSeatingMapVersions_NoOverlap";
                """);
            
            migrationBuilder.DropForeignKey(
                name: "FK_VenueSeatingMapVersions_Venues_VenueId",
                table: "VenueSeatingMapVersions");

            migrationBuilder.DropTable(
                name: "VenueSeats");

            migrationBuilder.DropTable(
                name: "SeatCategories");

            migrationBuilder.DropTable(
                name: "Venues");

            migrationBuilder.DropTable(
                name: "VenueSeatingMapVersions");
        }
    }
}
