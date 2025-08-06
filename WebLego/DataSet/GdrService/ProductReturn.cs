using System;
using System.Collections.Generic;

namespace WebLego.DataSet.GdrService;

public partial class ProductReturn
{
    public int ReturnId { get; set; }

    public int UserId { get; set; }

    public int OrderId { get; set; }

    public string? RequestType { get; set; }

    public decimal? TotalRefundAmount { get; set; }

    public string? ImageUrl { get; set; }

    public string? Note { get; set; }

    public string? ReturnStatus { get; set; }

    public DateTime? RequestedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<ReturnDetail> ReturnDetails { get; set; } = new List<ReturnDetail>();

    public virtual User User { get; set; } = null!;
}
