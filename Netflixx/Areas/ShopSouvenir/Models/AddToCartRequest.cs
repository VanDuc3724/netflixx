using System.ComponentModel.DataAnnotations;

namespace Netflixx.Areas.ShopSouvenir.Models
{
    public class AddToCartRequest
    {
        [Required]
        public int Id { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;
    }
}
