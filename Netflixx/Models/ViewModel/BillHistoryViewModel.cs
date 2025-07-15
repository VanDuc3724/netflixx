using System.Collections.Generic;
using Netflixx.Models;

namespace Netflixx.Models.ViewModel
{
    public class BillHistoryViewModel
    {
        public PackageSubscriptionsModel? CurrentPackage { get; set; }
        public List<PaymentTransactionsModel> Transactions { get; set; } = new();
    }
}
