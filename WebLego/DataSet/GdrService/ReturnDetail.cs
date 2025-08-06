using System;
using System.Collections.Generic;

namespace WebLego.DataSet.GdrService;

public partial class ReturnDetail
{
    public int ReturnDetailId { get; set; }

    public int ReturnId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public string? Reason { get; set; }

    public int? ReplacementProductId { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Product? ReplacementProduct { get; set; }

    public virtual ProductReturn Return { get; set; } = null!;
}
