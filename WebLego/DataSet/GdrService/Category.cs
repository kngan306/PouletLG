using System;
using System.Collections.Generic;

namespace WebLego.DataSet.GdrService;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public int CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }
    // Thuộc tính mới thêm
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public string? BackgroundColor { get; set; }
    public string? ButtonColor { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
