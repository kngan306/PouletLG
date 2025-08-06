using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebLego.Migrations
{
    /// <inheritdoc />
    public partial class MakeOrderIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AboutUsSections",
                columns: table => new
                {
                    SectionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AboutUsSections__SectionId", x => x.SectionId);
                    table.ForeignKey(
                        name: "FK__AboutUsSections__CreatedBy__Users",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ContactInformations",
                columns: table => new
                {
                    ContactId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ContactInformations__ContactId", x => x.ContactId);
                    table.ForeignKey(
                        name: "FK__ContactInformations__CreatedBy__Users",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Contests",
                columns: table => new
                {
                    ContestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Contests__ContestId", x => x.ContestId);
                    table.ForeignKey(
                        name: "FK__Contests__CreatedBy__Users",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "CommunityPosts",
                columns: table => new
                {
                    PostId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    ContestId = table.Column<int>(type: "int", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    CommentCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsFlagged = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CommunityPosts__PostId", x => x.PostId);
                    table.ForeignKey(
                        name: "FK__CommunityPosts__ContestId__Contests",
                        column: x => x.ContestId,
                        principalTable: "Contests",
                        principalColumn: "ContestId");
                    table.ForeignKey(
                        name: "FK__CommunityPosts__OrderId__Orders",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId");
                    table.ForeignKey(
                        name: "FK__CommunityPosts__ProductId__Products",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId");
                    table.ForeignKey(
                        name: "FK__CommunityPosts__UserId__Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ContestWinners",
                columns: table => new
                {
                    WinnerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContestId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RewardProductId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    WonAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Chưa gửi")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ContestWinners__WinnerId", x => x.WinnerId);
                    table.ForeignKey(
                        name: "FK__ContestWinners__ContestId__Contests",
                        column: x => x.ContestId,
                        principalTable: "Contests",
                        principalColumn: "ContestId");
                    table.ForeignKey(
                        name: "FK__ContestWinners__OrderId__Orders",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK__ContestWinners__RewardProductId__Products",
                        column: x => x.RewardProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId");
                    table.ForeignKey(
                        name: "FK__ContestWinners__UserId__Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "CommunityComments",
                columns: table => new
                {
                    CommentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    IsFlagged = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__CommunityComments__CommentId", x => x.CommentId);
                    table.ForeignKey(
                        name: "FK__CommunityComments__PostId__CommunityPosts",
                        column: x => x.PostId,
                        principalTable: "CommunityPosts",
                        principalColumn: "PostId");
                    table.ForeignKey(
                        name: "FK__CommunityComments__UserId__Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "ContestVotes",
                columns: table => new
                {
                    VoteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ContestVotes__VoteId", x => x.VoteId);
                    table.ForeignKey(
                        name: "FK__ContestVotes__PostId__CommunityPosts",
                        column: x => x.PostId,
                        principalTable: "CommunityPosts",
                        principalColumn: "PostId");
                    table.ForeignKey(
                        name: "FK__ContestVotes__UserId__Users",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AboutUsSections_CreatedBy",
                table: "AboutUsSections",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityComments_PostId",
                table: "CommunityComments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityComments_UserId",
                table: "CommunityComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_ContestId",
                table: "CommunityPosts",
                column: "ContestId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_OrderId",
                table: "CommunityPosts",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_ProductId",
                table: "CommunityPosts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityPosts_UserId",
                table: "CommunityPosts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactInformations_CreatedBy",
                table: "ContactInformations",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Contests_CreatedBy",
                table: "Contests",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ContestVotes_UserId",
                table: "ContestVotes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ_ContestVotes_Post_User",
                table: "ContestVotes",
                columns: new[] { "PostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContestWinners_OrderId",
                table: "ContestWinners",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ContestWinners_RewardProductId",
                table: "ContestWinners",
                column: "RewardProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ContestWinners_UserId",
                table: "ContestWinners",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ_ContestWinners_Contest",
                table: "ContestWinners",
                column: "ContestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AboutUsSections");

            migrationBuilder.DropTable(
                name: "CommunityComments");

            migrationBuilder.DropTable(
                name: "ContactInformations");

            migrationBuilder.DropTable(
                name: "ContestVotes");

            migrationBuilder.DropTable(
                name: "ContestWinners");

            migrationBuilder.DropTable(
                name: "CommunityPosts");

            migrationBuilder.DropTable(
                name: "Contests");
        }
    }
}
