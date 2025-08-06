using System;
using System.Collections.Generic;

namespace WebLego.DataSet.GdrService;

public partial class ProductReview
{
    public int ReviewId { get; set; }

    public int UserId { get; set; }

    public int ProductId { get; set; }

    public int? OrderId { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public bool? IsFlagged { get; set; }

    public string? ReviewStatus { get; set; }

    public string? AdminReply { get; set; }

    public DateTime? AdminReplyAt { get; set; }

    // CẬP NHẬT: Thêm thuộc tính IsUpdated để theo dõi trạng thái cập nhật đánh giá
    public bool IsUpdated { get; set; }

    // CẬP NHẬT: Thêm thuộc tính UpdatedAt để lưu thời gian cập nhật
    public DateTime? UpdatedAt { get; set; }
    public virtual Order? Order { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
