using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebLego.Areas.Admin.Models;
using WebLego.DataSet.GdrService;

namespace WebLego.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("Admin/[controller]/[action]/{id?}")]
    public class AccountController : Controller
    {
        private readonly DbpouletLgv5Context _context;
        private const int DefaultPageSize = 10;

        public AccountController(DbpouletLgv5Context context)
        {
            _context = context;
        }

        private int GetCurrentRoleId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.Role) ?? "0");

        public async Task<IActionResult> Index(string? searchName, string? searchEmail, int? filterRoleId,
            string? filterStatus, string? filterGender, string? filterRank, int page = 1, string? tab = null)
        {
            var allAccounts = _context.Users
                .Include(u => u.Role)
                .Include(u => u.CustomerProfile)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchName))
                allAccounts = allAccounts.Where(a => a.FullName.Contains(searchName));
            if (!string.IsNullOrEmpty(searchEmail))
                allAccounts = allAccounts.Where(a => a.Email.Contains(searchEmail));
            if (filterRoleId.HasValue)
                allAccounts = allAccounts.Where(a => a.RoleId == filterRoleId);
            if (!string.IsNullOrEmpty(filterStatus))
                allAccounts = allAccounts.Where(a => a.UserStatus == filterStatus);
            if (!string.IsNullOrEmpty(filterGender))
                allAccounts = allAccounts.Where(a => a.Gender == filterGender);
            if (!string.IsNullOrEmpty(filterRank))
                allAccounts = allAccounts.Where(a =>
                    a.Role.RoleName == "Khách hàng" &&
                    a.CustomerProfile != null &&
                    a.CustomerProfile.CustomerRank == filterRank);

            var pagedAccounts = await allAccounts
                .OrderBy(a => a.UserId)
                .Skip((page - 1) * DefaultPageSize)
                .Take(DefaultPageSize)
                .ToListAsync();

            var vm = new AccountViewModel
            {
                SearchName = searchName,
                SearchEmail = searchEmail,
                FilterRoleId = filterRoleId,
                FilterStatus = filterStatus,
                FilterGender = filterGender,
                FilterRank = filterRank,
                Page = page,
                PageSize = DefaultPageSize,
                TotalItems = await allAccounts.CountAsync(),
                Accounts = pagedAccounts,
                RoleOptions = new SelectList(await _context.Roles.ToListAsync(), "RoleId", "RoleName"),
                StatusOptions = new SelectList(new[] { "Hoạt động", "Tạm khóa" }),
                GenderOptions = new SelectList(new[] { "Nam", "Nữ" }),
                RankOptions = new SelectList(new[] { "Đồng", "Bạc", "Vàng" })
            };

            ViewBag.CustomerTab = pagedAccounts.Where(a => a.RoleId == 1).ToList();
            ViewBag.StaffTab = pagedAccounts.Where(a => a.RoleId != 1).ToList();

            return View(vm);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (GetCurrentRoleId() != 3) return Forbid();

            var vm = new CreateAccountViewModel
            {
                RoleOptions = new SelectList(_context.Roles.Where(r => r.RoleId != 1).ToList(), "RoleId", "RoleName"),
                StatusOptions = new SelectList(new[] { "Hoạt động", "Tạm khóa" }),
                GenderOptions = new SelectList(new[] { "Nam", "Nữ" })
            };
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAccountViewModel vm)
        {
            if (GetCurrentRoleId() != 3)
                return Forbid();

            // Xóa các lỗi validation không liên quan
            ModelState.Remove("RoleName");
            ModelState.Remove("DiscountCode");
            ModelState.Remove("CustomerRank");
            ModelState.Remove("SearchName");
            ModelState.Remove("SearchEmail");
            ModelState.Remove("FilterRoleId");
            ModelState.Remove("FilterStatus");
            ModelState.Remove("FilterGender");
            ModelState.Remove("FilterRank");
            ModelState.Remove("RoleOptions");
            ModelState.Remove("StatusOptions");
            ModelState.Remove("GenderOptions");
            ModelState.Remove("RankOptions");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                Console.WriteLine("ModelState Errors: " + string.Join("; ", errors));

                vm.RoleOptions = new SelectList(_context.Roles.Where(r => r.RoleId != 1).ToList(), "RoleId", "RoleName");
                vm.StatusOptions = new SelectList(new[] { "Hoạt động", "Tạm khóa" });
                vm.GenderOptions = new SelectList(new[] { "Nam", "Nữ" });
                return View(vm);
            }

            if (vm.RoleId == 1)
            {
                ModelState.AddModelError("", "Không thể tạo tài khoản khách hàng.");
                vm.RoleOptions = new SelectList(_context.Roles.Where(r => r.RoleId != 1).ToList(), "RoleId", "RoleName");
                vm.StatusOptions = new SelectList(new[] { "Hoạt động", "Tạm khóa" });
                vm.GenderOptions = new SelectList(new[] { "Nam", "Nữ" });
                return View(vm);
            }

            if (await _context.Users.AnyAsync(u => u.Email == vm.Email))
            {
                ModelState.AddModelError("Email", "Email đã tồn tại.");
                vm.RoleOptions = new SelectList(_context.Roles.Where(r => r.RoleId != 1).ToList(), "RoleId", "RoleName");
                vm.StatusOptions = new SelectList(new[] { "Hoạt động", "Tạm khóa" });
                vm.GenderOptions = new SelectList(new[] { "Nam", "Nữ" });
                return View(vm);
            }

            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(vm.UserPassword);

            var user = new User
            {
                FullName = vm.FullName,
                Email = vm.Email,
                Phone = vm.Phone,
                Gender = vm.Gender,
                DateOfBirth = vm.DateOfBirth,
                UserPassword = hashedPassword,
                RoleId = vm.RoleId,
                UserStatus = vm.UserStatus,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Tài khoản đã được tạo thành công.";
                return RedirectToAction(nameof(Index), new { tab = "Staff" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving user: {ex.Message}, StackTrace: {ex.StackTrace}");
                ModelState.AddModelError("", $"Lỗi khi tạo tài khoản: {ex.Message}");
                vm.RoleOptions = new SelectList(_context.Roles.Where(r => r.RoleId != 1).ToList(), "RoleId", "RoleName");
                vm.StatusOptions = new SelectList(new[] { "Hoạt động", "Tạm khóa" });
                vm.GenderOptions = new SelectList(new[] { "Nam", "Nữ" });
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? tab = null)
        {
            var entity = await _context.Users
                .Include(u => u.CustomerProfile)
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (entity == null)
                return NotFound();

            if (entity.RoleId == 1 && GetCurrentRoleId() != 3)
                return Forbid();

            var vm = new AccountViewModel
            {
                UserId = entity.UserId,
                FullName = entity.FullName,
                Email = entity.Email,
                Phone = entity.Phone,
                Gender = entity.Gender,
                DateOfBirth = entity.DateOfBirth,
                RoleId = entity.RoleId,
                RoleName = entity.Role?.RoleName,
                UserStatus = entity.UserStatus,
                DiscountCode = entity.CustomerProfile?.DiscountCode,
                CustomerRank = entity.CustomerProfile?.CustomerRank,
                CustomerProfile = entity.CustomerProfile,
                CreatedAt = entity.CreatedAt ?? DateTime.Now,

                RoleOptions = new SelectList(await _context.Roles.ToListAsync(), "RoleId", "RoleName", entity.RoleId),
                StatusOptions = new SelectList(new[] { "Hoạt động", "Tạm khóa" }, entity.UserStatus),
                GenderOptions = new SelectList(new[] { "Nam", "Nữ" }, entity.Gender),
                RankOptions = new SelectList(new[] { "Đồng", "Bạc", "Vàng" }, entity.CustomerProfile?.CustomerRank)
            };

            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AccountViewModel vm, string? tab = null)
        {
            var currentRoleId = GetCurrentRoleId();

            if (!ModelState.IsValid)
            {
                PopulateSelectLists(vm);
                return View(vm);
            }

            var entity = await _context.Users
                .Include(u => u.CustomerProfile)
                .FirstOrDefaultAsync(u => u.UserId == vm.UserId);

            if (entity == null)
                return NotFound();

            if (entity.RoleId == 1 && currentRoleId == 3)
                return Forbid();

            if (entity.RoleId == 1)
            {
                entity.UserStatus = vm.UserStatus;

                if (entity.CustomerProfile == null)
                {
                    entity.CustomerProfile = new CustomerProfile
                    {
                        CustomerId = entity.UserId,
                        DiscountCode = vm.DiscountCode,
                        CustomerRank = vm.CustomerRank
                    };
                    _context.CustomerProfiles.Add(entity.CustomerProfile);
                }
                else
                {
                    entity.CustomerProfile.DiscountCode = vm.DiscountCode;
                    entity.CustomerProfile.CustomerRank = vm.CustomerRank;
                    _context.CustomerProfiles.Update(entity.CustomerProfile);
                }

                _context.Users.Update(entity);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { tab = "Customer" });
            }

            if (currentRoleId == 3)
            {
                entity.FullName = vm.FullName;
                entity.RoleId = vm.RoleId;
            }

            entity.Email = vm.Email;
            entity.Phone = vm.Phone;
            entity.Gender = vm.Gender;
            entity.DateOfBirth = vm.DateOfBirth;
            entity.UserStatus = vm.UserStatus;

            _context.Users.Update(entity);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { tab = "Staff" });
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.RoleId == 1)
                return Forbid();

            return View(user);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.RoleId == 1) return Forbid();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteSelected(int[] selectedIds, string tab)
        {
            if (selectedIds == null || selectedIds.Length == 0)
            {
                TempData["BulkError"] = "Vui lòng chọn ít nhất một tài khoản để xóa.";
                return RedirectToAction("Index", new { tab });
            }

            var undeletableUsers = new List<string>();
            foreach (var id in selectedIds)
            {
                var user = _context.Users.FirstOrDefault(u => u.UserId == id);
                if (user != null)
                {
                    if (user.RoleId == 1)
                    {
                        undeletableUsers.Add($"{user.FullName} ({user.Email})");
                        continue;
                    }

                    _context.Users.Remove(user);
                }
            }

            _context.SaveChanges();

            if (undeletableUsers.Count > 0)
            {
                TempData["BulkError"] = $"Không thể xóa các tài khoản khách hàng: {string.Join(", ", undeletableUsers)}";
            }

            return RedirectToAction("Index", new { tab });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int userId, string status, string tab)
        {
            if (GetCurrentRoleId() != 3)
                return Forbid();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                return NotFound();

            if (!new[] { "Hoạt động", "Tạm khóa" }.Contains(status))
                return BadRequest("Trạng thái không hợp lệ.");

            user.UserStatus = status;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Cập nhật trạng thái tài khoản {user.FullName} thành công.";
            return RedirectToAction(nameof(Index), new { tab });
        }

        private void PopulateSelectLists(AccountViewModel vm)
        {
            vm.RoleOptions = new SelectList(_context.Roles.Where(r => r.RoleId != 1).ToList(), "RoleId", "RoleName");
            vm.StatusOptions = new SelectList(new[] { "Hoạt động", "Tạm khóa" });
            vm.GenderOptions = new SelectList(new[] { "Nam", "Nữ" });
        }
    }
}