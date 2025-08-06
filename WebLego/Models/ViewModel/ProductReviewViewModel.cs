namespace WebLego.Models.ViewModel
{
    public class ProductReviewViewModel
    {
        public int ReviewId { get; set; }
        public WebLego.DataSet.GdrService.User User { get; set; }
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public string ImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string AdminReply { get; set; }
        public DateTime? AdminReplyAt { get; set; }
        public bool IsFlagged { get; set; }

        // CẬP NHẬT: Thêm IsUpdated để theo dõi trạng thái cập nhật
        public bool IsUpdated { get; set; }

        // CẬP NHẬT: Thêm UpdatedAt để lưu thời gian cập nhật 
        public DateTime? UpdatedAt { get; set; }
    }
}