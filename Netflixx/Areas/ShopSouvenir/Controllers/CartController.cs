using Microsoft.AspNetCore.Mvc;
using Netflixx.Areas.ShopSouvenir.Models;
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
        public CartController(DBContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cartJson = HttpContext.Session.GetString("cart");
            var cartItems = string.IsNullOrEmpty(cartJson)
                ? new List<CartModel>()
                : JsonSerializer.Deserialize<List<CartModel>>(cartJson);

            var model = new CartViewModel
            {
                CartItems = cartItems ?? new List<CartModel>(),
                TotalPrice = cartItems?.Sum(x => x.Quantity * x.Price) ?? 0
            };
            return View(model);
        }

        [HttpPost("AddToCart/{id}/{quantity?}")]
        public async Task<IActionResult> AddToCart(int id, int quantity = 1)
        {
            try
            {
                var product = await _context.ProductSous.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                var cart = HttpContext.Session.GetJson<List<CartModel>>("cart") ?? new List<CartModel>();
                var item = cart.FirstOrDefault(x => x.ProductId == id);

                if (item == null)
                {
                    cart.Add(new CartModel(product) { Quantity = quantity });
                }
                else
                {
                    item.Quantity += quantity;
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
