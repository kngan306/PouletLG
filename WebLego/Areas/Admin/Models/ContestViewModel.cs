namespace WebLego.Areas.Admin.Models
{
    public class ContestViewModel
    {
        public int ContestId { get; set; }
        public string UserName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? WinnerUserId { get; set; }
        public int? OrderId { get; set; }
        public string Status { get; set; } // Trạng thái từ Index
        public string WinnerFullName { get; set; }
        public int? RewardProductId { get; set; }
        public string ImageUrl { get; set; }
        public IFormFile ImageFile { get; set; } // Thêm để upload ảnh
        public bool IsActive { get; set; } // Thêm trường IsActive
        public string ContestStatus { get; set; } // Thêm để hiển thị trạng thái read-only
        public bool IsManager { get; set; } // Thêm để kiểm tra quyền Quản lý
    }
}