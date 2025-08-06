using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebLego.Areas.Admin.Models;
using WebLego.DataSet.GdrService;

namespace WebLego.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("Admin/[controller]/[action]/{id?}")]
    public class PromotionsController : Controller
    {
        private readonly DbpouletLgv5Context _context;

        public PromotionsController(DbpouletLgv5Context context)
        {
            _context = context;
        }

        // GET: Admin/Promotions
        public async Task<IActionResult> Index()
        {
            await UpdatePromotionStatusesAsync();
            await SyncProductsWithPromotionStatusesAsync();

            var promotions = await _context.Promotions.ToListAsync();
            return View(promotions);
        }


        // GET: Admin/Promotions/Create
        public IActionResult Create()
        {
            // Nếu bạn có danh mục sản phẩm, load vào ViewBag để dùng cho lọc
            ViewBag.Categories = _context.Categories.ToList();

            var model = new PromotionViewModel
            {
                AvailableProducts = _context.Products
                    .Where(p => p.PromotionId == null)
                    .ToList()
            };
            return View(model);
        }

        // POST: Admin/Promotions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PromotionViewModel model)
        {
            // Kiểm tra ngày hợp lệ
            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("", "Ngày kết thúc phải sau ngày bắt đầu.");
            }

            // Kiểm tra đã chọn sản phẩm chưa
            if (model.SelectedProductIds == null || !model.SelectedProductIds.Any())
            {
                ModelState.AddModelError("", "Vui lòng chọn ít nhất một sản phẩm áp dụng khuyến mãi.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _context.Categories.ToList();

                model.AvailableProducts = _context.Products
                    .Where(p => p.PromotionId == null)
                    .ToList();
                return View(model);
            }

            try
            {
                var now = DateTime.Now;
                string status = now < model.StartDate ? "Sắp diễn ra" :
                                now > model.EndDate ? "Hết hạn" :
                                "Còn hạn";

                var promotion = new Promotion
                {
                    PromotionName = model.PromotionName,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    DiscountPercent = model.DiscountPercent,
                    Status = status
                };

                _context.Promotions.Add(promotion);
                await _context.SaveChangesAsync();

                var productsToUpdate = _context.Products
                    .Where(p => model.SelectedProductIds.Contains(p.ProductId))
                    .ToList();

                foreach (var product in productsToUpdate)
                {
                    product.PromotionId = promotion.PromotionId;
                    product.DiscountPrice = product.Price * (1 - model.DiscountPercent / 100);
                }

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra khi tạo khuyến mãi: " + ex.Message);

                ViewBag.Categories = _context.Categories.ToList();

                model.AvailableProducts = _context.Products
                    .Where(p => p.PromotionId == null)
                    .ToList();

                return View(model);
            }
        }

        // GET: Admin/Promotions/Details/5 -> trả về view Edit luôn
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null) return NotFound();

            ViewBag.Categories = _context.Categories.ToList();

            var model = new PromotionViewModel
            {
                PromotionId = promo.PromotionId,
                PromotionName = promo.PromotionName,
                StartDate = promo.StartDate,
                EndDate = promo.EndDate,
                DiscountPercent = promo.DiscountPercent,
                Status = promo.Status,
                AvailableProducts = _context.Products
                    .Where(p => p.PromotionId == null || p.PromotionId == promo.PromotionId)
                    .ToList(),
                SelectedProductIds = _context.Products
                    .Where(p => p.PromotionId == promo.PromotionId)
                    .Select(p => p.ProductId)
                    .ToList()
            };

            return View("Edit", model);
        }

        // POST: Admin/Promotions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PromotionViewModel model)
        {
            if (id != model.PromotionId)
                return NotFound();

            if (model.EndDate < model.StartDate)
            {
                ModelState.AddModelError("", "Ngày kết thúc phải sau ngày bắt đầu.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _context.Categories.ToList();

                model.AvailableProducts = _context.Products
                    .Where(p => p.PromotionId == null || p.PromotionId == model.PromotionId)
                    .ToList();

                return View(model);
            }

            var promo = await _context.Promotions.FindAsync(id);
            if (promo == null) return NotFound();

            promo.PromotionName = model.PromotionName;
            promo.StartDate = model.StartDate;
            promo.EndDate = model.EndDate;
            promo.DiscountPercent = model.DiscountPercent;

            var now = DateTime.Now;
            promo.Status = now < model.StartDate ? "Sắp diễn ra"
                          : now > model.EndDate ? "Hết hạn"
                          : "Còn hạn";

            _context.Promotions.Update(promo);

            var currentProducts = _context.Products.Where(p => p.PromotionId == id).ToList();
            var selectedIds = model.SelectedProductIds ?? new List<int>();

            // Gỡ sản phẩm cũ không còn chọn
            foreach (var p in currentProducts.Where(p => !selectedIds.Contains(p.ProductId)))
            {
                p.PromotionId = null;
                p.DiscountPrice = p.Price;
            }

            // Thêm sản phẩm mới được chọn
            var addedProducts = _context.Products
                .Where(p => selectedIds.Contains(p.ProductId) && p.PromotionId != id)
                .ToList();

            foreach (var p in addedProducts)
            {
                p.PromotionId = promo.PromotionId;
                p.DiscountPrice = p.Price * (1 - promo.DiscountPercent / 100);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Promotions/DeleteConfirmed/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var promo = await _context.Promotions.FindAsync(id);
            if (promo != null)
            {
                var products = _context.Products.Where(p => p.PromotionId == id).ToList();
                foreach (var p in products)
                {
                    p.PromotionId = null;
                    p.DiscountPrice = null;
                }

                _context.Promotions.Remove(promo);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Helper: cập nhật trạng thái khuyến mãi theo ngày
        private async Task UpdatePromotionStatusesAsync()
        {
            var now = DateTime.Now;
            var promotions = await _context.Promotions.ToListAsync();

            bool updated = false;
            foreach (var promo in promotions)
            {
                var status = now < promo.StartDate ? "Sắp diễn ra"
                            : now > promo.EndDate ? "Hết hạn"
                            : "Còn hạn";

                if (promo.Status != status)
                {
                    promo.Status = status;
                    _context.Promotions.Update(promo);
                    updated = true;
                }
            }

            if (updated)
                await _context.SaveChangesAsync();

        }
        private async Task SyncProductsWithPromotionStatusesAsync()
        {
            var promotions = await _context.Promotions.ToListAsync();

            foreach (var promo in promotions)
            {
                // Lấy tất cả sản phẩm liên quan đến khuyến mãi này
                var relatedProducts = _context.Products.Where(p => p.PromotionId == promo.PromotionId).ToList();

                if (promo.Status == "Còn hạn")
                {
                    // Cập nhật discount price cho sản phẩm có PromotionId đúng
                    foreach (var product in relatedProducts)
                    {
                        product.DiscountPrice = product.Price * (1 - promo.DiscountPercent / 100);
                    }
                }
                else if (promo.Status == "Sắp diễn ra")
                {
                    // Gán PromotionId cho các sản phẩm mà chưa được gán promotion
                    // Nếu có sản phẩm chưa gán promotion, gán PromotionId nhưng DiscountPrice = null
                    var productsWithoutPromo = _context.Products.Where(p => p.PromotionId == null).ToList();

                    // Nếu bạn muốn gán luôn tất cả sản phẩm theo promotion nào đó, bạn phải xác định danh sách sản phẩm áp dụng promotion.
                    // Ở đây tôi giả sử bạn muốn giữ nguyên danh sách sản phẩm đã chọn promotion,
                    // nên chỉ cần đảm bảo DiscountPrice = null với các sản phẩm đã được gán promotion

                    foreach (var product in relatedProducts)
                    {
                        product.DiscountPrice = null; // chưa áp dụng giảm giá
                    }
                }
                else if (promo.Status == "Hết hạn")
                {
                    // Gỡ hết PromotionId và DiscountPrice về null cho các sản phẩm
                    foreach (var product in relatedProducts)
                    {
                        product.PromotionId = null;
                        product.DiscountPrice = null;
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

    }
}