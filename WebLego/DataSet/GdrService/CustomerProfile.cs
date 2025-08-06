using System;
using System.Collections.Generic;

namespace WebLego.DataSet.GdrService;

public partial class CustomerProfile
{
    public int CustomerId { get; set; }

    public string? DiscountCode { get; set; }

    public string? CustomerRank { get; set; }

    public virtual User Customer { get; set; } = null!;
}
