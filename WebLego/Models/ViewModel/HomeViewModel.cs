using System.Collections.Generic;
using WebLego.DataSet.GdrService; // Thay đổi namespace

namespace WebLego.Models.ViewModel
{
    public class HomeViewModel
    {
        public List<HomeBanner> Banners { get; set; } // Sử dụng WebLego.DataSet.GdrService.HomeBanner
        public List<Product> FeaturedProducts { get; set; }
        public List<Product> DiscountedProducts { get; set; }

        // Thêm danh sách Category để hiển thị category động
        public List<Category> Categories { get; set; }
    }
}
