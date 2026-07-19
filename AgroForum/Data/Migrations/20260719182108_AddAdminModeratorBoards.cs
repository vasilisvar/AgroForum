using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroForum.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminModeratorBoards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "ForumReports",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedToId",
                table: "ForumReports",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ForumReports",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "ForumPosts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE [ForumReports]
                SET [Status] = CASE [Status]
                    WHEN 'Pending' THEN 'Open'
                    WHEN 'Accepted' THEN 'Resolved'
                    WHEN 'Rejected' THEN 'Dismissed'
                    ELSE [Status]
                END
                WHERE [Status] IN ('Pending', 'Accepted', 'Rejected');
                """);

            migrationBuilder.CreateTable(
                name: "ModerationActions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TargetId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TargetDisplay = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ForumReportId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModerationActions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModerationActions_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForumReports_AssignedToId",
                table: "ForumReports",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_ModerationActions_ActorId_CreatedAt",
                table: "ModerationActions",
                columns: new[] { "ActorId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ModerationActions_ForumReportId",
                table: "ModerationActions",
                column: "ForumReportId");

            migrationBuilder.AddForeignKey(
                name: "FK_ForumReports_AspNetUsers_AssignedToId",
                table: "ForumReports",
                column: "AssignedToId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [ForumReports]
                SET [Status] = CASE [Status]
                    WHEN 'Open' THEN 'Pending'
                    WHEN 'InReview' THEN 'Pending'
                    WHEN 'Dismissed' THEN 'Rejected'
                    ELSE [Status]
                END
                WHERE [Status] IN ('Open', 'InReview', 'Dismissed');
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_ForumReports_AspNetUsers_AssignedToId",
                table: "ForumReports");

            migrationBuilder.DropTable(
                name: "ModerationActions");

            migrationBuilder.DropIndex(
                name: "IX_ForumReports_AssignedToId",
                table: "ForumReports");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "ForumReports");

            migrationBuilder.DropColumn(
                name: "AssignedToId",
                table: "ForumReports");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ForumReports");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "ForumPosts");
        }
    }
}
