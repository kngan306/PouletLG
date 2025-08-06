using System;
using System.Collections.Generic;

namespace WebLego.DataSet.GdrService;

public partial class ContactInformation
{
    public int ContactId { get; set; }

    public string PhoneNumber { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string Email { get; set; } = null!;

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int CreatedBy { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;
}