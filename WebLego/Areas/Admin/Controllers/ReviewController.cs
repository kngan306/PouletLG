using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebLego.Areas.Admin.Models;
using WebLego.DataSet.GdrService;

namespace WebLego.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("Admin/[controller]/[action]/{id?}")]
    public class ReviewController : Controller
    {
        private readonly DbpouletLgv5Context _context;
        private const int PageSize = 10;

        public ReviewController(DbpouletLgv5Context context)
        {
            _context = context;
        }

        // ✅ Giống CategoryController – lấy RoleId từ Claim
        private int GetCurrentRoleId()
            => int.Parse(User.FindFirstValue(ClaimTypes.Role) ?? "0");

        // ✅ Cho phép cả Nhân viên (RoleId = 2) và Quản lý (RoleId = 3)
        private bool IsAuthorized()
        {
            int roleId = GetCurrentRoleId();
            return roleId == 2 || roleId == 3;
        }

        public IActionResult Index(string reviewStatus = null, DateTime? startDate = null, DateTime? endDate = null, bool? isFlagged = null, int page = 1)
        {
            if (!IsAuthorized())
                return RedirectToAction("Login", "Auth", new { area = "" });

            var query = _context.ProductReviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(reviewStatus))
            {
                query = query.Where(r => r.ReviewStatus == reviewStatus);
                ViewBag.ReviewStatus = reviewStatus;
            }

            if (startDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= startDate.Value);
                ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt <= endDate.Value.AddDays(1).AddTicks(-1));
                ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            }

            if (isFlagged.HasValue)
            {
                query = query.Where(r => r.IsFlagged == isFlagged.Value);
                ViewBag.IsFlagged = isFlagged.Value;
            }

            int totalItems = query.Count();
            var reviews = query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(r => new ReviewViewModel
                {
                    ReviewId = r.ReviewId,
                    ProductName = r.Product != null ? r.Product.ProductName : "Sản phẩm không tồn tại",
                    UserName = r.User != null ? r.User.FullName : "Người dùng không tồn tại",
                    Rating = r.Rating ?? 0,
                    CreatedAt = r.CreatedAt,
                    ReviewStatus = r.ReviewStatus,
                    IsFlagged = r.IsFlagged ?? false,
                    AdminReply = r.AdminReply,
                    AdminReplyAt = r.AdminReplyAt,
                    IsUpdated = r.IsUpdated,
                    UpdatedAt = r.UpdatedAt
                })
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / PageSize);

            return View(reviews);
        }

        public IActionResult Detail(int id)
        {
            if (!IsAuthorized())
                return RedirectToAction("Login", "Auth", new { area = "" });

            var review = _context.ProductReviews
                .Include(r => r.User)
                .Include(r => r.Product)
                .FirstOrDefault(r => r.ReviewId == id);

            if (review == null)
                return NotFound();

            var viewModel = new ReviewViewModel
            {
                ReviewId = review.ReviewId,
                ProductName = review.Product?.ProductName ?? "Sản phẩm không tồn tại",
                UserName = review.User?.FullName ?? "Người dùng không tồn tại",
                Rating = review.Rating ?? 0,
                Comment = review.Comment,
                ImageUrl = review.ImageUrl,
                CreatedAt = review.CreatedAt,
                ReviewStatus = review.ReviewStatus,
                IsFlagged = review.IsFlagged ?? false,
                AdminReply = review.AdminReply,
                AdminReplyAt = review.AdminReplyAt,
                IsUpdated = review.IsUpdated,
                UpdatedAt = review.UpdatedAt
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Reply(int reviewId, string adminReply)
        {
            if (!IsAuthorized())
                return Json(new { success = false, message = "Bạn không có quyền thực hiện hành động này." });

            if (string.IsNullOrWhiteSpace(adminReply))
                return Json(new { success = false, message = "Phản hồi không được để trống." });

            var review = await _context.ProductReviews.FindAsync(reviewId);
            if (review == null)
                return Json(new { success = false, message = "Đánh giá không tồn tại." });

            review.AdminReply = adminReply;
            review.AdminReplyAt = DateTime.Now;
            review.ReviewStatus = review.IsFlagged == true ? "Bị ẩn" : "Đã phản hồi";

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Phản hồi đã được cập nhật thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFlag(int reviewId)
        {
            if (!IsAuthorized())
                return Json(new { success = false, message = "Bạn không có quyền thực hiện hành động này." });

            var review = await _context.ProductReviews.FindAsync(reviewId);
            if (review == null)
                return Json(new { success = false, message = "Đánh giá không tồn tại." });

            review.IsFlagged = !(review.IsFlagged ?? false);
            review.ReviewStatus = review.IsFlagged == true
                ? "Bị ẩn"
                : (string.IsNullOrEmpty(review.AdminReply) ? "Chưa phản hồi" : "Đã phản hồi");

            await _context.SaveChangesAsync();
            return Json(new
            {
                success = true,
                message = review.IsFlagged == true ? "Đánh giá đã được gắn cờ và ẩn." : "Đánh giá đã được bỏ gắn cờ."
            });
        }
    }
}
