using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clercq.It.Infrastructure.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "Projects",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Company",
                table: "Projects");
        }
    }
}
