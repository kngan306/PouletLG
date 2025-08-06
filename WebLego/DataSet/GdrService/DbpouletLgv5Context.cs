using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace WebLego.DataSet.GdrService;

public partial class DbpouletLgv5Context : DbContext
{
    public DbpouletLgv5Context()
    {
    }

    public DbpouletLgv5Context(DbContextOptions<DbpouletLgv5Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Cart> Carts { get; set; }
    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<CustomerProfile> CustomerProfiles { get; set; }
    public virtual DbSet<HomeBanner> HomeBanners { get; set; }
    public virtual DbSet<Order> Orders { get; set; }
    public virtual DbSet<OrderDetail> OrderDetails { get; set; }
    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<ProductImage> ProductImages { get; set; }
    public virtual DbSet<ProductReturn> ProductReturns { get; set; }
    public virtual DbSet<ProductReview> ProductReviews { get; set; }
    public virtual DbSet<Promotion> Promotions { get; set; }
    public virtual DbSet<ReturnDetail> ReturnDetails { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<UserAddress> UserAddresses { get; set; }
    public virtual DbSet<Favorite> Favorites { get; set; }
    public virtual DbSet<ContactInformation> ContactInformations { get; set; }
    public virtual DbSet<AboutUsSection> AboutUsSections { get; set; }
    public virtual DbSet<CommunityPost> CommunityPosts { get; set; }
    public virtual DbSet<CommunityComment> CommunityComments { get; set; }
    public virtual DbSet<ContestVote> ContestVotes { get; set; }
    public virtual DbSet<Contest> Contests { get; set; }
    public virtual DbSet<ContestWinner> ContestWinners { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=DUYUYN;Database=DBPouletLGv5;User ID=sa;Password=sa;TrustServerCertificate=True");
    }
    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{
    //    optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=DBPouletLGv5;Trusted_Connection=True;TrustServerCertificate=True");
    //}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Cấu hình bảng Cart
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.CartId).HasName("PK__Cart__51BCD7975C1840AB");
            entity.ToTable("Cart");
            entity.HasIndex(e => new { e.UserId, e.ProductId }, "UQ_Cart_User_Product").IsUnique();
            entity.Property(e => e.CartId).HasColumnName("CartID");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.Quantity).HasDefaultValue(1);
            entity.Property(e => e.UserId).HasColumnName("UserID");
            entity.HasOne(d => d.Product).WithMany(p => p.Carts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Cart__ProductID__6383C8BA");
            entity.HasOne(d => d.User).WithMany(p => p.Carts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Cart__UserID__628FA481");
        });

        // Cấu hình bảng Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PK__Categori__19093A0BC0C9FB4E");

            entity.Property(e => e.CategoryName).HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasColumnName("CategoryDescription")
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.ImagePath)
                .HasColumnName("ImageUrl")
                .HasMaxLength(255);

            entity.Property(e => e.BackgroundColor)
                .HasColumnName("BackgroundColor")
                .HasMaxLength(20);

