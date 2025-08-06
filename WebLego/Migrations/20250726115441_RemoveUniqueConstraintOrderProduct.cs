using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLego.Migrations
{
    public partial class RemoveUniqueConstraintOrderProduct : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_CommunityPosts_Order_Product",
                table: "CommunityPosts");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "UQ_CommunityPosts_Order_Product",
                table: "CommunityPosts",
                columns: new[] { "OrderId", "ProductId" },
                unique: true,
                filter: "[OrderId] IS NOT NULL AND [ProductId] IS NOT NULL");
        }
    }
}