namespace WebLego.DataSet.GdrService;
public partial class ContestVote
{
    public int VoteId { get; set; }
    public int PostId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public virtual CommunityPost Post { get; set; }
    public virtual User User { get; set; }
}
