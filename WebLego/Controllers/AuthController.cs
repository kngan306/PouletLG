using Microsoft.AspNetCore.Mvc;
using WebLego.DataSet.GdrService;
using WebLego.Models.ViewModel;
using Microsoft.EntityFrameworkCore;
using WebLego.Services;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace WebLego.Controllers
{
    public class AuthController : Controller
    {
        private readonly DbpouletLgv5Context _context;
        private readonly EmailService _emailService;

        public AuthController(DbpouletLgv5Context context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: /Auth/Register
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model, int? NgaySinh, int? ThangSinh, int? NamSinh)
        {
            // ✅ Kiểm tra ngày/tháng/năm sinh đầy đủ
            if (!NgaySinh.HasValue || !ThangSinh.HasValue || !NamSinh.HasValue)
            {
                ModelState.AddModelError("", "Vui lòng chọn đầy đủ ngày, tháng và năm sinh.");
            }
            else
            {
                try
                {
                    model.DateOfBirth = new DateTime(NamSinh.Value, ThangSinh.Value, NgaySinh.Value);
                }
                catch
                {
                    ModelState.AddModelError("", "Ngày sinh không hợp lệ."); // ✅ Gắn lỗi đúng trường
                }
            }

            if (ModelState.IsValid)
            {
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại.");
                    return View(model);
                }

                if (await _context.Users.AnyAsync(u => u.Phone == model.Phone))
                {
                    ModelState.AddModelError("PhoneNumber", "Số điện thoại đã tồn tại.");
                    return View(model);
                }

                if (!IsValidPassword(model.UserPassword))
                {
                    ModelState.AddModelError("UserPassword", "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.");
                    return View(model);
                }

                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Phone = model.Phone,
                    UserPassword = BCrypt.Net.BCrypt.HashPassword(model.UserPassword),
                    RoleId = 1,
                    Gender = model.Gender,
                    DateOfBirth = model.DateOfBirth
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                // ⭐ Nếu là khách hàng (RoleId = 1), thêm vào CustomerProfiles
                if (user.RoleId == 1)
                {
                    var customerProfile = new CustomerProfile
                    {
                        CustomerId = user.UserId
                    };

                    _context.CustomerProfiles.Add(customerProfile);
                    await _context.SaveChangesAsync(); // Lưu hồ sơ khách hàng
                }

                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("UserFullName", user.FullName);

                var welcomeBody = $@"
        <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f4f4; padding: 20px; font-family: Arial, sans-serif;'>
          <tr>
            <td align='center'>
              <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 0 10px rgba(0,0,0,0.1);'>
                <tr>
                  <td style='padding-bottom: 10px;'>
                    <h2 style='color:#FECC29;'>Chào mừng đến với PouletLG!</h2>
                  </td>
                </tr>
                <tr>
                  <td style='color:#333333; font-size:16px; line-height:1.6;'>
                    <p>Xin chào <strong>{user.FullName}</strong>,</p>
                    <p>Cảm ơn bạn đã đăng ký tài khoản tại <strong>PouletLG</strong>. Chúng tôi rất vui được đồng hành cùng bạn trong hành trình xây dựng thế giới LEGO sáng tạo!</p>
                    <p>Hãy khám phá những sản phẩm LEGO mới nhất và ưu đãi dành riêng cho bạn.</p>
                    <p style='margin-top:30px;'>Thân ái,<br/>Đội ngũ PouletLG</p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
        </table>";

                await _emailService.SendEmailAsync(user.Email, "Chào mừng đến với PouletLG", welcomeBody);

                return RedirectToAction("Login", "Auth");
            }

            return View(model);
        }
        [HttpGet]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            var redirectUrl = Url.Action("GoogleResponse", "Auth", new { ReturnUrl = returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // GET: /Auth/GoogleResponse
        [HttpGet]
        public async Task<IActionResult> GoogleResponse(string? returnUrl = null)
        {
            var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            if (!authenticateResult.Succeeded)
                return RedirectToAction("Login");

            var claims = authenticateResult.Principal.Claims.ToList();
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var fullName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    FullName = fullName ?? email,
                    RoleId = 1,
                    UserStatus = "Hoạt động"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                if (user.RoleId == 1)
                {
                    var customerProfile = new CustomerProfile
                    {
                        CustomerId = user.UserId
                    };
                    _context.CustomerProfiles.Add(customerProfile);
                    await _context.SaveChangesAsync();
                }
            }

            if (user.UserStatus == "Tạm khóa")
            {
                TempData["AccountLocked"] = "Tài khoản của bạn đã bị tạm khóa do vi phạm chính sách. Vui lòng liên hệ pouletlg@gmail.com để được hỗ trợ.";
                return RedirectToAction("Login");
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserFullName", user.FullName);

            var claimsIdentity = new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.RoleId.ToString())
            }, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties
            );

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
        //public enum RoleEnum
        //{
        //    Customer = 1,
        //    Staff = 2,
        //    Manager = 3
        //}


        // GET: /Auth/Login
        public IActionResult Login() => View();

        // POST: /Auth/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Email == model.EmailOrPhone || u.Phone == model.EmailOrPhone);

                if (user == null ||
                    (!user.UserPassword.StartsWith("$2a$") && model.UserPassword != user.UserPassword) ||
                    (user.UserPassword.StartsWith("$2a$") && !BCrypt.Net.BCrypt.Verify(model.UserPassword, user.UserPassword)))
                {
                    ModelState.AddModelError("", "Email/Số điện thoại hoặc mật khẩu không đúng.");
                    return View(model);
                }

                if (user.UserStatus == "Tạm khóa")
                {
                    TempData["AccountLocked"] = "Tài khoản của bạn đã bị tạm khóa do vi phạm chính sách. Vui lòng liên hệ pouletlg@gmail.com để được hỗ trợ.";
                    return View(model);
                }

                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("UserFullName", user.FullName);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.RoleId.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

                if (user.RoleId == 1)
                {
                    return RedirectToAction("Index", "Home");
                }
                else if (user.RoleId == 2 || user.RoleId == 3)
                {
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }
                else if (user.RoleId == 4)
                {
                    return RedirectToAction("DeliveryStaff", "Orders", new { area = "Admin" });
                }
            }

            return View(model);
        }



        //// GET: /Auth/Logout
        //public async Task<IActionResult> Logout()
        //{
        //    await HttpContext.SignOutAsync();
        //    HttpContext.Session.Clear();
        //    return RedirectToAction("Index", "Home");
        //}

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            HttpContext.Session.Clear();

            // Điều hướng ra ngoài khu vực Admin, về trang Login ở root
            return RedirectToAction("Login", "Auth", new { area = "" });
        }


        //public IActionResult Logout()
        //{
        //    HttpContext.Session.Clear();
        //    return RedirectToAction("Login", "Auth");
        //}

        // Hàm kiểm tra độ mạnh của mật khẩu
        private bool IsValidPassword(string password)
        {
            return password.Length >= 8 &&
                   Regex.IsMatch(password, @"[A-Z]") &&         // ít nhất 1 chữ hoa
                   Regex.IsMatch(password, @"[a-z]") &&         // ít nhất 1 chữ thường
                   Regex.IsMatch(password, @"\d") &&            // ít nhất 1 số
                   Regex.IsMatch(password, @"[\W_]");           // ít nhất 1 ký tự đặc biệt
        }
        // GET: /Auth/ForgotPassword
        public IActionResult ForgotPassword() => View();

        // POST: /Auth/ForgotPassword
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string emailOrPhone)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == emailOrPhone || u.Phone == emailOrPhone);

            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản.");
                return View();
            }

            // Tạo mã OTP (6 số)
            var otp = new Random().Next(100000, 999999).ToString();

            // Lưu vào session hoặc DB (ở đây dùng session)
            HttpContext.Session.SetString("ResetOTP", otp);
            HttpContext.Session.SetString("ResetUser", user.Email); // hoặc user.Phone

            // Gửi email chứa OTP
            var body = $"Mã xác nhận khôi phục mật khẩu của bạn là: <strong>{otp}</strong>";
            await _emailService.SendEmailAsync(user.Email, "Mã khôi phục mật khẩu", body);

            return RedirectToAction("VerifyOtp");
        }
        // GET: /Auth/VerifyOtp
        public IActionResult VerifyOtp() => View();

        // POST: /Auth/VerifyOtp
        [HttpPost]
        public IActionResult VerifyOtp(string otp)
        {
            var storedOtp = HttpContext.Session.GetString("ResetOTP");

            if (storedOtp != otp)
            {
                ModelState.AddModelError("", "Mã OTP không đúng.");
                return View();
            }

            return RedirectToAction("ResetPassword");
        }
        // GET: /Auth/ResetPassword
        public IActionResult ResetPassword() => View();

        // POST: /Auth/ResetPassword
        [HttpPost]
        public async Task<IActionResult> ResetPassword(string newPassword, string confirmPassword)
        {
            var email = HttpContext.Session.GetString("ResetUser");

            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login");

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp.");
                return View();
            }

            if (!IsValidPassword(newPassword))
            {
                ModelState.AddModelError("", "Mật khẩu yếu. Cần ít nhất 8 ký tự, chữ hoa, thường, số và ký tự đặc biệt.");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return RedirectToAction("Login");

            user.UserPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("ResetOTP");
            HttpContext.Session.Remove("ResetUser");

            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
    }

}
