namespace WebLego.Models.ViewModel
{
    public class CommunityViewModel
    {
        public string Tab { get; set; } = "general";
        public List<CommunityPostViewModel> Posts { get; set; }
    }

    public class CommunityPostViewModel
    {
        public int PostId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int? OrderId { get; set; } // Sửa thành nullable
        public int? ProductId { get; set; } // Sửa thành nullable
        public string ProductName { get; set; }
        public int? ContestId { get; set; }
        public string ContestTitle { get; set; }
        public string ImageUrl { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public int CommentCount { get; set; }
        public int VoteCount { get; set; }
        public bool IsVoted { get; set; }
        public bool IsOwner { get; set; }
        public bool IsFlagged { get; set; }
        public List<CommunityCommentViewModel> Comments { get; set; }
    }

    public class CommunityCommentViewModel
    {
        public int CommentId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string CommentText { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsFlagged { get; set; }
    }
}