using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;  // để dùng IFormFile
using WebLego.DataSet.GdrService;

namespace WebLego.Areas.Admin.Models
{
    public class CategoryViewModel
    {
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        public string CategoryName { get; set; } = null!;

        public string? Description { get; set; }

        public string? ImagePath { get; set; }

        public string? BackgroundColor { get; set; }

        public string? ButtonColor { get; set; }

        // Dùng khi upload ảnh mới (nếu có form upload ảnh)
        public IFormFile? ImageFile { get; set; }

        public int CreatedBy { get; set; }

        public DateTime? CreatedAt { get; set; }

        // Dùng cho Index - phân trang, lọc
        public string? FilterCode { get; set; }

        public string? FilterName { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public int TotalItems { get; set; }

        public List<Category> Categories { get; set; } = new();

        public int TotalPages => (TotalItems + PageSize - 1) / PageSize;
    }
}
