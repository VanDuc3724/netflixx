using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models
{
    public class DailyRevenueModel
    {
        [Key]
        [DataType(DataType.Date)]
        public DateTime RevenueDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal TotalRevenue { get; set; } = 0;

        [Required]
        [Range(0, int.MaxValue)]
        public int TransactionCount { get; set; } = 0;
    }
}
