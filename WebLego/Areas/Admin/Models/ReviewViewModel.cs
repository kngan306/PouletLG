namespace WebLego.Areas.Admin.Models
{
    public class ReviewViewModel
    {
        public int ReviewId { get; set; }
        public string ProductName { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public string ImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string ReviewStatus { get; set; }
        public bool IsFlagged { get; set; }
        public string AdminReply { get; set; }
        public DateTime? AdminReplyAt { get; set; }
        public bool IsUpdated { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
