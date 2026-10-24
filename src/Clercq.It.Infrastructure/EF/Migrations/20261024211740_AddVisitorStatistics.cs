using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clercq.It.Infrastructure.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitorStatistics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StatsDailyPages",
                columns: table => new
                {
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Path = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Views = table.Column<long>(type: "bigint", nullable: false),
                    UniqueViews = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatsDailyPages", x => new { x.Day, x.Path });
                });

            migrationBuilder.CreateTable(
                name: "StatsDays",
                columns: table => new
                {
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Views = table.Column<long>(type: "bigint", nullable: false),
                    UniqueViews = table.Column<long>(type: "bigint", nullable: false),
                    Visitors = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatsDays", x => x.Day);
                });

            migrationBuilder.CreateTable(
                name: "StatsPageVisits",
                columns: table => new
                {
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Path = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VisitorHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    Views = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatsPageVisits", x => new { x.Day, x.Path, x.VisitorHash });
                });

            migrationBuilder.CreateTable(
                name: "StatsSalts",
                columns: table => new
                {
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Salt = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatsSalts", x => x.Day);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StatsDailyPages");

            migrationBuilder.DropTable(
                name: "StatsDays");

            migrationBuilder.DropTable(
                name: "StatsPageVisits");

            migrationBuilder.DropTable(
                name: "StatsSalts");
        }
    }
}
