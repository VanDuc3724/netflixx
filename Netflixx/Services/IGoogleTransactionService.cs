using System.Collections.Generic;
using System.Threading.Tasks;
using Netflixx.Models;

namespace Netflixx.Services
{
    public interface IGoogleTransactionService
    {
        Task<List<GoogleTransactionModel>> FetchTransactionsAsync();
    }
}
