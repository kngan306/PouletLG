using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLego.DataSet.GdrService;
using WebLego.Models.ViewModel;

namespace WebLego.Controllers
{
    public class ProductController : Controller
    {
        private readonly DbpouletLgv5Context _context;
        private const int PageSize = 9;

        public ProductController(DbpouletLgv5Context context)
        {
            _context = context;
        }

        public IActionResult Index(int? id, int page = 1, decimal? minPrice = null, decimal? maxPrice = null, bool isPromotion = false, string search = null)
        {
            var query = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Promotion)
                .AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.ProductName.Contains(search));
                ViewBag.SearchTerm = search;
                ViewData["Title"] = $"Kết quả tìm kiếm: {search}";
            }

            // Lọc danh mục
            if (id.HasValue)
            {
                query = query.Where(p => p.CategoryId == id.Value);
                var category = _context.Categories
                    .Where(c => c.CategoryId == id.Value)
                    .Select(c => new { c.CategoryId, c.CategoryName })
                    .FirstOrDefault();

                ViewBag.CurrentCategory = category?.CategoryName;
                ViewBag.CurrentCategoryId = category?.CategoryId;
            }

            // Lọc sản phẩm khuyến mãi theo yêu cầu: PromotionId != null và DiscountPrice != null
            DateTime? promoEndDate = null;
            if (isPromotion)
            {
                var now = DateTime.Now;
                query = query.Where(p => p.PromotionId != null && p.DiscountPrice.HasValue);
                promoEndDate = query.Any() ? query.Where(p => p.Promotion != null).Select(p => p.Promotion.EndDate).Min() : null;
            }
            ViewBag.PromoEndDate = promoEndDate;

            // Lọc theo giá, ưu tiên DiscountPrice nếu có
            if (minPrice.HasValue || maxPrice.HasValue)
            {
                query = query.Where(p =>
                    (p.DiscountPrice.HasValue
                        ? p.DiscountPrice >= (minPrice ?? decimal.MinValue) && p.DiscountPrice <= (maxPrice ?? decimal.MaxValue)
                        : p.Price >= (minPrice ?? decimal.MinValue) && p.Price <= (maxPrice ?? decimal.MaxValue))
                );
            }

            int totalItems = query.Count();

            var products = query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                var favoriteProductIds = _context.Favorites
                    .Where(f => f.UserId == userId.Value)
                    .Select(f => f.ProductId)
                    .ToList();
                ViewBag.FavoriteProductIds = favoriteProductIds;
            }
            else
            {
                ViewBag.FavoriteProductIds = new List<int>();
            }

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / PageSize);
            ViewBag.IsPromotion = isPromotion;

            return View(products);
        }

        public IActionResult Detail(int id)
        {
            var product = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Category)
                .FirstOrDefault(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            bool isFavorite = false;

            if (userId.HasValue)
            {
                isFavorite = _context.Favorites.Any(f => f.UserId == userId.Value && f.ProductId == id);
            }

            // Lấy danh sách đánh giá không bị ẩn
            var reviews = _context.ProductReviews
                .Include(r => r.User)
                .Where(r => r.ProductId == id && r.IsFlagged == false)
                .Select(r => new ProductReviewViewModel
                {
                    ReviewId = r.ReviewId,
                    User = r.User,
                    ProductId = r.ProductId,
                    Rating = r.Rating ?? 0,
                    Comment = r.Comment,
                    ImageUrl = r.ImageUrl,
                    CreatedAt = r.CreatedAt,
                    AdminReply = r.AdminReply,
                    AdminReplyAt = r.AdminReplyAt,
                    IsFlagged = (bool)r.IsFlagged
                })
                .ToList();

            // Tính lại điểm đánh giá trung bình dựa trên các đánh giá không bị ẩn
            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            product.Rating = (decimal?)(float?)averageRating;

            // Lấy danh sách sản phẩm đề xuất
            var recommendedProducts = _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.ProductImages.Any(i => i.IsMain == true))
                .Where(p => p.ProductId != id)
                .OrderBy(r => Guid.NewGuid())
                .Take(12)
                .ToList();

            ViewBag.Recommended = recommendedProducts;
            if (userId.HasValue)
            {
                var favoriteProductIds = _context.Favorites
                    .Where(f => f.UserId == userId.Value)
                    .Select(f => f.ProductId)
                    .ToList();
                ViewBag.FavoriteProductIds = favoriteProductIds;
            }
            else
            {
                ViewBag.FavoriteProductIds = new List<int>();
            }

            var viewModel = new ProductDetailViewModel
            {
                Product = product,
                IsFavorite = isFavorite,
                StockQuantity = product.StockQuantity ?? 0,
                Reviews = reviews
            };

            return View(viewModel);
        }
    }
}