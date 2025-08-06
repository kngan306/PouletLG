using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebLego.DataSet.GdrService;
using WebLego.Models.ViewModel;

namespace WebLego.Controllers
{
    public class CartController : Controller
    {
        private readonly DbpouletLgv5Context _context;

        public CartController(DbpouletLgv5Context context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult AddToCart(int productId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var product = _context.Products
                .FirstOrDefault(p => p.ProductId == productId);

            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });
            }

            if (product.StockQuantity == 0)
            {
                return Json(new { success = false, message = "Sản phẩm hiện đã hết hàng." });
            }

            var existingCart = _context.Carts
                .FirstOrDefault(c => c.UserId == userId && c.ProductId == productId);

            if (existingCart != null)
            {
                if (existingCart.Quantity + 1 > product.StockQuantity)
                {
                    return Json(new { success = false, message = $"Số lượng vượt quá tồn kho. Chỉ còn {product.StockQuantity} sản phẩm." });
                }
                existingCart.Quantity += 1;
            }
            else
            {
                var newCartItem = new Cart
                {
                    UserId = userId.Value,
                    ProductId = productId,
                    Quantity = 1,
                    AddedAt = DateTime.Now,
                };
                _context.Carts.Add(newCartItem);
            }

            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult AddToCartDT(int productId, int quantity, string action)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var product = _context.Products.FirstOrDefault(p => p.ProductId == productId);
            if (product == null)
            {
                // Xử lý không tìm thấy sản phẩm
                return NotFound();
            }

            if (quantity > product.StockQuantity)
            {
                // Nếu số lượng vượt quá tồn kho, trả về lại view với thông báo lỗi
                ModelState.AddModelError("", $"Số lượng bạn yêu cầu ({quantity}) hiện không đủ trong kho.");

                var viewModel = new ProductDetailViewModel
                {
                    Product = product,
                    StockQuantity = product.StockQuantity ?? 0,
                    //Reviews = _context.Reviews.Where(r => r.ProductId == productId).ToList(),
                    // Đảm bảo set các thuộc tính khác cần thiết trong ViewModel
                };

                return View("ProductDetail", viewModel);
            }

            var existingItem = _context.Carts.FirstOrDefault(c => c.UserId == userId && c.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
                _context.Carts.Update(existingItem);
            }
            else
            {
                _context.Carts.Add(new Cart
                {
                    UserId = userId.Value,
                    ProductId = productId,
                    Quantity = quantity,
                });
            }

            _context.SaveChanges();

            if (action == "buynow")
            {
                return RedirectToAction("Checkout", "Order", new { selectedProductIds = productId.ToString(), quantity = quantity });
            }


            return RedirectToAction("Index", "Cart");
        }



        [HttpPost]
        public IActionResult IncreaseQuantity(int productId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var cartItem = _context.Carts.FirstOrDefault(c => c.UserId == userId && c.ProductId == productId);
            if (cartItem != null)
            {
                cartItem.Quantity += 1;
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult DecreaseQuantity(int productId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var cartItem = _context.Carts.FirstOrDefault(c => c.UserId == userId && c.ProductId == productId);
            if (cartItem != null)
            {
                cartItem.Quantity -= 1;
                if (cartItem.Quantity <= 0)
                {
                    _context.Carts.Remove(cartItem);
                }
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            var cartItem = _context.Carts.FirstOrDefault(c => c.UserId == userId && c.ProductId == productId);
            if (cartItem != null)
            {
                _context.Carts.Remove(cartItem);
                _context.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult RemoveSelected(int[] selectedProductIds)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction("Login", "Auth");

            foreach (var productId in selectedProductIds)
            {
                var cartItem = _context.Carts.FirstOrDefault(c => c.UserId == userId && c.ProductId == productId);
                if (cartItem != null)
                {
                    _context.Carts.Remove(cartItem);
                }
            }

            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult CartIcon()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            int cartCount = 0;

            if (userId != null)
            {
                cartCount = _context.Carts
                    .Where(c => c.UserId == userId)
                    .Sum(c => c.Quantity);
            }

            ViewBag.CartCount = cartCount;
            return PartialView("_CartIconPartial");
        }

        public IActionResult Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var now = DateTime.Now;
            var cartEntities = _context.Carts
                .Where(c => c.UserId == userId)
                .Include(c => c.Product)
                    .ThenInclude(p => p.ProductImages)
                .ToList();

            var cartItems = cartEntities.Select(c =>
            {
                var effectivePrice = (c.Product.DiscountPrice.HasValue /*&& c.Product.PromotionStartDate <= now && c.Product.PromotionEndDate >= now*/)
                    ? c.Product.DiscountPrice.Value
                    : c.Product.Price;

                var savings = (c.Product.DiscountPrice.HasValue /*&& c.Product.PromotionStartDate <= now && c.Product.PromotionEndDate >= now*/)
                    ? (c.Product.Price - c.Product.DiscountPrice.Value)
                    : 0m;

                return new CartItemViewModel
                {
                    ProductId = c.ProductId,
                    ProductName = c.Product.ProductName,
                    ImageUrl = c.Product.ProductImages
                        .FirstOrDefault(img => img.IsMain == true)?.ImageUrl ?? "~/images/placeholder.png",
                    Price = effectivePrice,
                    Quantity = c.Quantity,
                    Total = c.Quantity * effectivePrice,
                    IsDiscounted = c.Product.DiscountPrice.HasValue /*&& c.Product.PromotionStartDate <= now && c.Product.PromotionEndDate >= now*/,
                    OriginalPrice = c.Product.Price,
                    Savings = savings,
                    StockQuantity = c.Product.StockQuantity ?? 0
                };
            }).ToList();

            var recommendedProducts = _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.ProductImages.Any(i => i.IsMain == true))
                .OrderBy(r => Guid.NewGuid())
                .Take(12)
                .ToList();

            ViewBag.Recommended = recommendedProducts;

            // Thêm danh sách sản phẩm yêu thích
            if (userId.HasValue)
            {
                var favoriteProductIds = _context.Favorites
                    .Where(f => f.UserId == userId.Value)
                    .Select(f => f.ProductId)
                    .ToList();
                ViewBag.FavoriteProductIds = favoriteProductIds;
            }
            else
            {
                ViewBag.FavoriteProductIds = new List<int>();
            }

            ViewBag.TotalAmount = cartItems.Sum(i => i.Total);

            return View(cartItems);
        }
    }
}