using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clercq.It.Infrastructure.EF.Migrations
{
    /// <summary>
    /// Visitor hashes and daily salts become upper-case hex text (#431). PostgreSQL has no
    /// automatic cast from bytea, so the existing values are converted with encode().
    /// </summary>
    public partial class StoreStatisticsHashesAsHex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """ALTER TABLE "StatsSalts" ALTER COLUMN "Salt" TYPE character(64) USING upper(encode("Salt", 'hex'));""");
            migrationBuilder.Sql(
                """ALTER TABLE "StatsPageVisits" ALTER COLUMN "VisitorHash" TYPE character(64) USING upper(encode("VisitorHash", 'hex'));""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """ALTER TABLE "StatsSalts" ALTER COLUMN "Salt" TYPE bytea USING decode("Salt", 'hex');""");
            migrationBuilder.Sql(
                """ALTER TABLE "StatsPageVisits" ALTER COLUMN "VisitorHash" TYPE bytea USING decode("VisitorHash", 'hex');""");
        }
    }
}
