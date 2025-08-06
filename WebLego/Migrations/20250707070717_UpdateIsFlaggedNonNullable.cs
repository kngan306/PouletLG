using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebLego.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIsFlaggedNonNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Cart__ProductID__60A75C0F",
                table: "Cart");

            migrationBuilder.DropForeignKey(
                name: "FK__Cart__UserID__5FB337D6",
                table: "Cart");

            migrationBuilder.DropForeignKey(
                name: "FK__Categorie__Creat__47DBAE45",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK__CustomerP__Custo__440B1D61",
                table: "CustomerProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK__OrderDeta__Order__72C60C4A",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK__OrderDeta__Produ__73BA3083",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK__ProductIm__Produ__5629CD9C",
                table: "ProductImages");

            migrationBuilder.DropForeignKey(
                name: "FK__ProductRe__Order__7A672E12",
                table: "ProductReturns");

            migrationBuilder.DropForeignKey(
                name: "FK__ProductRe__UserI__7B5B524B",
                table: "ProductReturns");

            migrationBuilder.DropForeignKey(
                name: "FK__ProductRe__Order__08B54D69",
                table: "ProductReviews");

            migrationBuilder.DropForeignKey(
                name: "FK__ProductRe__Produ__07C12930",
                table: "ProductReviews");

            migrationBuilder.DropForeignKey(
                name: "FK__ProductRe__UserI__06CD04F7",
                table: "ProductReviews");

            migrationBuilder.DropForeignKey(
                name: "FK__Products__Catego__5165187F",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK__Products__Create__52593CB8",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK__ReturnDet__Produ__7F2BE32F",
                table: "ReturnDetails");

            migrationBuilder.DropForeignKey(
                name: "FK__ReturnDet__Repla__00200768",
                table: "ReturnDetails");

            migrationBuilder.DropForeignKey(
                name: "FK__ReturnDet__Retur__7E37BEF6",
                table: "ReturnDetails");

            migrationBuilder.DropForeignKey(
                name: "FK__UserAddre__UserI__66603565",
                table: "UserAddresses");

            migrationBuilder.DropPrimaryKey(
                name: "PK__UserAddr__091C2AFBB894427E",
                table: "UserAddresses");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Roles__8AFACE1AA1E1042E",
                table: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK__ReturnDe__8B89C98A848FDBE0",
                table: "ReturnDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Products__B40CC6CD7DEF16AD",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK__ProductR__74BC79CEA51C80F4",
                table: "ProductReviews");

            migrationBuilder.DropIndex(
                name: "IX_ProductReviews_UserId",
                table: "ProductReviews");

            migrationBuilder.DropPrimaryKey(
                name: "PK__ProductR__F445E9A875665566",
                table: "ProductReturns");

            migrationBuilder.DropPrimaryKey(
                name: "PK__ProductI__7516F70C6F742BB8",
                table: "ProductImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Orders__C3905BCF2143D88A",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK__OrderDet__D3B9D36CF039E761",
                table: "OrderDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Customer__A4AE64D8326F3E62",
                table: "CustomerProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Categori__19093A0BCE95FF8D",
                table: "Categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Cart__51BCD7976B4968C0",
                table: "Cart");

            migrationBuilder.DropColumn(
                name: "PromotionEndDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "PromotionStartDate",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RewardPoints",
                table: "CustomerProfiles");

            migrationBuilder.RenameIndex(
                name: "UQ__Roles__8A2B61606CDEE7FE",
                table: "Roles",
                newName: "UQ__Roles__8A2B616057421610");

            migrationBuilder.AddColumn<int>(
                name: "PromotionId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUpdated",
                table: "ProductReviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProductReviews",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShipperId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VnpTransactionDate",
                table: "Orders",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VnpTransactionNo",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountCode",
                table: "CustomerProfiles",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK__UserAddr__091C2AFB82FBD5D5",
                table: "UserAddresses",
                column: "AddressId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Roles__8AFACE1ABDA23BAC",
                table: "Roles",
                column: "RoleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__ReturnDe__8B89C98A37BAE035",
                table: "ReturnDetails",
                column: "ReturnDetailId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Products__B40CC6CDAD639000",
                table: "Products",
                column: "ProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__ProductR__74BC79CE394E2AD2",
                table: "ProductReviews",
                column: "ReviewId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__ProductR__F445E9A8A07C06CE",
                table: "ProductReturns",
                column: "ReturnId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__ProductI__7516F70CAC4425F1",
                table: "ProductImages",
                column: "ImageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Orders__C3905BCFB6D3C544",
                table: "Orders",
                column: "OrderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__OrderDet__D3B9D36C0598E863",
                table: "OrderDetails",
                column: "OrderDetailId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Customer__A4AE64D80062C105",
                table: "CustomerProfiles",
                column: "CustomerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Categori__19093A0BC0C9FB4E",
                table: "Categories",
                column: "CategoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Cart__51BCD7975C1840AB",
                table: "Cart",
                column: "CartID");

            migrationBuilder.CreateTable(
                name: "HomeBanners",
                columns: table => new
                {
                    BannerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: true, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__HomeBann__32E86AD10A464E43", x => x.BannerId);
                    table.ForeignKey(
                        name: "FK__HomeBanne__Creat__123EB7A3",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Promotions",
                columns: table => new
                {
                    PromotionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromotionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Promotio__52C42FCFEE8EC12F", x => x.PromotionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_PromotionId",
                table: "Products",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "UQ_ProductReviews_User_Product_Order",
                table: "ProductReviews",
                columns: new[] { "UserId", "ProductId", "OrderId" },
                unique: true,
                filter: "[OrderId] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ProductReviews_ReviewStatus",
                table: "ProductReviews",
                sql: "ReviewStatus IN (N'Chưa phản hồi', N'Đã phản hồi', N'Bị ẩn')");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShipperId",
                table: "Orders",
                column: "ShipperId");

            migrationBuilder.CreateIndex(
                name: "IX_HomeBanners_CreatedBy",
                table: "HomeBanners",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK__Cart__ProductID__6383C8BA",
                table: "Cart",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK__Cart__UserID__628FA481",
                table: "Cart",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__Categorie__Creat__45F365D3",
                table: "Categories",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__CustomerP__Custo__4222D4EF",
                table: "CustomerProfiles",
                column: "CustomerId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__OrderDeta__Order__778AC167",
                table: "OrderDetails",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK__OrderDeta__Produ__787EE5A0",
                table: "OrderDetails",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Shipper",
                table: "Orders",
                column: "ShipperId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__ProductIm__Produ__59063A47",
                table: "ProductImages",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK__ProductRe__Order__7F2BE32F",
                table: "ProductReturns",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK__ProductRe__UserI__00200768",
                table: "ProductReturns",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__ProductRe__Order__0D7A0286",
                table: "ProductReviews",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK__ProductRe__Produ__0C85DE4D",
                table: "ProductReviews",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK__ProductRe__UserI__0B91BA14",
                table: "ProductReviews",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Promotions",
                table: "Products",
                column: "PromotionId",
                principalTable: "Promotions",
                principalColumn: "PromotionId");

            migrationBuilder.AddForeignKey(
                name: "FK__Products__Catego__4F7CD00D",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK__Products__Create__5070F446",
                table: "Products",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__ReturnDet__Produ__03F0984C",
                table: "ReturnDetails",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK__ReturnDet__Repla__04E4BC85",
                table: "ReturnDetails",
                column: "ReplacementProductId",
                principalTable: "Products",
                principalColumn: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK__ReturnDet__Retur__02FC7413",
                table: "ReturnDetails",
                column: "ReturnId",
                principalTable: "ProductReturns",
                principalColumn: "ReturnId");

            migrationBuilder.AddForeignKey(
                name: "FK__UserAddre__UserI__693CA210",
                table: "UserAddresses",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Cart__ProductID__6383C8BA",
                table: "Cart");

            migrationBuilder.DropForeignKey(
                name: "FK__Cart__UserID__628FA481",
                table: "Cart");

            migrationBuilder.DropForeignKey(
                name: "FK__Categorie__Creat__45F365D3",
                table: "Categories");

            migrationBuilder.DropForeignKey(
                name: "FK__CustomerP__Custo__4222D4EF",
                table: "CustomerProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK__OrderDeta__Order__778AC167",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK__OrderDeta__Produ__787EE5A0",
                table: "OrderDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Shipper",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK__ProductIm__Produ__59063A47",
                table: "ProductImages");

            migrationBuilder.DropForeignKey(
                name: "FK__ProductRe__Order__7F2BE32F",
                table: "ProductReturns");

            migrationBuilder.DropForeignKey(
                name: "FK__ProductRe__UserI__00200768",
                table: "ProductReturns");

            migrationBuilder.DropForeignKey(
                name: "FK__ProductRe__Order__0D7A0286",
                table: "ProductReviews");

            migrationBuilder.DropForeignKey(
                name: "FK__ProductRe__Produ__0C85DE4D",
                table: "ProductReviews");

            migrationBuilder.DropForeignKey(
                name: "FK__ProductRe__UserI__0B91BA14",
                table: "ProductReviews");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Promotions",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK__Products__Catego__4F7CD00D",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK__Products__Create__5070F446",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK__ReturnDet__Produ__03F0984C",
                table: "ReturnDetails");

            migrationBuilder.DropForeignKey(
                name: "FK__ReturnDet__Repla__04E4BC85",
                table: "ReturnDetails");

            migrationBuilder.DropForeignKey(
                name: "FK__ReturnDet__Retur__02FC7413",
                table: "ReturnDetails");

            migrationBuilder.DropForeignKey(
                name: "FK__UserAddre__UserI__693CA210",
                table: "UserAddresses");

            migrationBuilder.DropTable(
                name: "HomeBanners");

            migrationBuilder.DropTable(
                name: "Promotions");

            migrationBuilder.DropPrimaryKey(
                name: "PK__UserAddr__091C2AFB82FBD5D5",
                table: "UserAddresses");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Roles__8AFACE1ABDA23BAC",
                table: "Roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK__ReturnDe__8B89C98A37BAE035",
                table: "ReturnDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Products__B40CC6CDAD639000",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_PromotionId",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK__ProductR__74BC79CE394E2AD2",
                table: "ProductReviews");

            migrationBuilder.DropIndex(
                name: "UQ_ProductReviews_User_Product_Order",
                table: "ProductReviews");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ProductReviews_ReviewStatus",
                table: "ProductReviews");

            migrationBuilder.DropPrimaryKey(
                name: "PK__ProductR__F445E9A8A07C06CE",
                table: "ProductReturns");

            migrationBuilder.DropPrimaryKey(
                name: "PK__ProductI__7516F70CAC4425F1",
                table: "ProductImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Orders__C3905BCFB6D3C544",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ShipperId",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK__OrderDet__D3B9D36C0598E863",
                table: "OrderDetails");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Customer__A4AE64D80062C105",
                table: "CustomerProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Categori__19093A0BC0C9FB4E",
                table: "Categories");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Cart__51BCD7975C1840AB",
                table: "Cart");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsUpdated",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "ShipperId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VnpTransactionDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "VnpTransactionNo",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountCode",
                table: "CustomerProfiles");

            migrationBuilder.RenameIndex(
                name: "UQ__Roles__8A2B616057421610",
                table: "Roles",
                newName: "UQ__Roles__8A2B61606CDEE7FE");

            migrationBuilder.AddColumn<DateTime>(
                name: "PromotionEndDate",
                table: "Products",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PromotionStartDate",
                table: "Products",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RewardPoints",
                table: "CustomerProfiles",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK__UserAddr__091C2AFBB894427E",
                table: "UserAddresses",
                column: "AddressId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Roles__8AFACE1AA1E1042E",
                table: "Roles",
                column: "RoleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__ReturnDe__8B89C98A848FDBE0",
                table: "ReturnDetails",
                column: "ReturnDetailId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Products__B40CC6CD7DEF16AD",
                table: "Products",
                column: "ProductId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__ProductR__74BC79CEA51C80F4",
                table: "ProductReviews",
                column: "ReviewId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__ProductR__F445E9A875665566",
                table: "ProductReturns",
                column: "ReturnId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__ProductI__7516F70C6F742BB8",
                table: "ProductImages",
                column: "ImageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Orders__C3905BCF2143D88A",
                table: "Orders",
                column: "OrderId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__OrderDet__D3B9D36CF039E761",
                table: "OrderDetails",
                column: "OrderDetailId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Customer__A4AE64D8326F3E62",
                table: "CustomerProfiles",
                column: "CustomerId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Categori__19093A0BCE95FF8D",
                table: "Categories",
                column: "CategoryId");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Cart__51BCD7976B4968C0",
                table: "Cart",
                column: "CartID");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_UserId",
                table: "ProductReviews",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__Cart__ProductID__60A75C0F",
                table: "Cart",
                column: "ProductID",
                principalTable: "Products",
                principalColumn: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK__Cart__UserID__5FB337D6",
                table: "Cart",
                column: "UserID",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__Categorie__Creat__47DBAE45",
                table: "Categories",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__CustomerP__Custo__440B1D61",
                table: "CustomerProfiles",
                column: "CustomerId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__OrderDeta__Order__72C60C4A",
                table: "OrderDetails",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK__OrderDeta__Produ__73BA3083",
                table: "OrderDetails",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK__ProductIm__Produ__5629CD9C",
                table: "ProductImages",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK__ProductRe__Order__7A672E12",
                table: "ProductReturns",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK__ProductRe__UserI__7B5B524B",
                table: "ProductReturns",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__ProductRe__Order__08B54D69",
                table: "ProductReviews",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK__ProductRe__Produ__07C12930",
                table: "ProductReviews",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK__ProductRe__UserI__06CD04F7",
                table: "ProductReviews",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__Products__Catego__5165187F",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK__Products__Create__52593CB8",
                table: "Products",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK__ReturnDet__Produ__7F2BE32F",
                table: "ReturnDetails",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK__ReturnDet__Repla__00200768",
                table: "ReturnDetails",
                column: "ReplacementProductId",
                principalTable: "Products",
                principalColumn: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK__ReturnDet__Retur__7E37BEF6",
                table: "ReturnDetails",
                column: "ReturnId",
                principalTable: "ProductReturns",
                principalColumn: "ReturnId");

            migrationBuilder.AddForeignKey(
                name: "FK__UserAddre__UserI__66603565",
                table: "UserAddresses",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }
    }
}
