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
    public class AboutUsSectionsController : Controller
    {
        private readonly DbpouletLgv5Context _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AboutUsSectionsController(DbpouletLgv5Context context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/AboutUsSections
        public async Task<IActionResult> Index()
        {
            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
                return Unauthorized();

            ViewBag.CurrentRoleId = currentUser.RoleId;

            var sections = await _context.AboutUsSections
                .Include(s => s.CreatedByNavigation)
                .Select(s => new AboutUsSectionViewModel
                {
                    SectionId = s.SectionId,
                    Title = s.Title,
                    Description = s.Description,
                    ImageUrl = s.ImageUrl,
                    IsActive = s.IsActive ?? false,
                    DisplayOrder = s.DisplayOrder,
                    CreatedAt = s.CreatedAt ?? DateTime.Now,
                    CreatedByName = s.CreatedByNavigation.FullName
                })
                .OrderBy(s => s.DisplayOrder)
                .ToListAsync();

            return View(sections);
        }

        // GET: Admin/AboutUsSections/Create
        public async Task<IActionResult> Create()
        {
            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
                return Unauthorized();

            if (currentUser.RoleId != 3)
                return Forbid("Chỉ Quản lý mới có quyền thêm nội dung trang Giới thiệu.");

            return View();
        }

        // POST: Admin/AboutUsSections/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Description,IsActive,DisplayOrder")] AboutUsSection section, IFormFile imageFile)
        {
            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
            {
                ModelState.AddModelError("", "Không tìm thấy người dùng hiện tại.");
                return Unauthorized();
            }

            if (currentUser.RoleId != 3)
                return Forbid("Chỉ Quản lý mới có quyền thêm nội dung trang Giới thiệu.");

            ModelState.Remove("ImageUrl");
            ModelState.Remove("CreatedByNavigation");

            if (imageFile == null)
            {
                ModelState.AddModelError("imageFile", "Vui lòng chọn một file ảnh.");
            }
            else
            {
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
                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "about-us", fileName);

                    Directory.CreateDirectory(Path.Combine(_webHostEnvironment.WebRootPath, "images", "about-us"));

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    section.ImageUrl = $"/images/about-us/{fileName}";
                    section.CreatedBy = currentUser.UserId;
                    section.CreatedAt = DateTime.Now;

                    _context.Add(section);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Thêm nội dung thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Đã xảy ra lỗi khi lưu nội dung: {ex.Message}");
                }
            }

            return View(section);
        }

        // GET: Admin/AboutUsSections/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
                return Unauthorized();

            var section = await _context.AboutUsSections
                .Include(s => s.CreatedByNavigation)
                .FirstOrDefaultAsync(s => s.SectionId == id);

            if (section == null)
                return NotFound();

            ViewBag.ImageExists = !string.IsNullOrEmpty(section.ImageUrl) &&
                System.IO.File.Exists(Path.Combine(_webHostEnvironment.WebRootPath, section.ImageUrl.TrimStart('/')));

            return View(section);
        }

        // GET: Admin/AboutUsSections/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
                return Unauthorized();

            if (currentUser.RoleId != 3)
                return Forbid("Chỉ Quản lý mới có quyền chỉnh sửa nội dung trang Giới thiệu.");

            var section = await _context.AboutUsSections.FindAsync(id);
            if (section == null)
                return NotFound();

            ViewBag.ImageExists = !string.IsNullOrEmpty(section.ImageUrl) &&
                System.IO.File.Exists(Path.Combine(_webHostEnvironment.WebRootPath, section.ImageUrl.TrimStart('/')));

            return View(section);
        }

        // POST: Admin/AboutUsSections/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("SectionId,Title,Description,IsActive,DisplayOrder")] AboutUsSection section, IFormFile imageFile)
        {
            if (id != section.SectionId)
                return NotFound();

            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
                return Unauthorized();

            if (currentUser.RoleId != 3)
                return Forbid("Chỉ Quản lý mới có quyền chỉnh sửa nội dung trang Giới thiệu.");

            ModelState.Remove("ImageUrl");
            ModelState.Remove("CreatedByNavigation");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingSection = await _context.AboutUsSections.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.SectionId == id);

                    if (existingSection == null)
                        return NotFound();

                    if (imageFile != null)
                    {
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("imageFile", "Chỉ chấp nhận file ảnh định dạng .jpg, .jpeg, .png, .gif.");
                            return View(section);
                        }

                        var fileName = Guid.NewGuid().ToString() + extension;
                        var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "about-us", fileName);

                        Directory.CreateDirectory(Path.Combine(_webHostEnvironment.WebRootPath, "images", "about-us"));

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        if (!string.IsNullOrEmpty(existingSection.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingSection.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                                System.IO.File.Delete(oldImagePath);
                        }

                        section.ImageUrl = $"/images/about-us/{fileName}";
                    }
                    else
                    {
                        section.ImageUrl = existingSection.ImageUrl;
                    }

                    section.CreatedBy = existingSection.CreatedBy;
                    section.CreatedAt = existingSection.CreatedAt;

                    _context.Update(section);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Chỉnh sửa nội dung thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Đã xảy ra lỗi khi chỉnh sửa nội dung: {ex.Message}");
                }
            }

            ViewBag.ImageExists = !string.IsNullOrEmpty(section.ImageUrl) &&
                System.IO.File.Exists(Path.Combine(_webHostEnvironment.WebRootPath, section.ImageUrl.TrimStart('/')));
            return View(section);
        }

        // POST: Admin/AboutUsSections/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
                return Unauthorized();

            if (currentUser.RoleId != 3)
                return Forbid("Chỉ Quản lý mới có quyền xóa nội dung trang Giới thiệu.");

            var section = await _context.AboutUsSections.FindAsync(id);
            if (section != null)
            {
                try
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, section.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                        System.IO.File.Delete(imagePath);

                    _context.AboutUsSections.Remove(section);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Xóa nội dung thành công!";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Đã xảy ra lỗi khi xóa nội dung: {ex.Message}";
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/AboutUsSections/ToggleActive/5
        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
                return Unauthorized();

            if (currentUser.RoleId != 3)
                return Forbid("Chỉ Quản lý mới có quyền thay đổi trạng thái nội dung.");

            var section = await _context.AboutUsSections.FindAsync(id);
            if (section == null)
                return NotFound();

            section.IsActive = !section.IsActive ?? true;
            _context.Update(section);

            await _context.SaveChangesAsync();
            return Json(new { success = true, isActive = section.IsActive });
        }

        private bool AboutUsSectionExists(int id)
        {
            return _context.AboutUsSections.Any(e => e.SectionId == id);
        }
    }
}