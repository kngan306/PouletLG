namespace WebLego.Areas.Admin.Models
{
    public class BannerViewModel
    {
        public int BannerId { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByName { get; set; }
    }
}