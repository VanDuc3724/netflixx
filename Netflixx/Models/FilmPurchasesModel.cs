using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class FilmPurchasesModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PurchaseID { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserID { get; set; } // Kiểu string nếu dùng Identity
        public AppUserModel User { get; set; }

        [Required]
        [ForeignKey("Film")]
        public int FilmID { get; set; }
        public FilmsModel Film { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        [ForeignKey("PaymentTransaction")]
        public int? PaymentTransactionID { get; set; }
        public PaymentTransactionsModel PaymentTransaction { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int PointsUsed { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue)]
        public decimal PricePaid { get; set; }
    }
}