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
            if (amount < 20000)
            {
                ModelState.AddModelError(string.Empty, "Số tiền tối thiểu để nạp là 20.000 VND");
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

            var amountStr = Request.Query["vnp_Amount"].FirstOrDefault();
            var user = await _userManager.GetUserAsync(User);

            // Validate PaymentId exists and is not null/empty
            if (string.IsNullOrEmpty(response.PaymentId))
            {
                // Log error or handle case where PaymentId is missing
                return View("RechargeResult", false);
            }

            if (user != null)
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    long.TryParse(amountStr, out var amount);
                    var coins = (int)(amount / 100);

                    // Ensure provider and environment records exist
                    var provider = await _context.PaymentProviders.FirstOrDefaultAsync(p => p.Name == "VNPAY");
                    if (provider == null)
                    {
                        provider = new PaymentProvidersModel { Name = "VNPAY" };
                        _context.PaymentProviders.Add(provider);
                        await _context.SaveChangesAsync();
                    }

                    var environment = await _context.PaymentEnvironments.FirstOrDefaultAsync(e => e.Name == "VNPAY Sandbox");
                    if (environment == null)
                    {
                        environment = new PaymentEnvironmentsModel { Name = "VNPAY Sandbox" };
                        _context.PaymentEnvironments.Add(environment);
                        await _context.SaveChangesAsync();
                    }

                    // Check if this transaction was already processed (prevent duplicate processing)
                    var existingTransaction = await _context.PaymentTransactions
                        .FirstOrDefaultAsync(pt => pt.ExternalTransactionRef == response.PaymentId);

                    if (existingTransaction != null)
                    {
                        await transaction.RollbackAsync();
                        // Return success if transaction was already processed successfully
                        return View("RechargeResult", existingTransaction.Status == "Success");
                    }

                    if (success && amount > 0)
                    {
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
                    }

                    // Create payment transaction record
                    var paymentTransaction = new PaymentTransactionsModel
                    {
                        UserID = user.Id,
                        ProviderID = provider.ProviderID,
                        EnvironmentID = environment.EnvironmentID,
                        TransactionDate = DateTime.UtcNow,
                        Amount = coins,
                        Currency = "VND",
                        Status = success ? "Success" : "Failed",
                        ExternalTransactionRef = response.PaymentId // This is now guaranteed to be not null/empty
                    };
                    _context.PaymentTransactions.Add(paymentTransaction);
                    await _context.SaveChangesAsync();

                    if (success && amount > 0)
                    {
                        _context.PointsTransactions.Add(new PointsTransactionsModel
                        {
                            UserID = user.Id,
                            TransactionDate = DateTime.UtcNow,
                            PointsChange = coins,
                            Reason = "Recharge via VNPAY",
                            RelatedTransactionID = paymentTransaction.TransactionID
                        });

                        await _context.SaveChangesAsync();
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    // Log the exception
                    // You might want to add logging here
                    return View("RechargeResult", false);
                }
            }

            return View("RechargeResult", success);
        }
    }
}
