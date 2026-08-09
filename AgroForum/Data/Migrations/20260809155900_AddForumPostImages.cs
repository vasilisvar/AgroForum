using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroForum.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddForumPostImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "ForumPosts",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "ForumPosts");
        }
    }
}
