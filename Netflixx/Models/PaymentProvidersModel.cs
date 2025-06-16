using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace Netflixx.Models
{
    public class PaymentProvidersModel
    {
        [Key]
        public int ProviderID { get; set; }

        [Required(ErrorMessage = "Tên không được để trống.")]
        public string Name { get; set; }

        // Navigation
        [Required]
        public PaymentMethodType MethodType { get; set; }
        public ICollection<PaymentTransactionsModel> PaymentTransactions { get; set; }
    }
}
