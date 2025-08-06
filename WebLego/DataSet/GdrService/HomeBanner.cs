using System;
using System.Collections.Generic;

namespace WebLego.DataSet.GdrService;

public partial class HomeBanner
{
    public int BannerId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;
}
