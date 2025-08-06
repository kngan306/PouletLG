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
    public class CategoryController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly DbpouletLgv5Context _context;
        private const int DefaultPageSize = 10;

        public CategoryController(DbpouletLgv5Context context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int GetCurrentRoleId()
            => int.Parse(User.FindFirstValue(ClaimTypes.Role) ?? "0");

        // GET: /Admin/Category
        public async Task<IActionResult> Index(string? filterCode, string? filterName, int page = 1)
        {
            var query = _context.Categories.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filterCode))
                query = query.Where(c => EF.Functions.Like(c.CategoryId.ToString(), $"%{filterCode}%"));

            if (!string.IsNullOrWhiteSpace(filterName))
                query = query.Where(c => EF.Functions.Like(c.CategoryName, $"%{filterName}%"));

            int total = await query.CountAsync();
            var list = await query
                .OrderBy(c => c.CategoryId)
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToListAsync();

            var vm = new CategoryViewModel
            {
                FilterCode = filterCode,
                FilterName = filterName,
                Page = page,
                PageSize = DefaultPageSize,
                TotalItems = total,
                Categories = list
            };

            return View(vm);
        }

        // GET: /Admin/Category/Create
        [HttpGet]
        public IActionResult Create()
        {
            if (GetCurrentRoleId() == 2)
                return RedirectToAction(nameof(Index));

            return View(new CategoryViewModel());
        }

        // POST: /Admin/Category/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryViewModel vm)
        {
            if (GetCurrentRoleId() == 2)
                return RedirectToAction(nameof(Index));

            if (!ModelState.IsValid)
                return View(vm);

            int currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            string? imagePath = null;

            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                string uploadDir = Path.Combine(_env.WebRootPath, "images/categories");
                Directory.CreateDirectory(uploadDir); // đảm bảo thư mục tồn tại
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(vm.ImageFile.FileName);
                string fullPath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await vm.ImageFile.CopyToAsync(stream);
                }

                imagePath = "/images/categories/" + fileName;
            }

            var entity = new Category
            {
                CategoryName = vm.CategoryName,
                Description = vm.Description,
                ImagePath = imagePath,
                BackgroundColor = vm.BackgroundColor,
                ButtonColor = vm.ButtonColor,
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(entity);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // GET: /Admin/Category/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var entity = await _context.Categories.FindAsync(id);
            if (entity == null) return NotFound();

            var vm = new CategoryViewModel
            {
                CategoryId = entity.CategoryId,
                CategoryName = entity.CategoryName,
                Description = entity.Description,
                ImagePath = entity.ImagePath,
                BackgroundColor = entity.BackgroundColor,
                ButtonColor = entity.ButtonColor,
                CreatedBy = entity.CreatedBy,
                CreatedAt = entity.CreatedAt
            };


            return View(vm); // View sẽ xử lý quyền hiển thị
        }

        // POST: /Admin/Category/Edit
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryViewModel vm)
        {
            if (GetCurrentRoleId() == 2)
                return RedirectToAction(nameof(Index));

            if (!ModelState.IsValid)
                return View(vm);

            var entity = await _context.Categories.FindAsync(vm.CategoryId);
            if (entity == null) return NotFound();

            entity.CategoryName = vm.CategoryName;
            entity.Description = vm.Description;
            entity.BackgroundColor = vm.BackgroundColor;
            entity.ButtonColor = vm.ButtonColor;

            if (vm.ImageFile != null && vm.ImageFile.Length > 0)
            {
                string uploadDir = Path.Combine(_env.WebRootPath, "images/categories");
                Directory.CreateDirectory(uploadDir);
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(vm.ImageFile.FileName);
                string fullPath = Path.Combine(uploadDir, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await vm.ImageFile.CopyToAsync(stream);
                }

                entity.ImagePath = "/images/categories/" + fileName;
            }

            _context.Categories.Update(entity);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // GET: /Admin/Category/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            if (GetCurrentRoleId() == 2)
                return RedirectToAction(nameof(Index));

            var cat = await _context.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (cat == null) return NotFound();
            return View(cat);
        }

        // POST: /Admin/Category/Delete/5
        [HttpPost, ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (GetCurrentRoleId() == 2)
                return RedirectToAction(nameof(Index));

            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return NotFound();

            bool inUse = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (inUse)
            {
                ModelState.AddModelError("", "Không thể xóa: danh mục này đang được sử dụng trong sản phẩm.");
                return View(cat);
            }

            _context.Categories.Remove(cat);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Category/DeleteSelected
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSelected(int[] selectedIds)
        {
            if (GetCurrentRoleId() == 2)
                return RedirectToAction(nameof(Index));

            if (selectedIds == null || selectedIds.Length == 0)
                return RedirectToAction(nameof(Index));

            var used = await _context.Products
                .Where(p => p.CategoryId.HasValue && selectedIds.Contains(p.CategoryId.Value))
                .Select(p => p.CategoryId.Value)
                .Distinct()
                .ToListAsync();

            if (used.Any())
            {
                TempData["BulkError"] = $"Không thể xóa {used.Count} danh mục vì đang được sử dụng.";
                return RedirectToAction(nameof(Index));
            }

            var toDelete = _context.Categories
                .Where(c => selectedIds.Contains(c.CategoryId));

            _context.Categories.RemoveRange(toDelete);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
