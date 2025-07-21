using Netflixx.Models.Souvenir;

namespace Netflixx.Areas.ShopSouvenir.Models
{
    [Serializable]
    public class CartModel
    {
        public CartModel()
        {
        }

        public CartModel(ProductSouModel product)
        {
            ProductId = product.Id;
            ProductName = product.Name;
            Price = product.Price;
            Quantity = 1;
            ImageUrl = product.ImageUrl;
            CategoryName = product.Category?.Name;
            BrandName = product.Brand?.Name;
            SeriesName = product.Series?.Name;
        }

        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        public string? CategoryName { get; set; }
        public string? BrandName { get; set; }
        public string? SeriesName { get; set; }
        public decimal TotalPrice { get { return Price * Quantity; } }
    }
}
