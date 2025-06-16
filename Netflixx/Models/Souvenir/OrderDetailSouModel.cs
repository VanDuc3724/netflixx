using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models.Souvenir
{
    public class OrderDetailSouModel
    {
        [Key]
        public int OrderDetailId { get; set; }

        [Required]
        public string OrderCode { get; set; }

        public int? ProductId { get; set; }
        public ProductSouModel Product { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal Subtotal { get; set; }

        public int? OrderId { get; set; }
        public OrderSouModel Order { get; set; }
    }
}
