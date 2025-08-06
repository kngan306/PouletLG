namespace WebLego.Models.ViewModel
{
    public class MembershipViewModel
    {
        public string CustomerRank { get; set; }
        public int CompletedOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public List<MembershipTierViewModel> MembershipTiers { get; set; }
    }

    public class MembershipTierViewModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal RequiredAmount { get; set; }
        public decimal SpentPercentage { get; set; }
    }
}