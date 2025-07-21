using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Areas.ShopSouvenir.Models;
using Netflixx.Models.Souvenir;
using Netflixx.Repositories;
using Newtonsoft.Json;
using System.Text.Json;

namespace Netflixx.Areas.ShopSouvenir.Controllers
{
    // Areas/ShopSouvenir/Controllers/CartController.cs
    [Area("ShopSouvenir")]
    public class CartController : Controller
    {
        private const string CartSessionKey = "CartSession";
        private readonly DBContext _context;

        public CartController(DBContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int id, int quantity)
        {
            var product = await _context.ProductSous
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            var cart = GetCart();
            var existingItem = cart.CartItems.FirstOrDefault(x => x.ProductId == id);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.CartItems.Add(new CartModel(product) { Quantity = quantity });
            }

            SaveCart(cart);

            return RedirectToAction("Index", "Home", new { area = "ShopSouvenir" });
        }

        [HttpPost]
        public IActionResult UpdateCart(int id, int quantity)
        {
            var cart = GetCart();
            var item = cart.CartItems.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                if (quantity > 0)
                {
                    item.Quantity = quantity;
                }
                else
                {
                    cart.CartItems.Remove(item);
                }
                SaveCart(cart);
            }

            return Json(new
            {
                success = true,
                cartCount = cart.CartItems.Sum(x => x.Quantity),
                totalPrice = cart.TotalPrice.ToString("N0")
            });
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int id)
        {
            var cart = GetCart();
            var item = cart.CartItems.FirstOrDefault(x => x.ProductId == id);

            if (item != null)
            {
                cart.CartItems.Remove(item);
                SaveCart(cart);
            }

            return Json(new
            {
                success = true,
                cartCount = cart.CartItems.Sum(x => x.Quantity),
                totalPrice = cart.TotalPrice.ToString("N0")
            });
        }

        private CartViewModel GetCart()
        {
            var session = HttpContext.Session;
            var cartJson = session.GetString(CartSessionKey);

            if (string.IsNullOrEmpty(cartJson))
            {
                return new CartViewModel();
            }

            return JsonConvert.DeserializeObject<CartViewModel>(cartJson) ?? new CartViewModel();
        }

        private void SaveCart(CartViewModel cart)
        {
            var session = HttpContext.Session;
            var cartJson = JsonConvert.SerializeObject(cart);
            session.SetString(CartSessionKey, cartJson);
        }

        [HttpGet]
        public IActionResult GetCartCount()
        {
            var cart = GetCart();
            return Json(cart.CartItems.Sum(x => x.Quantity));
        }
    }

}
