using Microsoft.EntityFrameworkCore;
using Netflixx.Models.Souvenir;
using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models
{
    public class OrderSouModel
    {
        [Key]
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = null!;

        public string CustomerId { get; set; }
        public string? CouponCode { get; set; }

        public DateTime OrderDate { get; set; }
        [Precision(18, 2)]
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = null!;


        public virtual ICollection<OrderDetailSouModel> OrderDetails { get; set; } = new List<OrderDetailSouModel>();
    }
}
