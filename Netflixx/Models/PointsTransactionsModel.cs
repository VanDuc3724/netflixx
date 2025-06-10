using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models
{
    public class PointsTransactionsModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PointsTransactionID { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserID { get; set; }
        public AppUserModel User { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;

        [Required]
        public int PointsChange { get; set; } // Có thể âm hoặc dương

        [StringLength(100)]
        public string Reason { get; set; }

        [ForeignKey("PaymentTransaction")]
        public int? RelatedTransactionID { get; set; }
        public PaymentTransactionsModel PaymentTransaction { get; set; }
    }
}
