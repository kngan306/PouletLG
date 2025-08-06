using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLego.Areas.Admin.Models;
using WebLego.DataSet.GdrService;

namespace WebLego.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("Admin/[controller]/[action]/{id?}")]
    public class BannersController : Controller
    {
        private readonly DbpouletLgv5Context _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public BannersController(DbpouletLgv5Context context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Banners
        public async Task<IActionResult> Index()
        {
            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
                return Unauthorized();

            ViewBag.CurrentRoleId = currentUser.RoleId;

            var banners = await _context.HomeBanners
                .Include(b => b.CreatedByNavigation)
                .Select(b => new BannerViewModel
                {
                    BannerId = b.BannerId,
                    ImageUrl = b.ImageUrl,
                    IsActive = b.IsActive ?? false,
                    CreatedAt = b.CreatedAt ?? DateTime.Now,
                    CreatedByName = b.CreatedByNavigation.FullName
                })
                .ToListAsync();

            return View(banners);
        }

        // GET: Admin/Banners/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
                return Unauthorized();

            if (currentUser.RoleId != 3) // Chỉ Quản lý (RoleId = 3) được phép
                return Forbid("Chỉ Quản lý mới có quyền thêm banner.");

            return View();
        }

        // POST: Admin/Banners/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IsActive")] WebLego.DataSet.GdrService.HomeBanner banner, IFormFile imageFile)
        {
            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
            {
                ModelState.AddModelError("", "Không tìm thấy người dùng hiện tại.");
                return Unauthorized();
            }

            if (currentUser.RoleId != 3) // Chỉ Quản lý (RoleId = 3) được phép
                return Forbid("Chỉ Quản lý mới có quyền thêm banner.");

            // Debug ModelState và imageFile
            Console.WriteLine($"imageFile: {(imageFile != null ? imageFile.FileName : "null")}");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            foreach (var error in ModelState)
            {
                Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
            }

            // Xóa lỗi xác thực cho ImageUrl và CreatedByNavigation
            ModelState.Remove("ImageUrl");
            ModelState.Remove("CreatedByNavigation");

            if (imageFile == null)
            {
                ModelState.AddModelError("imageFile", "Vui lòng chọn một file ảnh.");
            }
            else
            {
                // Kiểm tra định dạng file ảnh
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("imageFile", "Chỉ chấp nhận file ảnh định dạng .jpg, .jpeg, .png, .gif.");
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "banners", fileName);

                    Directory.CreateDirectory(Path.Combine(_webHostEnvironment.WebRootPath, "images", "banners"));

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    banner.ImageUrl = $"/images/banners/{fileName}";
                    banner.CreatedBy = currentUser.UserId;
                    banner.CreatedAt = DateTime.Now;

                    _context.Add(banner);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Thêm banner thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving banner: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    ModelState.AddModelError("", $"Đã xảy ra lỗi khi lưu banner: {ex.Message}");
                }
            }

            return View(banner);
        }

        // GET: Admin/Banners/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
                return Unauthorized();

            var banner = await _context.HomeBanners
                .Include(b => b.CreatedByNavigation)
                .FirstOrDefaultAsync(b => b.BannerId == id);

            if (banner == null)
                return NotFound();

            // Kiểm tra file ảnh tồn tại
            ViewBag.ImageExists = !string.IsNullOrEmpty(banner.ImageUrl) &&
                System.IO.File.Exists(Path.Combine(_webHostEnvironment.WebRootPath, banner.ImageUrl.TrimStart('/')));
            Console.WriteLine($"Banner ImageUrl: {banner.ImageUrl}, ImageExists: {ViewBag.ImageExists}");

            return View(banner);
        }

        // POST: Admin/Banners/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
                return Unauthorized();

            if (currentUser.RoleId != 3) // Chỉ Quản lý
                return Forbid("Chỉ Quản lý mới có quyền xóa banner.");

            var banner = await _context.HomeBanners.FindAsync(id);
            if (banner != null)
            {
                try
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, banner.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                        System.IO.File.Delete(imagePath);

                    _context.HomeBanners.Remove(banner);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Xóa banner thành công!";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting banner: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xóa banner. Vui lòng thử lại.";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Banners/ToggleActive/5
        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
                return Unauthorized();

            if (currentUser.RoleId != 3) // Chỉ Quản lý
                return Forbid("Chỉ Quản lý mới có quyền thay đổi trạng thái banner.");

            var banner = await _context.HomeBanners.FindAsync(id);
            if (banner == null)
                return NotFound();

            banner.IsActive = !banner.IsActive ?? true;
            _context.Update(banner);

            await _context.SaveChangesAsync();
            return Json(new { success = true, isActive = banner.IsActive });
        }

        private bool BannerExists(int id)
        {
            return _context.HomeBanners.Any(e => e.BannerId == id);
        }
    }
}