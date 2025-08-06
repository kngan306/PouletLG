using System;
using System.Collections.Generic;

namespace WebLego.DataSet.GdrService;

public partial class Promotion
{
    public int PromotionId { get; set; }

    public string PromotionName { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? Status { get; set; }

    public decimal DiscountPercent { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
