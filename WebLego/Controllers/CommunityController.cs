using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLego.DataSet.GdrService;
using WebLego.Models.ViewModel;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebLego.Controllers
{
    public class CommunityController : Controller
    {
        private readonly DbpouletLgv5Context _context;

        public CommunityController(DbpouletLgv5Context context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string tab = "general", string status = "Đang diễn ra")
        {
            // Ngăn cache trình duyệt
            Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Add("Pragma", "no-cache");
            Response.Headers.Add("Expires", "0");

            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Auth");
            }

            var model = new CommunityViewModel
            {
                Tab = tab,
                Posts = await _context.CommunityPosts
                    .Where(p => !p.IsFlagged)
                    .Include(p => p.User)
                    .Include(p => p.Product)
                    .Include(p => p.Comments.Where(c => !c.IsFlagged))
                        .ThenInclude(c => c.User)
                    .Include(p => p.Contest)
                    .Include(p => p.ContestVotes)
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => new CommunityPostViewModel
                    {
                        PostId = p.PostId,
                        UserId = p.UserId,
                        UserName = p.User.FullName,
                        OrderId = p.OrderId,
                        ProductId = p.ProductId,
                        ProductName = p.Product != null ? p.Product.ProductName : null,
                        ContestId = p.ContestId,
                        ContestTitle = p.Contest != null ? p.Contest.Title : null,
                        ImageUrl = p.ImageUrl,
                        Description = p.Description,
                        CreatedAt = p.CreatedAt,
                        CommentCount = p.Comments.Count(c => !c.IsFlagged),
                        VoteCount = p.ContestVotes.Count,
                        IsVoted = userId.HasValue && p.ContestVotes.Any(v => v.UserId == userId.Value),
                        Comments = p.Comments.Select(c => new CommunityCommentViewModel
                        {
                            CommentId = c.CommentId,
                            UserId = c.UserId,
                            UserName = c.User.FullName,
                            CommentText = c.CommentText,
                            CreatedAt = c.CreatedAt,
                            IsFlagged = c.IsFlagged
                        }).Where(c => !c.IsFlagged).ToList(),
                        IsOwner = userId.HasValue && p.UserId == userId.Value,
                        IsFlagged = p.IsFlagged
                    }).ToListAsync()
            };

            var currentDate = DateTime.Now;
            var activeContests = await _context.Contests
                .Include(c => c.RewardProduct)
                    .ThenInclude(p => p.ProductImages)
                .Where(c =>
                    (status == "Sắp diễn ra" && currentDate < c.StartDate) ||
                    (status == "Đang diễn ra" && currentDate >= c.StartDate && currentDate <= c.EndDate) ||
                    (status == "Đã kết thúc" && currentDate > c.EndDate))
                .ToListAsync();

            var activeContest = activeContests.FirstOrDefault();
            string productName = "Chưa xác định";
            decimal? productPrice = null;
            string contestImage = activeContest?.ImageUrl ?? null;
            string rewardImage = null;

            if (activeContest?.RewardProduct != null)
            {
                productName = activeContest.RewardProduct.ProductName;
                productPrice = activeContest.RewardProduct.Price;
                rewardImage = activeContest.RewardProduct.ProductImages.FirstOrDefault(i => i.IsMain == true)?.ImageUrl ?? "/images/placeholder.png";
            }

            ViewBag.ActiveContest = activeContest;
            ViewBag.ContestImage = contestImage;
            ViewBag.RewardImage = rewardImage;
            ViewBag.ProductName = productName;
            ViewBag.ProductPrice = productPrice;
            ViewBag.IsAdmin = userId.HasValue && _context.Users.Any(u => u.UserId == userId.Value && u.RoleId == 3);
            ViewBag.Status = status;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost(CommunityPostViewModel model, IFormFile image)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Vui lòng đăng nhập để đăng bài." });
                    return RedirectToAction("Login", "Auth");
                }

                if (string.IsNullOrWhiteSpace(model.Description) && (image == null || image.Length == 0))
                {
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        return Json(new { success = false, message = "Vui lòng nhập nội dung hoặc tải ảnh." });
                    TempData["ErrorMessage"] = "Vui lòng nhập nội dung hoặc tải ảnh.";
                    return RedirectToAction("Index");
                }

                string imageUrl = "";
                if (image != null && image.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(image.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                            return Json(new { success = false, message = "Định dạng ảnh không hợp lệ. Chỉ chấp nhận .jpg, .jpeg, .png." });
                        TempData["ErrorMessage"] = "Định dạng ảnh không hợp lệ. Chỉ chấp nhận .jpg, .jpeg, .png.";
                        return RedirectToAction("Index");
                    }

                    if (image.Length > 5 * 1024 * 1024)
                    {
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                            return Json(new { success = false, message = "Kích thước ảnh không được vượt quá 5MB." });
                        TempData["ErrorMessage"] = "Kích thước ảnh không được vượt quá 5MB.";
                        return RedirectToAction("Index");
                    }

                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/community", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }
                    imageUrl = $"/images/community/{fileName}";
                }

                var post = new CommunityPost
                {
                    UserId = userId.Value,
                    OrderId = null,
                    ProductId = null,
                    Description = model.Description?.Trim(),
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.Now,
                    CommentCount = 0,
                    IsFlagged = false
                };

                _context.CommunityPosts.Add(post);
                await _context.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Đăng bài thành công!" });
                }

                TempData["SuccessMessage"] = "Đăng bài thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = $"Lỗi server: {ex.Message}" });
                TempData["ErrorMessage"] = $"Lỗi server: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateContestPost(int orderId, List<int> productIds, List<string> descriptions, IFormCollection form)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, message = "Vui lòng đăng nhập để đăng bài." });

            if (productIds == null || descriptions == null || productIds.Count == 0 || productIds.Count != descriptions.Count)
                return Json(new { success = false, message = "Dữ liệu bài đăng không hợp lệ." });

            var images = new List<IFormFile>();
            for (int i = 0; i < productIds.Count; i++)
            {
                var file = form.Files[$"images[{i}]"];
                images.Add(file);
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId.Value && o.OrderStatus == "Hoàn thành");
            if (order == null)
                return Json(new { success = false, message = "Đơn hàng không hợp lệ hoặc chưa hoàn thành." });

            var activeContest = await _context.Contests
                .FirstOrDefaultAsync(c => c.IsActive && c.StartDate <= DateTime.Now && c.EndDate >= DateTime.Now);
            if (activeContest == null)
                return Json(new { success = false, message = "Hiện tại không có cuộc thi nào đang diễn ra." });

            bool hasValidPost = false;
            for (int i = 0; i < productIds.Count; i++)
            {
                var productId = productIds[i];
                var description = descriptions[i]?.Trim();
                var image = images[i];

                if (string.IsNullOrWhiteSpace(description) || image == null || image.Length == 0)
                {
                    continue;
                }

                if (!order.OrderDetails.Any(od => od.ProductId == productId))
                    return Json(new { success = false, message = $"Sản phẩm #{productId} không thuộc đơn hàng này." });

                if (order.OrderDate.HasValue && order.OrderDate.Value.AddDays(30) < DateTime.Now)
                    return Json(new { success = false, message = "Đơn hàng đã quá 30 ngày, không thể tham gia cuộc thi." });

                var existingPost = await _context.CommunityPosts
                    .FirstOrDefaultAsync(p => p.UserId == userId.Value && p.OrderId == orderId && p.ProductId == productId && p.ContestId == activeContest.ContestId);
                if (existingPost != null)
                    return Json(new { success = false, message = $"Bạn đã đăng bài cho sản phẩm #{productId} trong cuộc thi này." });

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(image.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                    return Json(new { success = false, message = $"Định dạng ảnh không hợp lệ cho sản phẩm #{productId}. Chỉ chấp nhận .jpg, .jpeg, .png." });

                if (image.Length > 5 * 1024 * 1024)
                    return Json(new { success = false, message = $"Kích thước ảnh không được vượt quá 5MB cho sản phẩm #{productId}." });

                var fileName = $"{Guid.NewGuid()}{extension}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/community", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
                var imageUrl = $"/images/community/{fileName}";

                var post = new CommunityPost
                {
                    UserId = userId.Value,
                    OrderId = orderId,
                    ProductId = productId,
                    ContestId = activeContest.ContestId,
                    Description = description,
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.Now,
                    CommentCount = 0,
                    IsFlagged = false
                };

                _context.CommunityPosts.Add(post);
                hasValidPost = true;
            }

            if (!hasValidPost)
                return Json(new { success = false, message = "Vui lòng nhập mô tả và tải ảnh cho ít nhất một sản phẩm hợp lệ." });

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đăng bài cuộc thi thành công!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int postId, string description, IFormFile image)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, message = "Vui lòng đăng nhập để chỉnh sửa bài đăng." });

            var post = await _context.CommunityPosts
                .FirstOrDefaultAsync(p => p.PostId == postId && p.UserId == userId.Value && !p.IsFlagged);
            if (post == null)
                return Json(new { success = false, message = "Bài đăng không tồn tại, đã bị ẩn hoặc bạn không có quyền chỉnh sửa." });

            if (post.ContestId.HasValue)
                return Json(new { success = false, message = "Không thể chỉnh sửa bài đăng cuộc thi." });

            if (string.IsNullOrWhiteSpace(description) && (image == null || image.Length == 0) && string.IsNullOrEmpty(post.ImageUrl))
                return Json(new { success = false, message = "Vui lòng nhập nội dung hoặc tải ảnh." });

            if (!string.IsNullOrWhiteSpace(description))
                post.Description = description;

            if (image != null && image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(image.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                    return Json(new { success = false, message = "Định dạng ảnh không hợp lệ. Chỉ chấp nhận .jpg, .jpeg, .png." });

                if (image.Length > 5 * 1024 * 1024)
                    return Json(new { success = false, message = "Kích thước ảnh không được vượt quá 5MB." });

                var fileName = $"{Guid.NewGuid()}{extension}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/community", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }
                var newImageUrl = $"/images/community/{fileName}";

                if (!string.IsNullOrEmpty(post.ImageUrl))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", post.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                        System.IO.File.Delete(oldImagePath);
                }

                post.ImageUrl = newImageUrl;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Chỉnh sửa bài đăng thành công!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, message = "Vui lòng đăng nhập để xóa bài đăng." });

            var post = await _context.CommunityPosts
                .Include(p => p.Comments)
                .Include(p => p.ContestVotes)
                .FirstOrDefaultAsync(p => p.PostId == postId && p.UserId == userId.Value && !p.IsFlagged);
            if (post == null)
                return Json(new { success = false, message = "Bài đăng không tồn tại, đã bị ẩn hoặc bạn không có quyền xóa." });

            if (!string.IsNullOrEmpty(post.ImageUrl))
            {
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", post.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                    System.IO.File.Delete(imagePath);
            }

            _context.CommunityComments.RemoveRange(post.Comments);
            _context.ContestVotes.RemoveRange(post.ContestVotes);
            _context.CommunityPosts.Remove(post);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa bài đăng thành công!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateComment(int postId, string commentText)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, message = "Vui lòng đăng nhập để bình luận." });

            if (string.IsNullOrWhiteSpace(commentText))
                return Json(new { success = false, message = "Vui lòng nhập nội dung bình luận." });

            var post = await _context.CommunityPosts.FindAsync(postId);
            if (post == null || post.IsFlagged)
                return Json(new { success = false, message = "Bài đăng không tồn tại hoặc đã bị ẩn." });

            var comment = new CommunityComment
            {
                PostId = postId,
                UserId = userId.Value,
                CommentText = commentText,
                CreatedAt = DateTime.Now,
                IsFlagged = false
            };

            _context.CommunityComments.Add(comment);
            post.CommentCount = await _context.CommunityComments.CountAsync(c => c.PostId == postId && !c.IsFlagged);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Bình luận thành công!",
                commentId = comment.CommentId,
                userName = _context.Users.Find(userId.Value)?.FullName,
                createdAt = comment.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                isFlagged = comment.IsFlagged // Thêm isFlagged vào phản hồi
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment(int commentId, string commentText)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, message = "Vui lòng đăng nhập để chỉnh sửa bình luận." });

            var comment = await _context.CommunityComments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.CommentId == commentId && c.UserId == userId.Value && !c.IsFlagged && !c.Post.IsFlagged);
            if (comment == null)
                return Json(new { success = false, message = "Bình luận không tồn tại, đã bị ẩn, thuộc bài đăng bị ẩn hoặc bạn không có quyền chỉnh sửa." });

            if (string.IsNullOrWhiteSpace(commentText))
                return Json(new { success = false, message = "Vui lòng nhập nội dung bình luận." });

            comment.CommentText = commentText.Trim();
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Chỉnh sửa bình luận thành công!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, message = "Vui lòng đăng nhập để xóa bình luận." });

            var comment = await _context.CommunityComments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.CommentId == commentId && c.UserId == userId.Value && !c.IsFlagged && !c.Post.IsFlagged);
            if (comment == null)
                return Json(new { success = false, message = "Bình luận không tồn tại, đã bị ẩn, thuộc bài đăng bị ẩn hoặc bạn không có quyền xóa." });

            var post = comment.Post;
            _context.CommunityComments.Remove(comment);
            post.CommentCount = await _context.CommunityComments.CountAsync(c => c.PostId == post.PostId && !c.IsFlagged);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa bình luận thành công!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VotePost(int postId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, message = "Vui lòng đăng nhập để vote." });

            var post = await _context.CommunityPosts
                .Include(p => p.Contest)
                .FirstOrDefaultAsync(p => p.PostId == postId && !p.IsFlagged);
            if (post == null)
                return Json(new { success = false, message = "Bài đăng không tồn tại hoặc đã bị ẩn." });

            if (!post.ContestId.HasValue || post.Contest == null || post.Contest.EndDate < DateTime.Now || !post.Contest.IsActive)
                return Json(new { success = false, message = "Cuộc thi không tồn tại hoặc đã kết thúc." });

            if (post.UserId == userId.Value)
                return Json(new { success = false, message = "Bạn không thể vote cho bài đăng của chính mình." });

            var existingVote = await _context.ContestVotes
                .FirstOrDefaultAsync(v => v.PostId == postId && v.UserId == userId.Value);
            if (existingVote != null)
                return Json(new { success = false, message = "Bạn đã vote cho bài đăng này rồi." });

            var vote = new ContestVote
            {
                PostId = postId,
                UserId = userId.Value,
                CreatedAt = DateTime.Now
            };

            _context.ContestVotes.Add(vote);
            await _context.SaveChangesAsync();

            var voteCount = await _context.ContestVotes.CountAsync(v => v.PostId == postId);
            return Json(new { success = true, message = "Vote thành công!", voteCount = voteCount });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FlagPost(int postId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue || !_context.Users.Any(u => u.UserId == userId.Value && u.RoleId == 3))
                return Json(new { success = false, message = "Bạn không có quyền gắn cờ bài đăng." });

            var post = await _context.CommunityPosts.FindAsync(postId);
            if (post == null)
                return Json(new { success = false, message = "Bài đăng không tồn tại." });

            post.IsFlagged = true;
            post.CommentCount = 0; // Đặt lại CommentCount vì tất cả bình luận sẽ bị ẩn khi bài đăng bị ẩn
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Gắn cờ bài đăng thành công!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FlagComment(int commentId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue || !_context.Users.Any(u => u.UserId == userId.Value && u.RoleId == 3))
                return Json(new { success = false, message = "Bạn không có quyền gắn cờ bình luận." });

            var comment = await _context.CommunityComments
                .Include(c => c.Post)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);
            if (comment == null)
                return Json(new { success = false, message = "Bình luận không tồn tại." });

            comment.IsFlagged = true;
            var post = comment.Post;
            post.CommentCount = await _context.CommunityComments.CountAsync(c => c.PostId == post.PostId && !c.IsFlagged);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Gắn cờ bình luận thành công!" });
        }

        public IActionResult GetImage(string path)
        {
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", path.TrimStart('/'));
            if (!System.IO.File.Exists(imagePath))
                return NotFound();

            var imageBytes = System.IO.File.ReadAllBytes(imagePath);
            var extension = Path.GetExtension(imagePath).ToLower();
            var contentType = extension switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };
            return File(imageBytes, contentType);
        }
    }
}