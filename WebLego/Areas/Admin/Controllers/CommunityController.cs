using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebLego.DataSet.GdrService;
using WebLego.Models.ViewModel;

namespace WebLego.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("Admin/[controller]/[action]/{id?}")]
    public class CommunityController : Controller
    {
        private readonly DbpouletLgv5Context _context;

        public CommunityController(DbpouletLgv5Context context)
        {
            _context = context;
        }

        private int GetCurrentRoleId()
          => int.Parse(User.FindFirstValue(ClaimTypes.Role) ?? "0");

        // ✅ Cho phép cả Nhân viên (RoleId = 2) và Quản lý (RoleId = 3)
        private bool IsAuthorized()
        {
            int roleId = GetCurrentRoleId();
            return roleId == 2 || roleId == 3;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsAuthorized())
                return RedirectToAction("Login", "Auth", new { area = "" });

            var posts = await _context.CommunityPosts
                .Include(p => p.User)
                .Include(p => p.Product)
                .Include(p => p.Contest)
                .Select(p => new CommunityPostViewModel
                {
                    PostId = p.PostId,
                    UserId = p.UserId,
                    UserName = p.User.FullName,
                    OrderId = p.OrderId,
                    ProductId = p.ProductId,
                    ProductName = p.Product != null ? p.Product.ProductName : "N/A",
                    ContestId = p.ContestId,
                    ContestTitle = p.Contest != null ? p.Contest.Title : "N/A",
                    ImageUrl = p.ImageUrl,
                    Description = p.Description,
                    CreatedAt = p.CreatedAt,
                    CommentCount = p.Comments.Count(c => !c.IsFlagged), // Chỉ đếm bình luận không bị ẩn
                    VoteCount = p.ContestVotes.Count,
                    IsFlagged = p.IsFlagged,
                    IsVoted = false,
                    IsOwner = false
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(posts);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!IsAuthorized())
                return RedirectToAction("Login", "Auth", new { area = "" });

            if (id == null)
            {
                return NotFound();
            }

            var post = await _context.CommunityPosts
                .Include(p => p.User)
                .Include(p => p.Product)
                .Include(p => p.Contest)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(p => p.PostId == id);

            if (post == null)
            {
                return NotFound();
            }

            var viewModel = new CommunityPostViewModel
            {
                PostId = post.PostId,
                UserId = post.UserId,
                UserName = post.User.FullName,
                OrderId = post.OrderId,
                ProductId = post.ProductId,
                ProductName = post.Product != null ? post.Product.ProductName : "N/A",
                ContestId = post.ContestId,
                ContestTitle = post.Contest != null ? post.Contest.Title : "N/A",
                ImageUrl = post.ImageUrl,
                Description = post.Description,
                CreatedAt = post.CreatedAt,
                CommentCount = post.Comments.Count(c => !c.IsFlagged), // Chỉ đếm bình luận không bị ẩn
                VoteCount = post.ContestVotes.Count,
                IsFlagged = post.IsFlagged,
                IsVoted = false,
                IsOwner = false,
                Comments = post.Comments.Select(c => new CommunityCommentViewModel
                {
                    CommentId = c.CommentId,
                    UserId = c.UserId,
                    UserName = c.User.FullName,
                    CommentText = c.CommentText,
                    CreatedAt = c.CreatedAt,
                    IsFlagged = c.IsFlagged
                }).OrderBy(c => c.CreatedAt).ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FlagPost(int id)
        {
            if (!IsAuthorized())
                return RedirectToAction("Login", "Auth", new { area = "" });

            var post = await _context.CommunityPosts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            post.IsFlagged = true;
            post.CommentCount = 0; // Đặt lại CommentCount vì tất cả bình luận sẽ bị ẩn
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Bài đăng đã được đánh dấu ẩn thành công.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnflagPost(int id)
        {
            if (!IsAuthorized())
                return RedirectToAction("Login", "Auth", new { area = "" });

            var post = await _context.CommunityPosts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            post.IsFlagged = false;
            post.CommentCount = await _context.CommunityComments.CountAsync(c => c.PostId == id && !c.IsFlagged);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Bài đăng đã được bỏ đánh dấu ẩn thành công.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FlagComment(int id)
        {
            if (!IsAuthorized())
                return RedirectToAction("Login", "Auth", new { area = "" });

            var comment = await _context.CommunityComments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.CommentId == id);
            if (comment == null)
            {
                return NotFound();
            }

            comment.IsFlagged = true;
            var post = comment.Post;
            post.CommentCount = await _context.CommunityComments.CountAsync(c => c.PostId == post.PostId && !c.IsFlagged);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Bình luận đã được đánh dấu ẩn thành công.";
            return RedirectToAction(nameof(Details), new { id = comment.PostId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnflagComment(int id)
        {
            if (!IsAuthorized())
                return RedirectToAction("Login", "Auth", new { area = "" });

            var comment = await _context.CommunityComments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.CommentId == id);
            if (comment == null)
            {
                return NotFound();
            }

            comment.IsFlagged = false;
            var post = comment.Post;
            post.CommentCount = await _context.CommunityComments.CountAsync(c => c.PostId == post.PostId && !c.IsFlagged);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Bình luận đã được bỏ đánh dấu ẩn thành công.";
            return RedirectToAction(nameof(Details), new { id = comment.PostId });
        }
    }
}