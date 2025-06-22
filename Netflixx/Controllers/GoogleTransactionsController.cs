using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Netflixx.Models;
using Netflixx.Repositories;
using Netflixx.Services;

namespace Netflixx.Controllers
{
    public class GoogleTransactionsController : Controller
    {
        private readonly DBContext _context;
        private readonly IGoogleTransactionService _service;

        public GoogleTransactionsController(DBContext context, IGoogleTransactionService service)
        {
            _context = context;
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> Import()
        {
            var transactions = await _service.FetchTransactionsAsync();
            _context.GoogleTransactions.AddRange(transactions);
            await _context.SaveChangesAsync();
            return Ok(new { imported = transactions.Count });
        }
    }
}
