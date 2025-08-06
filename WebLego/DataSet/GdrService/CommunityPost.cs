namespace WebLego.DataSet.GdrService;

public partial class CommunityPost
{
    public int PostId { get; set; }
    public int UserId { get; set; }
    public int? OrderId { get; set; } // Sửa thành nullable
    public int? ProductId { get; set; } // Sửa thành nullable
    public int? ContestId { get; set; }
    public string ImageUrl { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CommentCount { get; set; }
    public bool IsFlagged { get; set; }
    public virtual User User { get; set; }
    public virtual Order Order { get; set; }
    public virtual Product Product { get; set; }
    public virtual Contest? Contest { get; set; }
    public virtual List<CommunityComment> Comments { get; set; } = new List<CommunityComment>();
    public virtual List<ContestVote> ContestVotes { get; set; } = new List<ContestVote>();
}