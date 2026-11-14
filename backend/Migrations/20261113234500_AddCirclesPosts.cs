using Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace socialmediaapi.Migrations
{
    [DbContext(typeof(Db))]
    [Migration("20261113234500_AddCirclesPosts")]
    public partial class AddCirclesPosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Circles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsPrivate = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Circles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Circles_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CircleId = table.Column<long>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Posts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Posts_Circles_CircleId",
                        column: x => x.CircleId,
                        principalTable: "Circles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserCircleRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CircleId = table.Column<long>(type: "INTEGER", nullable: false),
                    RoleId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCircleRoles", x => new { x.UserId, x.CircleId });
                    table.ForeignKey(
                        name: "FK_UserCircleRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCircleRoles_Circles_CircleId",
                        column: x => x.CircleId,
                        principalTable: "Circles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserCircleRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO "Roles" ("Id", "Name")
                VALUES (1, 'Owner'), (2, 'Moderator'), (3, 'Member');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Circles_CreatedByUserId",
                table: "Circles",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_CircleId",
                table: "Posts",
                column: "CircleId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_UserId",
                table: "Posts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCircleRoles_CircleId",
                table: "UserCircleRoles",
                column: "CircleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserCircleRoles_RoleId",
                table: "UserCircleRoles",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Posts");
            migrationBuilder.DropTable(name: "UserCircleRoles");
            migrationBuilder.DropTable(name: "Circles");
            migrationBuilder.DropTable(name: "Roles");
        }
    }
}