            entity.Property(e => e.ButtonColor)
                .HasColumnName("ButtonColor")
                .HasMaxLength(20);

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.CreatedByNavigation)
                .WithMany(p => p.Categories)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Categorie__Creat__45F365D3");
        });

        // Cấu hình bảng CustomerProfile
        modelBuilder.Entity<CustomerProfile>(entity =>
        {
            entity.HasKey(e => e.CustomerId).HasName("PK__Customer__A4AE64D80062C105");
            entity.ToTable(tb => tb.HasTrigger("trg_ValidateCustomerRole"));
            entity.Property(e => e.CustomerId).ValueGeneratedNever();
            entity.Property(e => e.CustomerRank).HasMaxLength(20);
            entity.Property(e => e.DiscountCode).HasMaxLength(20);
            entity.HasOne(d => d.Customer).WithOne(p => p.CustomerProfile)
                .HasForeignKey<CustomerProfile>(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CustomerP__Custo__4222D4EF");
        });

        // Cấu hình bảng HomeBanner
        modelBuilder.Entity<HomeBanner>(entity =>
        {
            entity.HasKey(e => e.BannerId).HasName("PK__HomeBann__32E86AD10A464E43");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.HomeBanners)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HomeBanne__Creat__123EB7A3");
        });

        // Cấu hình bảng Order
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__C3905BCFB6D3C544");
            entity.ToTable(tb =>
            {
                tb.HasTrigger("trg_UpdateRankAndDiscount");
                tb.HasTrigger("trg_UpdateStock_OnOrderCompleted");
            });
            entity.Property(e => e.Discount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.OrderDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Chờ xác nhận");
            entity.Property(e => e.PaymentMethod).HasMaxLength(50);
            entity.Property(e => e.PaymentStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Chưa thanh toán");
            entity.Property(e => e.ShippingFee)
                .HasDefaultValue(15000m)
                .HasColumnType("decimal(10, 2)");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.VnpTransactionDate).HasColumnType("datetime");
            entity.Property(e => e.VnpTransactionNo).HasMaxLength(50);
            entity.HasOne(d => d.Address).WithMany(p => p.Orders)
                .HasForeignKey(d => d.AddressId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Addresses");
            entity.HasOne(d => d.Shipper).WithMany(p => p.OrderShippers)
                .HasForeignKey(d => d.ShipperId)
                .HasConstraintName("FK_Orders_Shipper");
            entity.HasOne(d => d.User).WithMany(p => p.OrderUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Orders_Users");
        });

        // Cấu hình bảng OrderDetail
        modelBuilder.Entity<OrderDetail>(entity =>
        {
            entity.HasKey(e => e.OrderDetailId).HasName("PK__OrderDet__D3B9D36C0598E863");
            entity.Property(e => e.TotalPrice)
                .HasComputedColumnSql("([Quantity]*[UnitPrice])", true)
                .HasColumnType("decimal(21, 2)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(10, 2)");
            entity.HasOne(d => d.Order).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderDeta__Order__778AC167");
            entity.HasOne(d => d.Product).WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__OrderDeta__Produ__787EE5A0");
        });

        // Cấu hình bảng Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Products__B40CC6CDAD639000");
            entity.Property(e => e.AgeRange).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DiscountPrice).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.IsFeatured).HasDefaultValue(false);
            entity.Property(e => e.Price).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.ProductName).HasMaxLength(100);
            entity.Property(e => e.ProductStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Hoạt động");
            entity.Property(e => e.Rating)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(2, 1)");
            entity.Property(e => e.Sold).HasDefaultValue(0);
            entity.Property(e => e.StockQuantity).HasDefaultValue(0);
            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("FK__Products__Catego__4F7CD00D");
            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ProductsNavigation)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Products__Create__5070F446");
            entity.HasOne(d => d.Promotion).WithMany(p => p.Products)
                .HasForeignKey(d => d.PromotionId)
                .HasConstraintName("FK_Products_Promotions");
        });

        // Cấu hình bảng ProductImage
        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.ImageId).HasName("PK__ProductI__7516F70CAC4425F1");
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.IsMain).HasDefaultValue(false);
            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__ProductIm__Produ__59063A47");
        });

        // Cấu hình bảng ProductReturn
        modelBuilder.Entity<ProductReturn>(entity =>
        {
            entity.HasKey(e => e.ReturnId).HasName("PK__ProductR__F445E9A8A07C06CE");
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.Note).HasMaxLength(255);
            entity.Property(e => e.RequestType).HasMaxLength(50);
            entity.Property(e => e.RequestedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ReturnStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Đang xử lý");
            entity.Property(e => e.TotalRefundAmount)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)");
            entity.HasOne(d => d.Order).WithMany(p => p.ProductReturns)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProductRe__Order__7F2BE32F");
            entity.HasOne(d => d.User).WithMany(p => p.ProductReturns)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProductRe__UserI__00200768");
        });

        // Cấu hình bảng ProductReview
        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("PK__ProductR__74BC79CE394E2AD2");
            entity.ToTable(tb => tb.HasTrigger("trg_UpdateProductRating"));
            entity.HasIndex(e => new { e.UserId, e.ProductId, e.OrderId }, "UQ_ProductReviews_User_Product_Order").IsUnique();
            entity.Property(e => e.AdminReplyAt).HasColumnType("datetime");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.IsFlagged).HasDefaultValue(false);
            entity.Property(e => e.ReviewStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Chưa phản hồi");
            entity.Property(e => e.IsUpdated).HasDefaultValue(false);
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime");
            entity.HasOne(d => d.Order).WithMany(p => p.ProductReviews)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__ProductRe__Order__0D7A0286");
            entity.HasOne(d => d.Product).WithMany(p => p.ProductReviews)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProductRe__Produ__0C85DE4D");
            entity.HasOne(d => d.User).WithMany(p => p.ProductReviews)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ProductRe__UserI__0B91BA14");
            entity.HasCheckConstraint("CK_ProductReviews_ReviewStatus", "ReviewStatus IN (N'Chưa phản hồi', N'Đã phản hồi', N'Bị ẩn')");
        });

        // Cấu hình bảng Promotion
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.PromotionId).HasName("PK__Promotio__52C42FCFEE8EC12F");
            entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.EndDate).HasColumnType("datetime");
            entity.Property(e => e.PromotionName).HasMaxLength(100);
            entity.Property(e => e.StartDate).HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50);
        });

        // Cấu hình bảng ReturnDetail
        modelBuilder.Entity<ReturnDetail>(entity =>
        {
            entity.HasKey(e => e.ReturnDetailId).HasName("PK__ReturnDe__8B89C98A37BAE035");
            entity.HasOne(d => d.Product).WithMany(p => p.ReturnDetailProducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ReturnDet__Produ__03F0984C");
            entity.HasOne(d => d.ReplacementProduct).WithMany(p => p.ReturnDetailReplacementProducts)
                .HasForeignKey(d => d.ReplacementProductId)
                .HasConstraintName("FK__ReturnDet__Repla__04E4BC85");
            entity.HasOne(d => d.Return).WithMany(p => p.ReturnDetails)
                .HasForeignKey(d => d.ReturnId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ReturnDet__Retur__02FC7413");
        });

        // Cấu hình bảng Role
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1ABDA23BAC");
            entity.HasIndex(e => e.RoleName, "UQ__Roles__8A2B616057421610").IsUnique();
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        // Cấu hình bảng User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C9289ACB7");
            entity.HasIndex(e => e.Email, "UQ__Users__A9D105344E20A12A").IsUnique();
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DateOfBirth).HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Gender).HasMaxLength(10);
            entity.Property(e => e.Phone)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.UserPassword).HasMaxLength(100);
            entity.Property(e => e.UserStatus)
                .HasMaxLength(50)
                .HasDefaultValue("Hoạt động");
            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__RoleId__3E52440B");
        });

        // Cấu hình bảng UserAddress
        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.HasKey(e => e.AddressId).HasName("PK__UserAddr__091C2AFB82FBD5D5");
            entity.Property(e => e.AddressType).HasMaxLength(50);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.District).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.IsDefault).HasDefaultValue(false);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.Province).HasMaxLength(100);
            entity.Property(e => e.SpecificAddress).HasMaxLength(255);
            entity.Property(e => e.Ward).HasMaxLength(100);
            entity.HasOne(d => d.User).WithMany(p => p.UserAddresses)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserAddre__UserI__693CA210");
        });

        // Cấu hình bảng Favorite
        modelBuilder.Entity<Favorite>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.ProductId });
            entity.HasOne(d => d.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Favorites_Users");
            entity.HasOne(d => d.Product)
                .WithMany(p => p.Favorites)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Favorites_Products");
            entity.ToTable("Favorites");
        });

        // Cấu hình bảng ContactInformation
        modelBuilder.Entity<ContactInformation>(entity =>
        {
            entity.HasKey(e => e.ContactId).HasName("PK__ContactInformations__ContactId");
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Latitude).HasColumnType("decimal(9,6)");
            entity.Property(e => e.Longitude).HasColumnType("decimal(9,6)");
            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.ContactInformations)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ContactInformations__CreatedBy__Users");
        });

        // Cấu hình bảng AboutUsSection
        modelBuilder.Entity<AboutUsSection>(entity =>
        {
            entity.HasKey(e => e.SectionId).HasName("PK__AboutUsSections__SectionId");
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnType("nvarchar(max)");
            entity.Property(e => e.ImageUrl).HasMaxLength(255);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.DisplayOrder).HasColumnType("int");
            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.AboutUsSections)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AboutUsSections__CreatedBy__Users");
        });

        // Cấu hình bảng CommunityPost
        modelBuilder.Entity<CommunityPost>(entity =>
        {
            entity.HasKey(e => e.PostId).HasName("PK__CommunityPosts__PostId");
            entity.Property(e => e.ImageUrl).HasMaxLength(255); // Bỏ .IsRequired()
            entity.Property(e => e.Description).HasColumnType("nvarchar(max)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
            entity.Property(e => e.CommentCount).HasDefaultValue(0);
            entity.Property(e => e.ContestId).IsRequired(false);
            entity.Property(e => e.OrderId).IsRequired(false);
            entity.Property(e => e.ProductId).IsRequired(false);
            entity.Property(e => e.IsFlagged).HasDefaultValue(false);
            entity.HasOne(d => d.User)
                .WithMany(u => u.CommunityPosts)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CommunityPosts__UserId__Users");
            entity.HasOne(d => d.Order)
                .WithMany(o => o.CommunityPosts)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CommunityPosts__OrderId__Orders");
            entity.HasOne(d => d.Product)
                .WithMany(p => p.CommunityPosts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CommunityPosts__ProductId__Products");
            entity.HasOne(d => d.Contest)
                .WithMany(c => c.CommunityPosts)
                .HasForeignKey(d => d.ContestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CommunityPosts__ContestId__Contests");
        });

        // Cấu hình bảng CommunityComment
        modelBuilder.Entity<CommunityComment>(entity =>
        {
            entity.HasKey(e => e.CommentId).HasName("PK__CommunityComments__CommentId");
            entity.Property(e => e.CommentText).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
            entity.Property(e => e.IsFlagged).HasDefaultValue(false);
            entity.HasOne(d => d.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CommunityComments__PostId__CommunityPosts");
            entity.HasOne(d => d.User)
                .WithMany(u => u.CommunityComments)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__CommunityComments__UserId__Users");
        });

        // Cấu hình bảng ContestVote
        modelBuilder.Entity<ContestVote>(entity =>
        {
            entity.HasKey(e => e.VoteId).HasName("PK__ContestVotes__VoteId");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
            entity.HasOne(d => d.Post)
                .WithMany(p => p.ContestVotes)
                .HasForeignKey(d => d.PostId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ContestVotes__PostId__CommunityPosts");
            entity.HasOne(d => d.User)
                .WithMany(u => u.ContestVotes)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ContestVotes__UserId__Users");
            entity.HasIndex(e => new { e.PostId, e.UserId }, "UQ_ContestVotes_Post_User").IsUnique();
        });

        // Cấu hình bảng Contest
        modelBuilder.Entity<Contest>(entity =>
        {
            entity.HasKey(e => e.ContestId).HasName("PK__Contests__ContestId");
            entity.Property(e => e.Title).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnType("nvarchar(max)");
            entity.Property(e => e.StartDate).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.EndDate).HasColumnType("datetime").IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.ContestStatus).HasMaxLength(50); // Thêm cấu hình cho ContestStatus
            entity.Property(e => e.RewardProductId).IsRequired(false); // Nullable
            entity.Property(e => e.ImageUrl).HasMaxLength(255).IsRequired(false); // Nullable
            entity.HasOne(d => d.CreatedByNavigation)
                .WithMany(u => u.Contests)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Contests__CreatedBy__Users");
            entity.HasOne(d => d.RewardProduct)
                .WithMany()
                .HasForeignKey(d => d.RewardProductId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Contests_RewardProductId");
        });

        // Cấu hình bảng ContestWinner
        modelBuilder.Entity<ContestWinner>(entity =>
        {
            entity.HasKey(e => e.WinnerId).HasName("PK__ContestWinners__WinnerId");
            entity.Property(e => e.WonAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime");
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("Chưa gửi");
            entity.HasOne(d => d.Contest)
                .WithOne() // Mỗi cuộc thi chỉ có một người chiến thắng
                .HasForeignKey<ContestWinner>(d => d.ContestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ContestWinners__ContestId__Contests");
            entity.HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ContestWinners__UserId__Users");
            entity.HasOne(d => d.RewardProduct)
                .WithMany()
                .HasForeignKey(d => d.RewardProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ContestWinners__RewardProductId__Products");
            entity.HasOne(d => d.Order)
                .WithMany()
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__ContestWinners__OrderId__Orders");
            entity.HasIndex(e => e.ContestId, "UQ_ContestWinners_Contest").IsUnique();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}