using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebLego.Areas.Admin.Models;
using WebLego.DataSet.GdrService;
using WebLego.Services;

namespace WebLego.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("Admin/[controller]/[action]/{id?}")]
    public class ContestController : Controller
    {
        private readonly DbpouletLgv5Context _context;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly EmailService _emailService;

        public ContestController(DbpouletLgv5Context context, IWebHostEnvironment hostingEnvironment, EmailService emailService)
        {
            _context = context;
            _hostingEnvironment = hostingEnvironment;
            _emailService = emailService;
        }

        private async Task<User> GetCurrentUser()
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.FullName == User.Identity.Name);
            Console.WriteLine($"GetCurrentUser: FullName={User.Identity.Name}, UserId={user?.UserId}, RoleId={user?.RoleId}");
            return user;
        }

        private bool IsManager(User user)
        {
            return user?.RoleId == 3;
        }

        private bool IsManagerOrEmployee(User user)
        {
            return user?.RoleId == 3 || user?.RoleId == 2;
        }

        private async Task UpdateContestWinners()
        {
            var currentDate = DateTime.Now;
            var endedContests = await _context.Contests
                .Where(c => c.EndDate < currentDate && c.IsActive && c.ContestStatus != "Đã kết thúc")
                .ToListAsync();

            foreach (var contest in endedContests)
            {
                contest.ContestStatus = "Đã kết thúc";
                await _context.SaveChangesAsync();

                var winnerPost = await _context.CommunityPosts
                    .Include(cp => cp.User)
                    .Include(cp => cp.ContestVotes)
                    .Where(cp => cp.ContestId == contest.ContestId)
                    .OrderByDescending(cp => cp.ContestVotes.Count)
                    .FirstOrDefaultAsync();

                if (winnerPost != null && winnerPost.UserId != 0)
                {
                    var existingWinner = await _context.ContestWinners
                        .AnyAsync(cw => cw.ContestId == contest.ContestId);

                    if (!existingWinner)
                    {
                        var winner = new ContestWinner
                        {
                            ContestId = contest.ContestId,
                            UserId = winnerPost.UserId,
                            RewardProductId = (int)contest.RewardProductId,
                            WonAt = DateTime.Now,
                            Status = "Chưa gửi"
                        };
                        _context.ContestWinners.Add(winner);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }

        private async Task<bool> HasOverlappingContest(DateTime startDate, DateTime endDate, int? excludeContestId = null)
        {
            var currentDate = DateTime.Now;
            var overlappingContests = await _context.Contests
                .Where(c => c.IsActive &&
                            (c.ContestStatus == "Đang diễn ra" || c.ContestStatus == "Sắp diễn ra") &&
                            (excludeContestId == null || c.ContestId != excludeContestId) &&
                            (c.StartDate <= endDate && c.EndDate >= startDate))
                .ToListAsync();

            return overlappingContests.Any();
        }

        private async Task<bool> HasActiveOngoingContest(DateTime startDate, DateTime endDate, int? excludeContestId = null)
        {
            var currentDate = DateTime.Now;
            if (currentDate >= startDate && currentDate <= endDate)
            {
                var ongoingContests = await _context.Contests
                    .Where(c => c.IsActive && c.ContestStatus == "Đang diễn ra" &&
                                (excludeContestId == null || c.ContestId != excludeContestId))
                    .CountAsync();
                return ongoingContests > 0;
            }
            return false;
        }

        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUser();
            if (!IsManagerOrEmployee(user))
                return RedirectToAction("Login", "Auth", new { area = "" });

            await UpdateContestWinners();

            var contests = await _context.Contests
                .Include(c => c.CreatedByNavigation)
                .Include(c => c.CommunityPosts)
                .ToListAsync();

            var currentDate = DateTime.Now;
            var contestViewModels = contests.Select(c =>
            {
                string status = c.ContestStatus ?? (currentDate < c.StartDate ? "Sắp diễn ra" :
                                                  currentDate >= c.StartDate && currentDate <= c.EndDate ? "Đang diễn ra" :
                                                  "Đã kết thúc");

                var winner = _context.ContestWinners
                    .FirstOrDefault(cw => cw.ContestId == c.ContestId);

                string winnerFullName = winner != null && winner.UserId != 0
                    ? _context.Users.Where(u => u.UserId == winner.UserId).Select(u => u.FullName).FirstOrDefault() ?? $"Người dùng #{winner.UserId}"
                    : (c.CommunityPosts.Any() ? "Chưa có bình chọn" : "Chưa có bài đăng");

                return new ContestViewModel
                {
                    ContestId = c.ContestId,
                    UserName = c.CreatedByNavigation?.FullName ?? "Không xác định",
                    Title = c.Title,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    WinnerUserId = winner?.UserId,
                    OrderId = winner?.OrderId,
                    Status = status,
                    Description = c.Description,
                    RewardProductId = c.RewardProductId,
                    ImageUrl = c.ImageUrl,
                    WinnerFullName = winnerFullName,
                    IsManager = IsManager(user) // Thêm để kiểm tra quyền trong view
                };
            }).ToList();

            return View(contestViewModels);
        }

        public async Task<IActionResult> Create()
        {
            var user = await GetCurrentUser();
            if (!IsManager(user))
                return RedirectToAction("Login", "Auth", new { area = "" });

            ViewBag.Products = _context.Products.ToList();
            return View(new ContestViewModel { IsActive = true });
        }

        [HttpPost]
        public async Task<IActionResult> Create(ContestViewModel model)
        {
            var user = await GetCurrentUser();
            if (!IsManager(user))
                return Json(new { success = false, message = "Bạn không có quyền tạo cuộc thi." });

            if (user == null)
                return Json(new { success = false, message = "Không tìm thấy người dùng." });

            // Kiểm tra thời gian trùng lặp
            if (await HasOverlappingContest(model.StartDate, model.EndDate))
                return Json(new { success = false, message = "Thời gian cuộc thi trùng lặp với một cuộc thi đang diễn ra hoặc sắp diễn ra." });

            // Kiểm tra chỉ có một cuộc thi "Đang diễn ra"
            if (await HasActiveOngoingContest(model.StartDate, model.EndDate))
                return Json(new { success = false, message = "Đã có một cuộc thi đang diễn ra. Chỉ được phép có một cuộc thi Đang diễn ra tại một thời điểm." });

            string imageUrl = null;
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "images", "contests");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }
                imageUrl = $"/images/contests/{fileName}";
            }

            var currentDate = DateTime.Now;
            string contestStatus = currentDate < model.StartDate ? "Sắp diễn ra" :
                                  currentDate >= model.StartDate && currentDate <= model.EndDate ? "Đang diễn ra" :
                                  "Đã kết thúc";

            var contest = new Contest
            {
                Title = model.Title,
                Description = model.Description,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                CreatedBy = user.UserId,
                RewardProductId = model.RewardProductId,
                ImageUrl = imageUrl,
                IsActive = model.IsActive,
                CreatedAt = DateTime.Now,
                ContestStatus = contestStatus
            };

            _context.Contests.Add(contest);
            await _context.SaveChangesAsync();

            // Gửi email thông báo cuộc thi mới
            if (contest.IsActive && (contestStatus == "Sắp diễn ra" || contestStatus == "Đang diễn ra"))
            {
                var customers = await _context.Users
                    .Where(u => u.RoleId == 1)
                    .Select(u => u.Email)
                    .ToListAsync();

                var emailBody = $@"
        <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f4f4; padding: 20px; font-family: Arial, sans-serif;'>
          <tr>
            <td align='center'>
              <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 0 10px rgba(0,0,0,0.1);'>
                <tr>
                  <td style='padding-bottom: 10px;'>
                    <h2 style='color:#FECC29;'>Cuộc thi LEGO mới tại PouletLG!</h2>
                  </td>
                </tr>
                <tr>
                  <td style='color:#333333; font-size:16px; line-height:1.6;'>
                    <p>Xin chào,</p>
                    <p>Chúng tôi rất hào hứng thông báo về <strong>{contest.Title}</strong>, một cuộc thi LEGO mới đầy thú vị đang chờ bạn tham gia!</p>
                    <p><strong>Chi tiết cuộc thi:</strong></p>
                    <ul>
                      <li><strong>Tên cuộc thi:</strong> {contest.Title}</li>
                      <li><strong>Mô tả:</strong> {contest.Description}</li>
                      <li><strong>Thời gian bắt đầu:</strong> {contest.StartDate:dd/MM/yyyy HH:mm}</li>
                      <li><strong>Thời gian kết thúc:</strong> {contest.EndDate:dd/MM/yyyy HH:mm}</li>
                      <li><strong>Phần thưởng:</strong> {_context.Products.FirstOrDefault(p => p.ProductId == contest.RewardProductId)?.ProductName ?? "Không xác định"}</li>
                    </ul>
                    <p><strong>Cách tham gia:</strong></p>
                    <p>Để tham gia, bạn chỉ cần có <strong>đơn hàng trong 30 ngày gần nhất</strong> và vào đơn hàng để <strong>chia sẻ cảm xúc</strong> về bất kỳ sản phẩm LEGO nào. Bài đăng của bạn sẽ được đưa vào cuộc thi để nhận lượt bình chọn!</p>
                    <p style='margin-top:30px;'>Thân ái,<br/>Đội ngũ PouletLG</p>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
        </table>";

                foreach (var email in customers)
                {
                    try
                    {
                        await _emailService.SendEmailAsync(email, $"Cuộc thi LEGO mới: {contest.Title}", emailBody);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send contest announcement email to {email}: {ex.Message}");
                    }
                }
            }

            return Json(new { success = true, message = "Tạo cuộc thi thành công." });
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await GetCurrentUser();
            if (!IsManagerOrEmployee(user))
                return RedirectToAction("Login", "Auth", new { area = "" });

            await UpdateContestWinners();

            var contest = await _context.Contests
                .Include(c => c.CreatedByNavigation)
                .FirstOrDefaultAsync(c => c.ContestId == id);
            if (contest == null)
                return NotFound();

            var currentDate = DateTime.Now;
            var contestStatus = currentDate < contest.StartDate ? "Sắp diễn ra" :
                                currentDate >= contest.StartDate && currentDate <= contest.EndDate ? "Đang diễn ra" :
                                "Đã kết thúc";

            var winner = _context.ContestWinners
                .FirstOrDefault(cw => cw.ContestId == contest.ContestId);

            var model = new ContestViewModel
            {
                ContestId = contest.ContestId,
                Title = contest.Title,
                Description = contest.Description,
                StartDate = contest.StartDate,
                EndDate = contest.EndDate,
                RewardProductId = contest.RewardProductId,
                ImageUrl = contest.ImageUrl,
                IsActive = contest.IsActive,
                ContestStatus = contestStatus,
                WinnerUserId = winner?.UserId,
                WinnerFullName = winner != null && winner.UserId != 0
                    ? _context.Users.Where(u => u.UserId == winner.UserId).Select(u => u.FullName).FirstOrDefault() ?? $"Người dùng #{winner.UserId}"
                    : (contest.CommunityPosts.Any() ? "Chưa có bình chọn" : "Chưa có bài đăng"),
                OrderId = winner?.OrderId,
                IsManager = IsManager(user) // Thêm để kiểm tra quyền trong view
            };

            ViewBag.Products = await _context.Products.ToListAsync();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(ContestViewModel model)
        {
            var user = await GetCurrentUser();
            if (!IsManager(user))
                return Json(new { success = false, message = "Bạn không có quyền chỉnh sửa cuộc thi." });

            var contest = await _context.Contests.FindAsync(model.ContestId);
            if (contest == null)
                return Json(new { success = false, message = "Cuộc thi không tồn tại." });

            // Kiểm tra thời gian trùng lặp (loại trừ cuộc thi hiện tại)
            if (await HasOverlappingContest(model.StartDate, model.EndDate, model.ContestId))
                return Json(new { success = false, message = "Thời gian cuộc thi trùng lặp với một cuộc thi đang diễn ra hoặc sắp diễn ra." });

            // Kiểm tra chỉ có một cuộc thi "Đang diễn ra"
            if (await HasActiveOngoingContest(model.StartDate, model.EndDate, model.ContestId))
                return Json(new { success = false, message = "Đã có một cuộc thi đang diễn ra. Chỉ được phép có một cuộc thi Đang diễn ra tại một thời điểm." });

            string imageUrl = contest.ImageUrl;
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_hostingEnvironment.WebRootPath, "images", "contests");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(stream);
                }
                imageUrl = $"/images/contests/{fileName}";
            }

            var currentDate = DateTime.Now;
            string contestStatus = currentDate < model.StartDate ? "Sắp diễn ra" :
                                  currentDate >= model.StartDate && currentDate <= model.EndDate ? "Đang diễn ra" :
                                  "Đã kết thúc";

            contest.Title = model.Title;
            contest.Description = model.Description;
            contest.StartDate = model.StartDate;
            contest.EndDate = model.EndDate;
            contest.RewardProductId = model.RewardProductId;
            contest.ImageUrl = imageUrl;
            contest.IsActive = model.IsActive;
            contest.ContestStatus = contestStatus;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Chỉnh sửa cuộc thi thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> CreateRewardOrder(int contestId, int userId, int rewardProductId, int addressId)
        {
            try
            {
                var user = await GetCurrentUser();
                Console.WriteLine($"CreateRewardOrder: UserId={user?.UserId}, Role={user?.Role?.RoleName}, ContestId={contestId}, UserId={userId}, RewardProductId={rewardProductId}, AddressId={addressId}");

                if (!IsManagerOrEmployee(user))
                {
                    Console.WriteLine("CreateRewardOrder: Unauthorized access");
                    return Json(new { success = false, message = "Bạn không có quyền tạo đơn hàng phần thưởng." });
                }

                var contest = await _context.Contests.FindAsync(contestId);
                if (contest == null || contest.EndDate > DateTime.Now || !contest.IsActive)
                {
                    Console.WriteLine($"CreateRewardOrder: Invalid contest - ContestId={contestId}, EndDate={contest?.EndDate}, IsActive={contest?.IsActive}");
                    return Json(new { success = false, message = "Cuộc thi không hợp lệ hoặc chưa kết thúc." });
                }

                var targetUser = await _context.Users.FindAsync(userId);
                if (targetUser == null)
                {
                    Console.WriteLine($"CreateRewardOrder: User not found - UserId={userId}");
                    return Json(new { success = false, message = "Người dùng không tồn tại." });
                }

                var product = await _context.Products.FindAsync(rewardProductId);
                if (product == null || product.StockQuantity < 1)
                {
                    Console.WriteLine($"CreateRewardOrder: Product invalid - ProductId={rewardProductId}, StockQuantity={product?.StockQuantity}");
                    return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc đã hết hàng." });
                }

                var address = await _context.UserAddresses
                    .FirstOrDefaultAsync(a => a.AddressId == addressId && a.UserId == userId);
                if (address == null)
                {
                    Console.WriteLine($"CreateRewardOrder: Invalid address - AddressId={addressId}, UserId={userId}");
                    return Json(new { success = false, message = "Địa chỉ không hợp lệ." });
                }

                var winner = await _context.ContestWinners
                    .FirstOrDefaultAsync(cw => cw.ContestId == contestId);
                if (winner == null || winner.UserId != userId)
                {
                    Console.WriteLine($"CreateRewardOrder: Invalid winner - ContestId={contestId}, WinnerUserId={winner?.UserId}, ExpectedUserId={userId}");
                    return Json(new { success = false, message = "Chưa xác định người chiến thắng hoặc người dùng không khớp." });
                }

                if (winner.OrderId.HasValue)
                {
                    Console.WriteLine($"CreateRewardOrder: Order already exists - OrderId={winner.OrderId}");
                    return Json(new { success = false, message = "Đơn hàng phần thưởng đã được tạo." });
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Tạo đơn hàng
                    var order = new Order
                    {
                        UserId = userId,
                        AddressId = addressId,
                        OrderDate = DateTime.Now,
                        PaymentMethod = "Reward",
                        ShippingFee = 30000m,
                        TotalAmount = 30000m,
                        OrderStatus = "Đang xử lý",
                        PaymentStatus = "Chưa thanh toán"
                    };
                    Console.WriteLine($"CreateRewardOrder: Creating Order - UserId={order.UserId}, AddressId={order.AddressId}");
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    // Tạo chi tiết đơn hàng
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.OrderId,
                        ProductId = rewardProductId,
                        Quantity = 1,
                        UnitPrice = 0m
                    };
                    Console.WriteLine($"CreateRewardOrder: Creating OrderDetail - OrderId={orderDetail.OrderId}, ProductId={orderDetail.ProductId}");
                    _context.OrderDetails.Add(orderDetail);

                    // Cập nhật số lượng tồn kho
                    product.StockQuantity -= 1;
                    Console.WriteLine($"CreateRewardOrder: Updating Product - ProductId={product.ProductId}, NewStockQuantity={product.StockQuantity}");

                    // Cập nhật người chiến thắng
                    winner.OrderId = order.OrderId;
                    winner.Status = "Đã tạo đơn hàng";
                    winner.WonAt = DateTime.Now;
                    Console.WriteLine($"CreateRewardOrder: Updating ContestWinner - ContestId={winner.ContestId}, OrderId={winner.OrderId}");

                    await _context.SaveChangesAsync();

                    // Gửi email thông báo đơn hàng
                    var winnerPost = await _context.CommunityPosts
                        .Include(cp => cp.ContestVotes)
                        .FirstOrDefaultAsync(cp => cp.ContestId == contestId && cp.UserId == userId);
                    var emailBody = $@"
                    <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f4f4f4; padding: 20px; font-family: Arial, sans-serif;'>
                      <tr>
                        <td align='center'>
                          <table width='600' cellpadding='0' cellspacing='0' style='background-color:#ffffff; padding: 30px; border-radius: 8px; box-shadow: 0 0 10px rgba(0,0,0,0.1);'>
                            <tr>
                              <td style='padding-bottom: 10px;'>
                                <h2 style='color:#FECC29;'>Chúc mừng bạn đã nhận được đơn hàng phần thưởng!</h2>
                              </td>
                            </tr>
                            <tr>
                              <td style='color:#333333; font-size:16px; line-height:1.6;'>
                                <p>Xin chào <strong>{targetUser.FullName}</strong>,</p>
                                <p>Đơn hàng phần thưởng của bạn đã được tạo thành công cho cuộc thi <strong>{contest.Title}</strong>!</p>
                                <p><strong>Chi tiết đơn hàng:</strong></p>
                                <ul>
                                  <li><strong>Sản phẩm:</strong> {product.ProductName}</li>
                                  <li><strong>Phí vận chuyển:</strong> 30,000₫ (Bạn vui lòng thanh toán khi nhận hàng)</li>
                                  <li><strong>Địa chỉ giao hàng:</strong> {address.SpecificAddress}, {address.Ward}, {address.District}, {address.Province}</li>
                                  <li><strong>Mã đơn hàng:</strong> #{order.OrderId}</li>
                                </ul>
                                <p><strong>Thông tin cuộc thi:</strong></p>
                                <ul>
                                  <li><strong>Tên cuộc thi:</strong> {contest.Title}</li>
                                  <li><strong>Mô tả:</strong> {contest.Description}</li>
                                  <li><strong>Bài đăng của bạn:</strong> {(winnerPost != null ? winnerPost.Contest : "Không xác định")}</li>
                                  <li><strong>Số lượt bình chọn:</strong> {(winnerPost != null ? winnerPost.ContestVotes.Count : 0)}</li>
                                </ul>
                                <p>Chúng tôi sẽ sớm giao sản phẩm đến địa chỉ của bạn. Vui lòng chuẩn bị phí vận chuyển khi nhận hàng.</p>
                                <p style='margin-top:30px;'>Thân ái,<br/>Đội ngũ PouletLG</p>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                    </table>";

                    try
                    {
                        await _emailService.SendEmailAsync(targetUser.Email, $"Đơn hàng phần thưởng #{order.OrderId} - PouletLG", emailBody);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send reward order email to {targetUser.Email}: {ex.Message}");
                    }

                    await transaction.CommitAsync();

                    Console.WriteLine($"CreateRewardOrder: Success - OrderId={order.OrderId}");
                    return Json(new
                    {
                        success = true,
                        message = "Tạo đơn hàng phần thưởng thành công.",
                        orderId = order.OrderId
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var errorMessage = ex.Message;
                    var stackTrace = ex.StackTrace;
                    var innerException = ex.InnerException != null ? $"Inner Exception: {ex.InnerException.Message}, Inner StackTrace: {ex.InnerException.StackTrace}" : "No Inner Exception";
                    Console.WriteLine($"CreateRewardOrder: Transaction failed - Error: {errorMessage}, StackTrace: {stackTrace}, {innerException}");
                    return Json(new { success = false, message = $"Lỗi khi tạo đơn hàng: {ex.Message}. Inner Exception: {(ex.InnerException != null ? ex.InnerException.Message : "None")}" });
                }
            }
            catch (Exception ex)
            {
                var errorMessage = ex.Message;
                var stackTrace = ex.StackTrace;
                var innerException = ex.InnerException != null ? $"Inner Exception: {ex.InnerException.Message}, Inner StackTrace: {ex.InnerException.StackTrace}" : "No Inner Exception";
                Console.WriteLine($"CreateRewardOrder: General error - Error: {errorMessage}, StackTrace: {stackTrace}, {innerException}");
                return Json(new { success = false, message = $"Lỗi khi tạo đơn hàng: {ex.Message}. Inner Exception: {(ex.InnerException != null ? ex.InnerException.Message : "None")}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetContestStats(int id)
        {
            var user = await GetCurrentUser();
            if (!IsManagerOrEmployee(user))
                return Json(new { success = false, message = "Không có quyền truy cập." });

            var contest = await _context.Contests
                .Include(c => c.CommunityPosts)
                    .ThenInclude(cp => cp.User)
                .Include(c => c.CommunityPosts)
                    .ThenInclude(cp => cp.ContestVotes)
                .FirstOrDefaultAsync(c => c.ContestId == id);

            if (contest == null)
                return Json(new { success = false, message = "Cuộc thi không tồn tại." });

            var currentDate = DateTime.Now;
            var contestStatus = contest.ContestStatus ?? (currentDate < contest.StartDate ? "Sắp diễn ra" :
                                                        currentDate >= contest.StartDate && currentDate <= contest.EndDate ? "Đang diễn ra" :
                                                        "Đã kết thúc");

            var posts = contest.CommunityPosts.Select(cp => new
            {
                userName = cp.User?.FullName ?? "Không xác định",
                createdAt = cp.CreatedAt,
                voteCount = cp.ContestVotes.Count
            }).ToList();

            var winner = await _context.ContestWinners
                .FirstOrDefaultAsync(cw => cw.ContestId == id);

            var response = new
            {
                success = true,
                participantCount = contest.CommunityPosts.Count,
                posts = posts,
                contestStatus = contestStatus,
                winnerName = winner != null && winner.UserId != 0
                    ? _context.Users.Where(u => u.UserId == winner.UserId).Select(u => u.FullName).FirstOrDefault() ?? $"Người dùng #{winner.UserId}"
                    : (contest.CommunityPosts.Any() ? "Chưa có bình chọn" : "Chưa có bài đăng"),
                winnerUserId = winner?.UserId,
                winnerEmail = winner != null && winner.UserId != 0
                    ? _context.Users.Where(u => u.UserId == winner.UserId).Select(u => u.Email).FirstOrDefault() ?? "Không xác định"
                    : "Không xác định",
                productName = _context.Products
                    .Where(p => p.ProductId == contest.RewardProductId)
                    .Select(p => p.ProductName)
                    .FirstOrDefault() ?? "Không xác định",
                hasWinner = winner != null && winner.UserId != 0,
                orderId = winner?.OrderId
            };

            return Json(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserAddresses(int userId)
        {
            var user = await GetCurrentUser();
            if (!IsManagerOrEmployee(user))
                return Json(new { success = false, message = "Không có quyền truy cập." });

            var addresses = await _context.UserAddresses
                .Where(a => a.UserId == userId)
                .Select(a => new
                {
                    addressId = a.AddressId,
                    specificAddress = a.SpecificAddress,
                    ward = a.Ward,
                    district = a.District,
                    province = a.Province,
                    isDefault = a.IsDefault,
                    fullName = a.FullName,
                    phone = a.Phone
                })
                .ToListAsync();

            return Json(addresses);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await GetCurrentUser();
            if (!IsManager(user))
                return Json(new { success = false, message = "Bạn không có quyền xóa cuộc thi." });

            var contest = await _context.Contests.FindAsync(id);
            if (contest == null)
                return Json(new { success = false, message = "Cuộc thi không tồn tại." });

            _context.Contests.Remove(contest);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa cuộc thi thành công." });
        }
    }
}