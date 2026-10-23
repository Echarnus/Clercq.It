using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Clercq.It.Infrastructure.EF.Migrations
{
    /// <inheritdoc />
    public partial class AddBlogTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Blogs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            // Existing posts used the short description as their title; keep showing that.
            migrationBuilder.Sql(
                "UPDATE \"Blogs\" SET \"Title\" = LEFT(\"ShortDescription\", 200) WHERE \"Title\" = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "Blogs");
        }
    }
}
