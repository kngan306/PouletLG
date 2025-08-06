using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLego.DataSet.GdrService;
using WebLego.Models.ViewModel;

public class CategoryController : Controller
{
    private readonly DbpouletLgv5Context _context;

    public CategoryController(DbpouletLgv5Context context)
    {
        _context = context;
    }

    public IActionResult Index(int id)
    {
        var category = _context.Categories.FirstOrDefault(c => c.CategoryId == id);
        if (category == null)
        {
            return NotFound();
        }

        var products = _context.Products
            .Where(p => p.CategoryId == id && p.ProductStatus == "Active")
            .Include(p => p.ProductImages)
            .ToList();

        var viewModel = new ProductListViewModel
        {
            CategoryName = category.CategoryName,
            Products = products
        };

        return View(viewModel); // --> View này nên là Views/Category/Index.cshtml
    }
}
