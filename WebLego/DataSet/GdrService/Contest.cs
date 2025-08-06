namespace WebLego.DataSet.GdrService;

public partial class Contest
{
    public int ContestId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public bool IsActive { get; set; }
    public string ContestStatus { get; set; }
    public int? RewardProductId { get; set; } // Thêm cột cho phần thưởng
    public string ImageUrl { get; set; } // Thêm cột cho hình ảnh tùy chỉnh
    public virtual User CreatedByNavigation { get; set; }
    public virtual Product RewardProduct { get; set; } // Thêm navigation property
    public virtual List<CommunityPost> CommunityPosts { get; set; } = new List<CommunityPost>();
}