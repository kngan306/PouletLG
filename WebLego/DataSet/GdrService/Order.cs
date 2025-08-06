using System;
using System.Collections.Generic;

namespace WebLego.DataSet.GdrService;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? OrderStatus { get; set; }

    public decimal? ShippingFee { get; set; }

    public decimal? Discount { get; set; }

    public decimal? TotalAmount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? PaymentStatus { get; set; }

    public int AddressId { get; set; }

    public string? VnpTransactionNo { get; set; }

    public DateTime? VnpTransactionDate { get; set; }

    public int? ShipperId { get; set; }

    public virtual UserAddress Address { get; set; } = null!;

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<ProductReturn> ProductReturns { get; set; } = new List<ProductReturn>();

    public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();

    public virtual User? Shipper { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual List<CommunityPost> CommunityPosts { get; set; } = new List<CommunityPost>();
}
