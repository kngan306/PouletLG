using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebLego.Areas.Admin.Models;
using WebLego.DataSet.GdrService;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WebLego.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("Admin/[controller]/[action]/{id?}")]
    public class ProductController : Controller
    {
        private readonly DbpouletLgv5Context _context;
        private readonly IWebHostEnvironment _env;
        private const int DefaultPageSize = 10;

        public ProductController(DbpouletLgv5Context context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int GetCurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private int GetCurrentRoleId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.Role) ?? "0");

        // GET: /Admin/Product
        public async Task<IActionResult> Index(string? filterName, int? filterCategoryId, int page = 1)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Promotion)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filterName))
                query = query.Where(p => EF.Functions.Like(p.ProductName, $"%{filterName}%"));

            if (filterCategoryId.HasValue)
                query = query.Where(p => p.CategoryId == filterCategoryId.Value);

            int total = await query.CountAsync();

            var list = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToListAsync();

            var vm = new ProductViewModel
            {
                Products = list,
                FilterName = filterName,
                FilterCategoryId = filterCategoryId,
                Page = page,
                PageSize = DefaultPageSize,
                TotalItems = total,
                Categories = await _context.Categories.ToListAsync()
            };

            return View(vm);
        }


        // GET: /Admin/Product/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            if (GetCurrentRoleId() == 2)
                return RedirectToAction(nameof(Index));

            var categories = await _context.Categories.ToListAsync();
            var promotions = await _context.Promotions.ToListAsync();

            var vm = new ProductViewModel
            {
                CategoriesSelectList = categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName
                }).ToList(),

                //PromotionsSelectList = promotions.Select(p => new SelectListItem
                //{
                //    Value = p.PromotionId.ToString(),
                //    Text = p.PromotionName
                //}).ToList()
                PromotionId = null // mặc định không chọn Promotion khi tạo

            };

            return View(vm);
        }


        // POST: /Admin/Product/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel vm)
        {
            if (GetCurrentRoleId() == 2)
                return RedirectToAction(nameof(Index));

            if (!ModelState.IsValid)
            {
                vm.AllCategories = await _context.Categories.ToListAsync();
                vm.AllPromotions = await _context.Promotions.ToListAsync();
                return View(vm);
            }

            var product = new Product
            {
                ProductName = vm.ProductName,
                ProductDes = vm.ProductDes,
                Price = vm.Price,
                AgeRange = vm.AgeRange,
                PieceCount = vm.PieceCount,
                DiscountPrice = vm.DiscountPrice,
                StockQuantity = vm.StockQuantity ?? 0,
                IsFeatured = vm.IsFeatured,
                ProductStatus = vm.ProductStatus,
                CategoryId = vm.CategoryId,
                PromotionId = null, // Không cho gán khi tạo
                CreatedAt = DateTime.Now,
                CreatedBy = GetCurrentUserId()
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Upload ảnh
            if (vm.Images != null && vm.Images.Count > 0)
            {
                foreach (var img in vm.Images)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(img.FileName);
                    var path = Path.Combine(_env.WebRootPath, "uploads", "products", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                    using (var stream = new FileStream(path, FileMode.Create))
                        await img.CopyToAsync(stream);

                    var isMain = img == vm.Images.First(); // Ảnh đầu tiên là chính

                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.ProductId,
                        ImageUrl = $"/uploads/products/{fileName}",
                        IsMain = isMain
                    });
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Product/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();
            var categories = await _context.Categories.ToListAsync();
            var promotions = await _context.Promotions.ToListAsync();
            var vm = new ProductViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                ProductDes = product.ProductDes,
                Price = product.Price,
                AgeRange = product.AgeRange,
                PieceCount = product.PieceCount,
                StockQuantity = product.StockQuantity,
                DiscountPrice = product.DiscountPrice,
                ProductStatus = product.ProductStatus,
                IsFeatured = product.IsFeatured ?? false,
                CategoryId = product.CategoryId,
                PromotionId = product.PromotionId,
                PromotionName = product.Promotion?.PromotionName, // lấy tên khuyến mãi
                CreatedAt = product.CreatedAt ?? DateTime.Now,
                CreatedBy = product.CreatedBy,
                AllCategories = await _context.Categories.ToListAsync(),
                AllPromotions = await _context.Promotions.ToListAsync(),
                CategoriesSelectList = categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName,
                    Selected = product.CategoryId == c.CategoryId
                }).ToList(),

                PromotionsSelectList = promotions.Select(p => new SelectListItem
                {
                    Value = p.PromotionId.ToString(),
                    Text = p.PromotionName,
                    Selected = product.PromotionId == p.PromotionId
                }).ToList(),
                ExistingImageUrls = product.ProductImages.Select(i => i.ImageUrl).ToList(),
                MainImageUrl = product.ProductImages.FirstOrDefault(i => i.IsMain == true)?.ImageUrl
            };

            return View(vm);
        }

        // POST: /Admin/Product/Edit
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductViewModel vm)
        {
            if (GetCurrentRoleId() == 2)
                return RedirectToAction(nameof(Index));

            if (!ModelState.IsValid)
            {
                vm.AllCategories = await _context.Categories.ToListAsync();
                vm.AllPromotions = await _context.Promotions.ToListAsync();
                return View(vm);
            }

            var product = await _context.Products.FindAsync(vm.ProductId);
            if (product == null) return NotFound();

            product.ProductName = vm.ProductName;
            product.ProductDes = vm.ProductDes;
            product.Price = vm.Price;
            product.AgeRange = vm.AgeRange;
            product.PieceCount = vm.PieceCount;
            product.StockQuantity = vm.StockQuantity ?? 0;
            product.DiscountPrice = vm.DiscountPrice;
            product.IsFeatured = vm.IsFeatured;
            product.ProductStatus = vm.ProductStatus;
            product.CategoryId = vm.CategoryId;
            //product.PromotionId = vm.PromotionId;

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            // Upload thêm ảnh nếu có
            if (vm.Images != null && vm.Images.Count > 0)
            {
                foreach (var img in vm.Images)
                {
                    var fileName = Guid.NewGuid() + Path.GetExtension(img.FileName);
                    var path = Path.Combine(_env.WebRootPath, "uploads", "products", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                    using (var stream = new FileStream(path, FileMode.Create))
                        await img.CopyToAsync(stream);

                    var isMain = img == vm.Images.First();

                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.ProductId,
                        ImageUrl = $"/uploads/products/{fileName}",
                        IsMain = isMain
                    });
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Product/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();
            return View(product);
        }

        // POST: /Admin/Product/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            var images = _context.ProductImages.Where(i => i.ProductId == id);
            _context.ProductImages.RemoveRange(images);

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Product/DeleteSelected
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(int[] selectedIds)
        {
            foreach (var id in selectedIds)
            {
                var product = await _context.Products
                                .Include(p => p.ProductImages) // load ảnh liên quan
                                .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product != null)
                {
                    // Xóa ảnh trước
                    if (product.ProductImages != null)
                    {
                        _context.ProductImages.RemoveRange(product.ProductImages);
                    }

                    // Xóa sản phẩm
                    _context.Products.Remove(product);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

    }
}
