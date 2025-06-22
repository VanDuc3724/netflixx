using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Models.Binding;
using Netflixx.Repositories;
using System.Text.Json;

namespace Netflixx.Controllers
{
    public class PaymentTransactionsController : Controller
    {
        private readonly DBContext _db;
        private readonly UserManager<AppUserModel> _userManager;

        public PaymentTransactionsController(DBContext db, UserManager<AppUserModel> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpPost]
        public async Task<IActionResult> ImportFromGoogle()
        {
            var url = "https://script.google.com/macros/s/AKfycbyi5nMWfuHp3BigdOV9h0lq97ci0f1YTWOlULUsRgkW8_AT0e34FVu67ao3l00EXOHx/exec";
            using var client = new HttpClient();
            var json = await client.GetStringAsync(url);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var response = JsonSerializer.Deserialize<ExternalTransactionResponse>(json, options);
            if (response?.Data == null)
            {
                return BadRequest("No data received");
            }

            var provider = await _db.PaymentProviders.FirstOrDefaultAsync();
            if (provider == null)
            {
                provider = new PaymentProvidersModel { Name = "Default Provider" };
                _db.PaymentProviders.Add(provider);
                await _db.SaveChangesAsync();
            }

            var environment = await _db.PaymentEnvironments.FirstOrDefaultAsync();
            if (environment == null)
            {
                environment = new PaymentEnvironmentsModel { Name = "Default Environment" };
                _db.PaymentEnvironments.Add(environment);
                await _db.SaveChangesAsync();
            }

            var user = await _userManager.Users.FirstOrDefaultAsync();
            if (user == null)
            {
                var newUser = new AppUserModel { UserName = "imported", Email = "import@example.com" };
                await _userManager.CreateAsync(newUser, "Password@123");
                user = newUser;
            }

            foreach (var item in response.Data)
            {
                if (!DateTime.TryParse(item.TransactionDate, out var date))
                {
                    continue;
                }

                var transaction = new PaymentTransactionsModel
                {
                    UserID = user.Id,
                    ProviderID = provider.ProviderID,
                    EnvironmentID = environment.EnvironmentID,
                    TransactionDate = date,
                    Amount = item.Amount,
                    Currency = "VND",
                    Status = "Success",
                    ExternalTransactionRef = item.TransactionCode.ToString(),
                    SecretToken = Guid.NewGuid().ToString()
                };

                _db.PaymentTransactions.Add(transaction);
            }

            var inserted = await _db.SaveChangesAsync();
            return Ok(new { inserted });
        }
    }
}
