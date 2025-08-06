namespace WebLego.DataSet.GdrService;

public partial class ContestWinner
{
    public int WinnerId { get; set; }
    public int ContestId { get; set; }
    public int UserId { get; set; }
    public int RewardProductId { get; set; }
    public int? OrderId { get; set; }
    public DateTime WonAt { get; set; }
    public string Status { get; set; }
    public virtual Contest Contest { get; set; }
    public virtual User User { get; set; }
    public virtual Product RewardProduct { get; set; }
    public virtual Order Order { get; set; }
}