using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Netflixx.Models.Vnpay;
using Netflixx.Models;
using Netflixx.Repositories;
using Netflixx.Services.Vnpay;
using System.Text.Json;

namespace Netflixx.Controllers
{
    public class PaymentController : Controller
    {
        public IActionResult Index()
        {
            PaymentResponseModel? result = null;
            if (TempData["PaymentResult"] is string json)
            {
                result = JsonSerializer.Deserialize<PaymentResponseModel>(json);
            }
            return View(result);
        }
        private readonly IVnPayService _vnPayService;
        private readonly DBContext _context;
        private readonly UserManager<AppUserModel> _userManager;
        public PaymentController(IVnPayService vnPayService, DBContext context, UserManager<AppUserModel> userManager)
        {

            _vnPayService = vnPayService;
            _context = context;
            _userManager = userManager;
        }

        public IActionResult CreatePaymentUrlVnpay(PaymentInformationModel model)
        {
            var url = _vnPayService.CreatePaymentUrl(model, HttpContext);

            return Redirect(url);
        }
        [HttpGet]
        public async Task<IActionResult> PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);

            var transaction = new PaymentTransactionsModel
            {
                UserID = _userManager.GetUserId(User),
                ProviderID = 1,
                EnvironmentID = 1,
                TransactionDate = DateTime.UtcNow,
                Amount = decimal.TryParse(Request.Query["vnp_Amount"], out var amt) ? amt / 100 : 0,
                Currency = Request.Query["vnp_CurrCode"],
                Status = response.Success ? "Success" : "Fail",
                ExternalTransactionRef = response.TransactionId
            };

            _context.PaymentTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            TempData["PaymentResult"] = JsonSerializer.Serialize(response);
            if (response.Success)
            {
                TempData["success"] = JsonSerializer.Serialize("Thanh toán thành công");
            }
            else
            {
                TempData["error"] = JsonSerializer.Serialize("Thanh toán thất bại");
            }

            return RedirectToAction("Index");
        }


    }
}
