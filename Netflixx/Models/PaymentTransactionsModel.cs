using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Netflixx.Models
{
    public class PaymentTransactionsModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int TransactionID { get; set; }

        [ForeignKey("User")] // Khóa ngoại đến UserModel
        public string UserID { get; set; }
        public AppUserModel User { get; set; }

        [ForeignKey("Provider")] // Khóa ngoại đến PaymentProvidersModel
        public int ProviderID { get; set; }
        public PaymentProvidersModel Provider { get; set; }

        [ForeignKey("Environment")] // Khóa ngoại đến PaymentEnvironmentsModel
        public int EnvironmentID { get; set; }
        public PaymentEnvironmentsModel Environment { get; set; }

        public DateTime TransactionDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public string ExternalTransactionRef { get; set; }

        public ICollection<FilmPurchasesModel> FilmPurchases { get; set; }

        public ICollection<PointsTransactionsModel> RelatedPointsTransactions { get; set; }
    }
}