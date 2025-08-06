using System;
using System.Collections.Generic;

namespace WebLego.Areas.Admin.Models
{
    public class OrderViewModel
    {
        public int OrderId { get; set; }

        // Thông tin khách hàng
        public int UserId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }

        // Ngày đặt hàng
        public DateTime? OrderDate { get; set; }

        // Trạng thái
        public string OrderStatus { get; set; }
        public string PaymentStatus { get; set; }

        // Tổng tiền & khuyến mãi
        public decimal? TotalAmount { get; set; }
        public decimal? ShippingFee { get; set; }
        public decimal? Discount { get; set; }
        public string DiscountCode { get; set; } // Thêm thuộc tính DiscountCode

        // Địa chỉ giao hàng
        public string FullAddress { get; set; }

        // Nhân viên giao hàng
        public int? ShipperId { get; set; }          // ID nhân viên giao hàng (nullable)
        public string ShipperName { get; set; }      // Tên nhân viên giao hàng (nếu có)

        // Danh sách chi tiết sản phẩm
        public List<OrderDetailViewModel> OrderDetails { get; set; }
    }

    public class OrderDetailViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal OriginalPrice { get; set; }
        public bool IsDiscounted { get; set; }

        public decimal TotalAmount
        {
            get
            {
                return UnitPrice * Quantity;
            }
        }
    }

}
