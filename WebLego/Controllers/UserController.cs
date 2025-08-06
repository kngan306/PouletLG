using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLego.DataSet.GdrService;
using WebLego.Models.ViewModel;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using System.Text.RegularExpressions;
using WebLego.Services;
using System.Threading.Tasks;

namespace WebLego.Controllers
{
    public class UserController : Controller
    {
        private readonly DbpouletLgv5Context _context;
        private readonly MembershipService _membershipService;

        public UserController(DbpouletLgv5Context context, MembershipService membershipService)
        {
            _context = context;
            _membershipService = membershipService;
        }

        // =================== THÔNG TIN TÀI KHOẢN ===================
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var customer = (from u in _context.Users
                            join c in _context.CustomerProfiles on u.UserId equals c.CustomerId
                            where u.UserId == userId
                            select new CustomerInfoViewModel
                            {
                                FullName = u.FullName,
                                Email = u.Email,
                                Phone = u.Phone,
                                Gender = u.Gender,
                                DateOfBirth = u.DateOfBirth,
                                CustomerRank = c.CustomerRank,
                                DiscountCode = c.DiscountCode
                            }).FirstOrDefault();

            if (customer == null) return NotFound();

            return View(customer);
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken] // nên có để khớp token với client
        //public async Task<IActionResult> ConfirmDeleteAccount(string password)
        //{
        //    var userId = HttpContext.Session.GetInt32("UserId");
        //    if (userId == null)
        //        return Json(new { success = false, message = "Bạn chưa đăng nhập." });

        //    var user = await _context.Users.FindAsync(userId);
        //    if (user == null)
        //        return Json(new { success = false, message = "Tài khoản không tồn tại." });

        //    if (!BCrypt.Net.BCrypt.Verify(password, user.UserPassword))
        //        return Json(new { success = false, message = "Mật khẩu không chính xác." });

        //    _context.Users.Remove(user);
        //    await _context.SaveChangesAsync();

        //    await HttpContext.SignOutAsync();
        //    HttpContext.Session.Clear();

        //    return Json(new { success = true, message = "Tài khoản đã được xóa thành công." });
        //}


        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return Json(new { success = false, message = "Vui lòng đăng nhập lại." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy người dùng." });

            // So sánh mật khẩu cũ
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.UserPassword))
            {
                return Json(new { success = false, message = "Mật khẩu cũ không chính xác." });
            }

            // Mật khẩu mới giống mật khẩu cũ
            if (BCrypt.Net.BCrypt.Verify(newPassword, user.UserPassword))
            {
                return Json(new { success = false, message = "Mật khẩu mới không được trùng với mật khẩu cũ." });
            }

            // Mật khẩu xác nhận không khớp
            if (newPassword != confirmPassword)
            {
                return Json(new { success = false, message = "Mật khẩu xác nhận không khớp." });
            }

            // Kiểm tra điều kiện độ mạnh mật khẩu
            if (!IsValidPassword(newPassword))
            {
                return Json(new
                {
                    success = false,
                    message = "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt."
                });
            }

