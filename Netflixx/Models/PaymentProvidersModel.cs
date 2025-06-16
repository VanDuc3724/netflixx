using System.ComponentModel.DataAnnotations;

namespace Netflixx.Models
{
    public class PaymentProvidersModel
    {
        [Key]
        public int ProviderID { get; set; }

        [Required(ErrorMessage = "Tên không được để trống.")]
        public string Name { get; set; }

        // Navigation property
        public ICollection<PaymentTransactionsModel> PaymentTransactions { get; set; }
    }
}
