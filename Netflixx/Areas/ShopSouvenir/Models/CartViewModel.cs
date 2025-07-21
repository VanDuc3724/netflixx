namespace Netflixx.Areas.ShopSouvenir.Models
{
    public class CartViewModel
    {
        public List<CartModel> CartItems { get; set; } = new List<CartModel>();
        public decimal TotalPrice => CartItems.Sum(x => x.TotalPrice);
    }
}
