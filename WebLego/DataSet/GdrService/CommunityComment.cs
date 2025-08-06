namespace WebLego.DataSet.GdrService;
public partial class CommunityComment
{
    public int CommentId { get; set; }
    public int PostId { get; set; }
    public int UserId { get; set; }
    public string CommentText { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsFlagged { get; set; }
    public virtual CommunityPost Post { get; set; }
    public virtual User User { get; set; }
}
