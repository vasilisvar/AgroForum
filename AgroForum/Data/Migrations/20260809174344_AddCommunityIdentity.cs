using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgroForum.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "AspNetUsers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FarmingInterests",
                table: "AspNetUsers",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [AspNetUsers]
                SET [DisplayName] =
                    CASE
                        WHEN LTRIM(RTRIM(CONCAT([FirstName], ' ', [LastName]))) <> ''
                            THEN LEFT(LTRIM(RTRIM(CONCAT([FirstName], ' ', [LastName]))), 50)
                        ELSE CONCAT('Farmer-', UPPER(LEFT(REPLACE([Id], '-', ''), 6)))
                    END
                WHERE [DisplayName] IS NULL OR LTRIM(RTRIM([DisplayName])) = '';
                """);

            migrationBuilder.Sql(
                """
                INSERT INTO [AspNetUserRoles] ([UserId], [RoleId])
                SELECT users.[Id], roles.[Id]
                FROM [AspNetUsers] AS users
                CROSS JOIN [AspNetRoles] AS roles
                WHERE roles.[NormalizedName] = 'FARMER'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM [AspNetUserRoles] AS existing
                      WHERE existing.[UserId] = users.[Id]
                        AND existing.[RoleId] = roles.[Id]
                  );
                """);

            migrationBuilder.CreateTable(
                name: "UserNotifications",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ForumPostId = table.Column<int>(type: "int", nullable: true),
                    ForumCommentId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserNotifications_AspNetUsers_ActorId",
                        column: x => x.ActorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserNotifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserNotifications_ForumComments_ForumCommentId",
                        column: x => x.ForumCommentId,
                        principalTable: "ForumComments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserNotifications_ForumPosts_ForumPostId",
                        column: x => x.ForumPostId,
                        principalTable: "ForumPosts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_ActorId",
                table: "UserNotifications",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_ForumCommentId",
                table: "UserNotifications",
                column: "ForumCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_ForumPostId",
                table: "UserNotifications",
                column: "ForumPostId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_Type_ActorId_ForumPostId",
                table: "UserNotifications",
                columns: new[] { "Type", "ActorId", "ForumPostId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_ReadAt_CreatedAt",
                table: "UserNotifications",
                columns: new[] { "UserId", "ReadAt", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FarmingInterests",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "AspNetUsers");
        }
    }
}
