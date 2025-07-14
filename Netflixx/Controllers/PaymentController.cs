using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Models.Vnpay;
using Netflixx.Repositories;
using Netflixx.Services.Vnpay;

namespace Netflixx.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        private readonly IVnPayService _vnPayService;
        private readonly UserManager<AppUserModel> _userManager;
        private readonly DBContext _context;

        public PaymentController(IVnPayService vnPayService,
                                 UserManager<AppUserModel> userManager,
                                 DBContext context)
        {
            _vnPayService = vnPayService;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public IActionResult Recharge()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Recharge(decimal amount)
        {
            if (amount <= 0)
            {
                ModelState.AddModelError(string.Empty, "Amount must be greater than 0");
                return View();
            }

            var paymentModel = new PaymentInformationModel
            {
                Amount = (double)amount,
                OrderDescription = "Recharge coins",
                OrderType = "deposit",
                Name = User.Identity?.Name ?? "User"
            };

            var paymentUrl = _vnPayService.CreatePaymentUrl(paymentModel, HttpContext);
            return Redirect(paymentUrl);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            var success = response.Success && response.VnPayResponseCode == "00";

            if (success)
            {
                var amountStr = Request.Query["vnp_Amount"].FirstOrDefault();
                var user = await _userManager.GetUserAsync(User);
                if (user != null && long.TryParse(amountStr, out var amount))
                {
                    var coins = (int)(amount / 100);
                    var account = await _context.UserAccounts.FirstOrDefaultAsync(a => a.UserID == user.Id);
                    if (account == null)
                    {
                        account = new UserAccountsModel
                        {
                            UserID = user.Id,
                            Balance = 0,
                            PointsBalance = 0
                        };
                        _context.UserAccounts.Add(account);
                    }
                    account.PointsBalance += coins;
                    _context.PointsTransactions.Add(new PointsTransactionsModel
                    {
                        UserID = user.Id,
                        TransactionDate = DateTime.UtcNow,
                        PointsChange = coins,
                        Reason = "Recharge via VNPAY"
                    });
                    await _context.SaveChangesAsync();
                }
            }

            return View("RechargeResult", success);
        }
    }
}
