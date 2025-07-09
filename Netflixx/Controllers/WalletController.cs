using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Netflixx.Models;
using Netflixx.Repositories;
using Netflixx.Services;

namespace Netflixx.Controllers
{
    [Authorize]
    public class WalletController : Controller
    {
        private readonly DBContext _db;
        private readonly UserManager<AppUserModel> _userManager;
        private readonly IVNPayService _vnPayService;

        public WalletController(DBContext db, UserManager<AppUserModel> userManager, IVNPayService vnPayService)
        {
            _db = db;
            _userManager = userManager;
            _vnPayService = vnPayService;
        }

        [HttpGet]
        public IActionResult Deposit()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Deposit(decimal amount)
        {
            if (amount <= 0)
            {
                TempData["error"] = "Invalid amount";
                return View();
            }
            string returnUrl = Url.Action("VNPayReturn", "Wallet", null, Request.Scheme)!;
            string url = _vnPayService.CreatePaymentUrl(amount, "Recharge", returnUrl);
            return Redirect(url);
        }

        [HttpGet]
        public async Task<IActionResult> VNPayReturn()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Login");
            }
            if (!decimal.TryParse(Request.Query["vnp_Amount"], out var vnpAmount))
            {
                TempData["error"] = "Payment error";
                return RedirectToAction("Deposit");
            }
            decimal amount = vnpAmount / 100m;
            var account = _db.UserAccounts.FirstOrDefault(a => a.UserID == user.Id);
            if (account == null)
            {
                account = new UserAccountsModel { UserID = user.Id, Balance = 0, PointsBalance = 0 };
                _db.UserAccounts.Add(account);
            }
            account.Balance += amount;
            _db.PointsTransactions.Add(new PointsTransactionsModel
            {
                UserID = user.Id,
                PointsChange = (int)amount,
                Reason = "VNPay recharge",
                RelatedTransactionID = null,
                TransactionDate = DateTime.UtcNow
            });
            _db.SaveChanges();
            return RedirectToAction("DepositSuccess");
        }

        public IActionResult DepositSuccess()
        {
            return View();
        }
    }
}
