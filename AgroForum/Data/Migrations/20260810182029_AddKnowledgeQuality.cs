using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroForum.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeQuality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcceptedCommentId",
                table: "ForumPosts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourceTitle",
                table: "ForumPosts",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourceUrl",
                table: "ForumPosts",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForumPosts_AcceptedCommentId",
                table: "ForumPosts",
                column: "AcceptedCommentId");

            migrationBuilder.AddForeignKey(
                name: "FK_ForumPosts_ForumComments_AcceptedCommentId",
                table: "ForumPosts",
                column: "AcceptedCommentId",
                principalTable: "ForumComments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ForumPosts_ForumComments_AcceptedCommentId",
                table: "ForumPosts");

            migrationBuilder.DropIndex(
                name: "IX_ForumPosts_AcceptedCommentId",
                table: "ForumPosts");

            migrationBuilder.DropColumn(
                name: "AcceptedCommentId",
                table: "ForumPosts");

            migrationBuilder.DropColumn(
                name: "ResourceTitle",
                table: "ForumPosts");

            migrationBuilder.DropColumn(
                name: "ResourceUrl",
                table: "ForumPosts");
        }
    }
}
