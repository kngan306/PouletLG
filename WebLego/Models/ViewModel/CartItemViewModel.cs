namespace WebLego.Models.ViewModel
{
    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int StockQuantity { get; set; }  // số lượng tồn kho thực tế
        public decimal Total { get; set; }
        public bool IsDiscounted { get; set; } // Kiểm tra có khuyến mãi không
        public decimal OriginalPrice { get; set; } // Giá gốc nếu có khuyến mãi
        public decimal Savings { get; set; } // Số tiền tiết kiệm (OriginalPrice - Price) nếu có khuyến mãi

    }
}
