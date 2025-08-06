using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLego.DataSet.GdrService; // Chỉ sử dụng namespace này
using WebLego.Models;
using WebLego.Models.ViewModel;

namespace WebLego.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly DbpouletLgv5Context _context;

    public HomeController(ILogger<HomeController> logger, DbpouletLgv5Context context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.Now;

        var banners = await _context.HomeBanners
            .Where(b => b.IsActive == true)
            .ToListAsync();

        var featuredProducts = await _context.Products
            .Include(p => p.ProductImages)
            .Where(p => p.Rating >= 4)
            .OrderByDescending(p => p.Rating)
            .Take(10)
            .ToListAsync();

        var discountedProducts = await _context.Products
            .Include(p => p.ProductImages)
            .Include(p => p.Promotion)
            .Where(p => p.Promotion != null
                && p.Promotion.StartDate <= now
                && p.Promotion.EndDate >= now
                && p.DiscountPrice != null)
            .OrderByDescending(p => p.Promotion.EndDate)
            .ToListAsync();

        List<int> favoriteProductIds = new List<int>();
        if (User.Identity.IsAuthenticated)
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            favoriteProductIds = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Select(f => f.ProductId)
                .ToListAsync();
        }

        // Lấy danh sách Categories và thêm vào ViewModel
        var categories = await _context.Categories
            .OrderBy(c => c.CategoryId)
            .ToListAsync();

        var vm = new HomeViewModel
        {
            Banners = banners,
            FeaturedProducts = featuredProducts,
            DiscountedProducts = discountedProducts,
            Categories = categories // thêm danh sách category vào đây
        };

        ViewBag.FavoriteProductIds = favoriteProductIds;

        return View(vm);
    }

    public async Task<IActionResult> Contact()
    {
        var contacts = await _context.ContactInformations
            .Where(c => c.IsActive == true)
            .ToListAsync();
        return View(contacts);
    }

    public async Task<IActionResult> AboutUs()
    {
        var sections = await _context.AboutUsSections
            .Where(s => s.IsActive == true)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();
        return View(sections);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public async Task<IActionResult> ShowCategories()
    {
        var categories = await _context.Categories
            .OrderBy(c => c.CategoryId)
            .ToListAsync();

        return View(categories);
    }
}
