using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WebLego.DataSet.GdrService;

namespace WebLego.Areas.Admin.Models
{
    public class ProductViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        public string ProductName { get; set; }

        public string? ProductDes { get; set; }

        [Required(ErrorMessage = "Giá là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải >= 0")]
        public decimal Price { get; set; }

        public string? AgeRange { get; set; }

        public int? PieceCount { get; set; }

        public decimal? Rating { get; set; }

        public int? Sold { get; set; }

        public int? StockQuantity { get; set; }

        public bool IsFeatured { get; set; }

        public string ProductStatus { get; set; } = "Hoạt động";

        public int? CategoryId { get; set; }

        public decimal? DiscountPrice { get; set; }

        public int CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? PromotionId { get; set; }
        public string? PromotionName { get; set; }


        public List<IFormFile>? Images { get; set; } // Để upload nhiều ảnh

        public string? MainImageUrl { get; set; } // Hiển thị ảnh chính (nếu có)

        public List<string>? ExistingImageUrls { get; set; } // Các ảnh hiện có

        // Dùng để hiển thị dropdown
        public List<Category>? AllCategories { get; set; }

        public List<Promotion>? AllPromotions { get; set; }

        // Thêm 1 property mới để binding select list:
        public List<SelectListItem>? CategoriesSelectList { get; set; }

        // Nếu cần dropdown Promotion thì cũng tương tự:
        public List<SelectListItem>? PromotionsSelectList { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public List<Product>? Products { get; set; }

        // Bộ lọc
        public string? FilterName { get; set; }
        public int? FilterCategoryId { get; set; } // để lọc theo danh mục
        public List<Category>? Categories { get; set; } // chứa danh sách danh mục
    }
}
