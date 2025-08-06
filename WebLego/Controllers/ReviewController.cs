using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLego.DataSet.GdrService;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace WebLego.Controllers
{
    public class ReviewController : Controller
    {
        private readonly DbpouletLgv5Context _context;
        public ReviewController(DbpouletLgv5Context context)
        {
            _context = context;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int orderId, List<int> productIds, List<int> ratings, List<string> comments, IFormCollection form)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Vui lòng đăng nhập để đánh giá." });

            if (productIds == null || ratings == null || productIds.Count == 0 || productIds.Count != ratings.Count)
                return Json(new { success = false, message = "Dữ liệu đánh giá không hợp lệ." });

            if (comments != null && comments.Count < productIds.Count)
            {
                comments.AddRange(Enumerable.Repeat("", productIds.Count - comments.Count));
            }
            else if (comments == null)
            {
                comments = Enumerable.Repeat("", productIds.Count).ToList();
            }

            var images = new IFormFile[productIds.Count];
            foreach (var file in form.Files.Where(f => f.Name.StartsWith("images[")))
            {
                if (file.Length > 0)
                {
                    if (int.TryParse(file.Name.Replace("images[", "").Replace("]", ""), out int index) && index >= 0 && index < productIds.Count)
                    {
                        images[index] = file;
                    }
                }
            }

            var order = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId && o.OrderStatus == "Hoàn thành");
            if (order == null) return Json(new { success = false, message = "Đơn hàng không hợp lệ hoặc chưa hoàn thành." });

            bool hasValidReview = false;
            for (int i = 0; i < productIds.Count; i++)
            {
                var productId = productIds[i];
                var rating = ratings[i];
                var comment = comments != null && i < comments.Count ? comments[i] : null;
                var image = images[i];

                if (rating < 1 || rating > 5)
                    return Json(new { success = false, message = $"Điểm đánh giá cho sản phẩm #{productId} phải từ 1 đến 5." });

                var orderDetail = order.OrderDetails.FirstOrDefault(od => od.ProductId == productId);
                if (orderDetail == null)
                    return Json(new { success = false, message = $"Sản phẩm #{productId} không thuộc đơn hàng này." });

                var existingReview = _context.ProductReviews
                    .FirstOrDefault(r => r.UserId == userId && r.ProductId == productId && r.OrderId == orderId);
                if (existingReview != null)
                    return Json(new { success = false, message = $"Bạn đã đánh giá sản phẩm #{productId} rồi." });

                string imageUrl = null;
                if (image != null && image.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(image.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                        return Json(new { success = false, message = $"Định dạng ảnh không hợp lệ cho sản phẩm #{productId}. Chỉ chấp nhận .jpg, .jpeg, .png." });

                    if (image.Length > 5 * 1024 * 1024)
                        return Json(new { success = false, message = $"Kích thước ảnh không được vượt quá 5MB cho sản phẩm #{productId}." });

                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/reviews", fileName);
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }
                        imageUrl = $"/images/reviews/{fileName}";
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi khi lưu ảnh cho sản phẩm #{productId}: {ex.Message}");
                        return Json(new { success = false, message = $"Lỗi khi lưu ảnh cho sản phẩm #{productId}: {ex.Message}" });
                    }
                }

                var review = new ProductReview
                {
                    UserId = userId.Value,
                    ProductId = productId,
                    OrderId = orderId,
                    Rating = rating,
                    Comment = string.IsNullOrWhiteSpace(comment) ? null : comment,
                    ImageUrl = imageUrl,
                    CreatedAt = DateTime.Now,
                    ReviewStatus = "Chưa phản hồi",
                    IsFlagged = false,
                    IsUpdated = false
                };
                _context.ProductReviews.Add(review);
                hasValidReview = true;
            }

            if (!hasValidReview)
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một sản phẩm để đánh giá với điểm từ 1 đến 5." });

            await _context.SaveChangesAsync();
            TempData["ReviewSuccess"] = "Đánh giá của bạn đã được gửi thành công!";
            return Json(new { success = true, message = "Đánh giá thành công!" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReview(int orderId, List<int> reviewIds, List<int> ratings, List<string> comments, IFormCollection form)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Json(new { success = false, message = "Vui lòng đăng nhập để cập nhật đánh giá." });

            if (reviewIds == null || ratings == null || reviewIds.Count == 0 || reviewIds.Count != ratings.Count)
                return Json(new { success = false, message = "Dữ liệu đánh giá không hợp lệ." });

            if (comments != null && comments.Count < reviewIds.Count)
            {
                comments.AddRange(Enumerable.Repeat("", reviewIds.Count - comments.Count));
            }
            else if (comments == null)
            {
                comments = Enumerable.Repeat("", reviewIds.Count).ToList();
            }

            var images = new IFormFile[reviewIds.Count];
            foreach (var file in form.Files.Where(f => f.Name.StartsWith("images[")))
            {
                if (file.Length > 0)
                {
                    if (int.TryParse(file.Name.Replace("images[", "").Replace("]", ""), out int index) && index >= 0 && index < reviewIds.Count)
                    {
                        images[index] = file;
                    }
                }
            }

            bool hasValidReview = false;
            for (int i = 0; i < reviewIds.Count; i++)
            {
                var reviewId = reviewIds[i];
                var rating = ratings[i];
                var comment = comments != null && i < comments.Count ? comments[i] : null;
                var image = images[i];

                if (rating < 1 || rating > 5)
                    return Json(new { success = false, message = $"Điểm đánh giá cho đánh giá #{reviewId} phải từ 1 đến 5." });

                var review = _context.ProductReviews
                    .FirstOrDefault(r => r.ReviewId == reviewId && r.UserId == userId && r.OrderId == orderId);
                if (review == null)
                    return Json(new { success = false, message = $"Đánh giá #{reviewId} không tồn tại hoặc bạn không có quyền chỉnh sửa. OrderId: {orderId}, UserId: {userId}, ReviewId: {reviewId}" });

                // Kiểm tra xem đánh giá có bị gắn cờ không
                if ((bool)review.IsFlagged)
                    return Json(new { success = false, message = $"Đánh giá #{reviewId} đã bị gắn cờ và không thể cập nhật." });

                if (review.IsUpdated)
                    return Json(new { success = false, message = $"Đánh giá #{reviewId} đã được cập nhật trước đó." });

                if (review.CreatedAt.HasValue && review.CreatedAt.Value.AddDays(7) < DateTime.Now)
                    return Json(new { success = false, message = $"Đã hết thời gian 7 ngày để cập nhật đánh giá #{reviewId}." });

                string imageUrl = review.ImageUrl;
                if (image != null && image.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(image.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                        return Json(new { success = false, message = $"Định dạng ảnh không hợp lệ cho đánh giá #{reviewId}. Chỉ chấp nhận .jpg, .jpeg, .png." });

                    if (image.Length > 5 * 1024 * 1024)
                        return Json(new { success = false, message = $"Kích thước ảnh không được vượt quá 5MB cho đánh giá #{reviewId}." });

                    var fileName = $"{Guid.NewGuid()}{extension}";
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/reviews", fileName);
                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await image.CopyToAsync(stream);
                        }
                        imageUrl = $"/images/reviews/{fileName}";

                        if (!string.IsNullOrEmpty(review.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", review.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                                System.IO.File.Delete(oldImagePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi khi lưu ảnh cho đánh giá #{reviewId}: {ex.Message}");
                        return Json(new { success = false, message = $"Lỗi khi lưu ảnh cho đánh giá #{reviewId}: {ex.Message}" });
                    }
                }

                review.Rating = rating;
                review.Comment = string.IsNullOrWhiteSpace(comment) ? null : comment;
                review.ImageUrl = imageUrl;
                review.IsUpdated = true;
                review.UpdatedAt = DateTime.Now;
                review.ReviewStatus = string.IsNullOrEmpty(review.AdminReply) ? "Chưa phản hồi" : "Đã phản hồi";

                hasValidReview = true;
            }

            if (!hasValidReview)
                return Json(new { success = false, message = "Vui lòng chọn ít nhất một sản phẩm để cập nhật đánh giá với điểm từ 1 đến 5." });

            await _context.SaveChangesAsync();
            TempData["ReviewSuccess"] = "Đánh giá của bạn đã được cập nhật thành công!";
            return Json(new { success = true, message = "Cập nhật đánh giá thành công!" });
        }
    }
}