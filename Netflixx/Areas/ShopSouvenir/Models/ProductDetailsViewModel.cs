using Netflixx.Models;
using Netflixx.Models.Souvenir;

namespace Netflixx.Areas.ShopSouvenir.Models
{
    internal class ProductDetailsViewModel
    {
        public ProductSouModel Product { get; set; }
        public List<ProductImageSouModel> ProductImages { get; set; }
    }
}