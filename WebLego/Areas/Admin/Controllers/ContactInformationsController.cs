using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLego.Areas.Admin.Models;
using WebLego.DataSet.GdrService;

namespace WebLego.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("Admin/[controller]/[action]/{id?}")]
    public class ContactInformationsController : Controller
    {
        private readonly DbpouletLgv5Context _context;

        public ContactInformationsController(DbpouletLgv5Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
                return Unauthorized();

            ViewBag.CurrentRoleId = currentUser.RoleId;

            var contacts = await _context.ContactInformations
                .Include(c => c.CreatedByNavigation)
                .Select(c => new ContactViewModel
                {
                    ContactId = c.ContactId,
                    PhoneNumber = c.PhoneNumber,
                    Address = c.Address,
                    Email = c.Email,
                    IsActive = c.IsActive ?? false,
                    CreatedAt = c.CreatedAt ?? DateTime.Now,
                    CreatedByName = c.CreatedByNavigation.FullName,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude
                })
                .ToListAsync();

            return View(contacts);
        }

        public async Task<IActionResult> Create()
        {
            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
                return Unauthorized();

            if (currentUser.RoleId != 3)
                return Forbid("Chỉ Quản lý mới có quyền thêm thông tin liên hệ.");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ContactViewModel model)
        {
            Console.WriteLine($"Starting Create action - User.Identity.Name: {User.Identity.Name}");
            Console.WriteLine($"Received model: PhoneNumber={model.PhoneNumber ?? "null"}, Address={model.Address ?? "null"}, Email={model.Email ?? "null"}, IsActive={model.IsActive}, Latitude={model.Latitude}, Longitude={model.Longitude}");

            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
            {
                Console.WriteLine("Không tìm thấy người dùng với FullName: " + User.Identity.Name);
                return Unauthorized();
            }

            Console.WriteLine($"Current User ID: {currentUser.UserId}, RoleId: {currentUser.RoleId}");
            if (currentUser.RoleId != 3)
                return Forbid("Chỉ Quản lý mới có quyền thêm thông tin liên hệ.");

            ModelState.Remove("CreatedByName");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is invalid");
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                return View(model);
            }

            var contact = new ContactInformation
            {
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                Email = model.Email,
                IsActive = model.IsActive,
                CreatedBy = currentUser.UserId,
                CreatedAt = DateTime.Now,
                Latitude = model.Latitude,
                Longitude = model.Longitude
            };

            try
            {
                _context.Add(contact);
                await _context.SaveChangesAsync();
                Console.WriteLine("SaveChanges completed successfully");
                TempData["SuccessMessage"] = "Thêm thông tin liên hệ thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving contact: {ex.Message}\nStackTrace: {ex.StackTrace}");
                ModelState.AddModelError("", $"Đã xảy ra lỗi khi lưu: {ex.Message}");
                return View(model);
            }
        }

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
                return Forbid("Chỉ Quản lý mới có quyền chỉnh sửa thông tin liên hệ.");

            var contact = await _context.ContactInformations
                .Include(c => c.CreatedByNavigation)
                .FirstOrDefaultAsync(c => c.ContactId == id);

            if (contact == null)
                return NotFound();

            var viewModel = new ContactViewModel
            {
                ContactId = contact.ContactId,
                PhoneNumber = contact.PhoneNumber,
                Address = contact.Address,
                Email = contact.Email,
                IsActive = contact.IsActive ?? false,
                CreatedAt = contact.CreatedAt ?? DateTime.Now,
                CreatedByName = contact.CreatedByNavigation?.FullName ?? "Unknown",
                Latitude = contact.Latitude,
                Longitude = contact.Longitude
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ContactViewModel model)
        {
            if (id != model.ContactId)
                return NotFound();

            var currentUser = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);

            if (currentUser == null)
                return Unauthorized();

            if (currentUser.RoleId != 3)
                return Forbid("Chỉ Quản lý mới có quyền chỉnh sửa thông tin liên hệ.");

            Console.WriteLine($"Starting Edit action - User.Identity.Name: {User.Identity.Name}");
            Console.WriteLine($"Received model: PhoneNumber={model.PhoneNumber ?? "null"}, Address={model.Address ?? "null"}, Email={model.Email ?? "null"}, IsActive={model.IsActive}, Latitude={model.Latitude}, Longitude={model.Longitude}");

            ModelState.Remove("CreatedByName");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("ModelState is invalid");
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
                return View(model);
            }

            try
            {
                var contact = await _context.ContactInformations.FindAsync(id);
                if (contact == null)
                    return NotFound();

                contact.PhoneNumber = model.PhoneNumber;
                contact.Address = model.Address;
                contact.Email = model.Email;
                contact.IsActive = model.IsActive;
                contact.Latitude = model.Latitude;
                contact.Longitude = model.Longitude;

                await _context.SaveChangesAsync();
                Console.WriteLine($"Update completed for contact ID: {id}");
                TempData["SuccessMessage"] = "Cập nhật thông tin liên hệ thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating contact: {ex.Message}\nStackTrace: {ex.StackTrace}");
                ModelState.AddModelError("", $"Đã xảy ra lỗi khi cập nhật: {ex.Message}");
                return View(model);
            }
        }

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
                return Forbid("Chỉ Quản lý mới có quyền xóa thông tin liên hệ.");

            var contact = await _context.ContactInformations.FindAsync(id);
            if (contact != null)
            {
                _context.ContactInformations.Remove(contact);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa thông tin liên hệ thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(int id)
        {
            return _context.ContactInformations.Any(e => e.ContactId == id);
        }
    }
}