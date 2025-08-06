namespace WebLego.Models.ViewModel
{
    public class OrderViewModel
    {
        public int OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public decimal ShippingFee { set; get; }
        public decimal TotalAmount { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string DiscountCode { get; set; } // Thêm thuộc tính mã giảm giá
        public decimal DiscountAmount { get; set; } // Thêm thuộc tính số tiền giảm
        public string VnpTransactionNo { get; set; } // Thêm để lưu mã giao dịch VNPAY
        public DateTime? VnpTransactionDate { get; set; }
        public List<OrderItemViewModel> Items { get; set; }
        public AddressViewModel Address { get; set; } // Thêm thuộc tính địa chỉ
        public List<OrderReviewViewModel> Reviews { get; set; } // Thêm thuộc tính Reviews
        public List<CommunityPostViewModel> CommunityPosts { get; set; } // Thêm thuộc tính CommunityPosts
    }

    public class OrderItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal OriginalPrice { get; set; } // Giá gốc của sản phẩm
        public decimal? DiscountPrice { get; set; } // Giá flash sale
        public bool IsDiscounted { get; set; } // Kiểm tra có trong flash sale
        public decimal Price { get; set; } // Giá cuối cùng (sau flash sale và rank)
        public string ImageUrl { get; set; }
        public decimal Savings { get; set; } // Số tiền tiết kiệm từ flash sale
        public bool HasReviewed { get; set; } // Thêm thuộc tính HasReviewed
    }

    public class OrderReviewViewModel
    {
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int? Rating { get; set; }
        public string Comment { get; set; }
        public string ImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string AdminReply { get; set; } // Thêm thuộc tính AdminReply
        public DateTime? AdminReplyAt { get; set; } // Thêm thuộc tính AdminReplyAt
        public bool IsFlagged { get; set; } // Thêm thuộc tính IsFlagged

        // CẬP NHẬT: Thêm IsUpdated để theo dõi trạng thái cập nhật
        public bool IsUpdated { get; set; }

        // CẬP NHẬT: Thêm UpdatedAt để lưu thời gian cập nhật 
        public DateTime? UpdatedAt { get; set; }
    }
}