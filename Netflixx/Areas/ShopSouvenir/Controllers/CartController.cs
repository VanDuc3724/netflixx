using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Netflixx.Areas.ShopSouvenir.Models;
using Netflixx.Models;
using Netflixx.Models.Souvenir;
using Netflixx.Repositories;
using System.Text.Json;

namespace Netflixx.Areas.ShopSouvenir.Controllers
{
    [Area("ShopSouvenir")]
    [Route("[area]/Cart/[action]")]
    public class CartController : Controller
    {
        private readonly DBContext _context;
        private readonly UserManager<AppUserModel> _userManager;

        public CartController(DBContext context, UserManager<AppUserModel> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var cartJson = HttpContext.Session.GetString("cart");
            var cartItems = string.IsNullOrEmpty(cartJson)
                ? new List<CartModel>()
                : JsonSerializer.Deserialize<List<CartModel>>(cartJson);

            var coupon = HttpContext.Session.GetString("coupon");
            var discountStr = HttpContext.Session.GetString("discount");
            decimal.TryParse(discountStr, out var discount);

            var model = new CartViewModel
            {
                CartItems = cartItems ?? new List<CartModel>(),
                TotalPrice = cartItems?.Sum(x => x.Quantity * x.Price) ?? 0,
                CouponCode = coupon,
                Discount = discount
            };
            return View(model);
        }

        [HttpPost("AddToCart")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                var product = await _context.ProductSous.FindAsync(request.Id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                var cart = HttpContext.Session.GetJson<List<CartModel>>("cart") ?? new List<CartModel>();
                var item = cart.FirstOrDefault(x => x.ProductId == request.Id);

                if (item == null)
                {
                    cart.Add(new CartModel(product) { Quantity = request.Quantity });
                }
                else
                {
                    item.Quantity += request.Quantity;
                }

                HttpContext.Session.SetJson("cart", cart);
                await HttpContext.Session.CommitAsync();

                return Json(new
                {
                    success = true,
                    message = "Added to cart successfully",
                    cartCount = cart.Sum(x => x.Quantity),
                    cartTotal = cart.Sum(x => x.Quantity * x.Price)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetCartItems()
        {
            var cartItems = HttpContext.Session.GetJson<List<CartModel>>("cart") ?? new List<CartModel>();

            // Tính tổng số lượng sản phẩm
            var totalQuantity = cartItems.Sum(item => item.Quantity);

            // Tính tổng giá trị
            var totalPrice = cartItems.Sum(item => item.Quantity * item.Price);

            return Json(new
            {
                items = cartItems,
                totalQuantity,
                totalPrice
            });
        }

        [HttpGet]
        public IActionResult Increase(int productId)
        {
            var cart = HttpContext.Session.GetJson<List<CartModel>>("cart") ?? new List<CartModel>();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                item.Quantity += 1;
                HttpContext.Session.SetJson("cart", cart);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Decrease(int productId)
        {
            var cart = HttpContext.Session.GetJson<List<CartModel>>("cart") ?? new List<CartModel>();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity -= 1;
                }
                else
                {
                    cart.Remove(item);
                }
                HttpContext.Session.SetJson("cart", cart);
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Remove(int productId)
        {
            var cart = HttpContext.Session.GetJson<List<CartModel>>("cart") ?? new List<CartModel>();
            var item = cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.SetJson("cart", cart);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult GetCoupon(string coupon_value)
        {
            var cart = HttpContext.Session.GetJson<List<CartModel>>("cart") ?? new List<CartModel>();
            var total = cart.Sum(x => x.Quantity * x.Price);
            decimal discount = 0;

            switch (coupon_value?.ToUpperInvariant())
            {
                case "SAVE10":
                    discount = total * 0.10m;
                    break;
                case "SAVE20":
                    discount = total * 0.20m;
                    break;
            }

            if (discount > 0)
            {
                HttpContext.Session.SetString("coupon", coupon_value);
                HttpContext.Session.SetString("discount", discount.ToString());
                return Json(new { success = true, message = "Áp dụng mã giảm giá thành công" });
            }

            return Json(new { success = false, message = "Mã giảm giá không hợp lệ" });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PayWithCoins()
        {
            var cart = HttpContext.Session.GetJson<List<CartModel>>("cart") ?? new List<CartModel>();
            if (!cart.Any())
            {
                TempData["error"] = "Giỏ hàng trống.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var account = await _context.UserAccounts.FirstOrDefaultAsync(a => a.UserID == user.Id);
            if (account == null)
            {
                TempData["error"] = "Không tìm thấy tài khoản.";
                return RedirectToAction(nameof(Index));
            }

            var total = cart.Sum(x => x.Quantity * x.Price);
            int totalCoins = (int)Math.Ceiling(total);

            if (account.PointsBalance < totalCoins)
            {
                TempData["error"] = "Không đủ coin để thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            var order = new OrderSouModel
            {
                OrderCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(),
                CustomerId = user.Id,
                CouponCode = HttpContext.Session.GetString("coupon"),
                OrderDate = DateTime.UtcNow,
                TotalAmount = total,
                Status = "Paid"
            };

            _context.OrderSous.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cart)
            {
                _context.OrderDetailSous.Add(new OrderDetailSouModel
                {
                    OrderId = order.OrderId,
                    OrderCode = order.OrderCode,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price,
                    Subtotal = item.TotalPrice
                });
            }

            account.PointsBalance -= totalCoins;
            _context.PointsTransactions.Add(new PointsTransactionsModel
            {
                UserID = user.Id,
                TransactionDate = DateTime.UtcNow,
                PointsChange = -totalCoins,
                Reason = $"Purchase souvenir order {order.OrderCode}"
            });

            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("cart");
            HttpContext.Session.Remove("coupon");
            HttpContext.Session.Remove("discount");

            TempData["success"] = "Thanh toán thành công bằng coin!";
            return RedirectToAction(nameof(Index));
        }

    }


    public static class SessionExtensions
    {
        public static void SetJson(this ISession session, string key, object value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? GetJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}
