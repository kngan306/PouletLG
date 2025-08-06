using System;
using System.Collections.Generic;

namespace WebLego.DataSet.GdrService;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public string? ProductDes { get; set; }

    public decimal Price { get; set; }

    public string? AgeRange { get; set; }

    public int? PieceCount { get; set; }

    public decimal? Rating { get; set; }

    public int? Sold { get; set; }

    public int? StockQuantity { get; set; }

    public bool? IsFeatured { get; set; }

    public string? ProductStatus { get; set; }

    public int? CategoryId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public decimal? DiscountPrice { get; set; }

    public int CreatedBy { get; set; }

    public int? PromotionId { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual Category? Category { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

    public virtual Promotion? Promotion { get; set; }

    public virtual ICollection<ReturnDetail> ReturnDetailProducts { get; set; } = new List<ReturnDetail>();

    public virtual ICollection<ReturnDetail> ReturnDetailReplacementProducts { get; set; } = new List<ReturnDetail>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    // Thêm thuộc tính Favorites
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();

    public virtual ICollection<CommunityPost> CommunityPosts { get; set; } = new List<CommunityPost>();

}