            // Nếu mọi thứ hợp lệ, cập nhật mật khẩu
            user.UserPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
        }
        private bool IsValidPassword(string password)
        {
            return password.Length >= 8 &&
                   Regex.IsMatch(password, @"[A-Z]") &&     // ít nhất 1 chữ hoa
                   Regex.IsMatch(password, @"[a-z]") &&     // ít nhất 1 chữ thường
                   Regex.IsMatch(password, @"\d") &&        // ít nhất 1 số
                   Regex.IsMatch(password, @"[\W_]");       // ít nhất 1 ký tự đặc biệt
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(CustomerInfoViewModel model)
        {
            // Lấy userId từ session hoặc claims
            int? userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // Cập nhật thông tin
            user.FullName = model.FullName;
            user.Phone = model.Phone;
            user.Gender = model.Gender;
            user.DateOfBirth = model.DateOfBirth;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("Profile");
        }





        // =================== YÊU THÍCH ===================
        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int productId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để yêu thích sản phẩm." });
            }

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

            if (favorite == null)
            {
                _context.Favorites.Add(new Favorite { UserId = userId.Value, ProductId = productId });
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = true });
            }
            else
            {
                _context.Favorites.Remove(favorite);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = false });
            }
        }

        [HttpGet]
        public IActionResult CheckFavorite(int productId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, isFavorite = false });
            }

            var isFavorite = _context.Favorites.Any(f => f.UserId == userId && f.ProductId == productId);
            return Json(new { success = true, isFavorite = isFavorite });
        }

        public async Task<IActionResult> Favorites()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Product)
                .ThenInclude(p => p.ProductImages)
                .Select(f => f.Product)
                .ToListAsync();

            return View(favorites);
        }

        // =================== ĐỊA CHỈ ===================

        public IActionResult Addresses()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var addresses = _context.UserAddresses
                .Where(a => a.UserId == userId)
                .Select(a => new AddressViewModel
                {
                    AddressId = a.AddressId,
                    FullName = a.FullName,
                    Phone = a.Phone,
                    Province = a.Province,
                    District = a.District,
                    Ward = a.Ward,
                    SpecificAddress = a.SpecificAddress,
                    AddressType = a.AddressType,
                    IsDefault = a.IsDefault ?? false
                }).ToList();

            return View(addresses); // => Views/User/Addresses.cshtml
        }

        [HttpPost]
        public IActionResult SetDefaultAddress(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            var addresses = _context.UserAddresses.Where(a => a.UserId == userId);
            foreach (var addr in addresses)
                addr.IsDefault = addr.AddressId == id;

            _context.SaveChanges();
            return RedirectToAction("Addresses");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddAddress(AddressViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            var address = new UserAddress
            {
                UserId = userId.Value,
                FullName = model.FullName,
                Phone = model.Phone,
                Province = model.Province,
                District = model.District,
                Ward = model.Ward,
                SpecificAddress = model.SpecificAddress,
                AddressType = model.AddressType,
                IsDefault = false
            };

            _context.UserAddresses.Add(address);
            _context.SaveChanges();
            return RedirectToAction("Addresses");
        }
        [HttpPost]
        public IActionResult DeleteAddress(int id)
        {
            var address = _context.UserAddresses.Find(id);
            if (address == null) return NotFound();

            _context.UserAddresses.Remove(address);
            _context.SaveChanges();
            return RedirectToAction("Addresses");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateAddress(AddressViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return Unauthorized();

            var address = _context.UserAddresses.FirstOrDefault(a => a.AddressId == model.AddressId && a.UserId == userId);
            if (address == null) return NotFound();

            address.FullName = model.FullName;
            address.Phone = model.Phone;
            address.Province = model.Province;
            address.District = model.District;
            address.Ward = model.Ward;
            address.SpecificAddress = model.SpecificAddress;
            address.AddressType = model.AddressType;

            _context.SaveChanges();

            return RedirectToAction("Addresses");
        }

        // =================== HẠNG THÀNH VIÊN ===================
        public async Task<IActionResult> Membership()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            // Lấy thông tin khách hàng
            var customer = _context.CustomerProfiles
                .FirstOrDefault(c => c.CustomerId == userId);

            if (customer == null) return NotFound();

            // Kiểm tra và cập nhật hạng thành viên
            await _membershipService.CheckAndUpdateMembershipAsync(userId.Value);

            // Lấy số đơn hàng hoàn thành và tổng tiền chi tiêu
            var completedOrders = _context.Orders
                .Where(o => o.UserId == userId && o.OrderStatus == "Hoàn thành")
                .Count();

            var totalSpent = _context.Orders
                .Where(o => o.UserId == userId && o.OrderStatus == "Hoàn thành")
                .Sum(o => o.TotalAmount) ?? 0;

            // Tạo danh sách các hạng thành viên
            var membershipTiers = new List<MembershipTierViewModel>
            {
                new MembershipTierViewModel
                {
                    Name = "Đồng",
                    Description = "Hạng mặc định khi tạo tài khoản.",
                    RequiredAmount = 0,
                    SpentPercentage = 100 // Luôn đạt hạng Đồng
                },
                new MembershipTierViewModel
                {
                    Name = "Bạc",
                    Description = "Cần tích lũy đạt từ 5,000,000 VNĐ.",
                    RequiredAmount = 5000000,
                    SpentPercentage = totalSpent >= 5000000 ? 100 : (totalSpent / 5000000m * 100)
                },
                new MembershipTierViewModel
                {
                    Name = "Vàng",
                    Description = "Cần tích lũy đạt từ 10,000,000 VNĐ.",
                    RequiredAmount = 10000000,
                    SpentPercentage = totalSpent >= 10000000 ? 100 : (totalSpent / 10000000m * 100)
                }
            };

            var model = new MembershipViewModel
            {
                CustomerRank = customer.CustomerRank,
                CompletedOrders = completedOrders,
                TotalSpent = totalSpent,
                MembershipTiers = membershipTiers
            };

            return View(model);
        }
    }
}
