using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;
using Netflixx.Services.Momo;

namespace Netflixx.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IMomoService _momoService;
        private readonly DBContext _context;
        private readonly UserManager<AppUserModel> _userManager;
        private readonly IVnPayService _vnPayService;

        public PaymentController(IMomoService momoService, DBContext context, UserManager<AppUserModel> userManager)
        {
            _momoService = momoService;
            _context = context;
            _userManager = userManager;

        }
        [HttpPost]
        [Authorize]
        [Route("CreatePaymentUrl")]
        public async Task<IActionResult> CreatePaymentUrl(int id)
        {
            var package = await _context.Packages.FindAsync(id);
            if (package == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var order = new OrderInfoModel
            {
                FullName = user?.FullName ?? user?.UserName ?? string.Empty,
                OrderInformation = $"Thanh toan goi {package.Name}",
                Amount = package.Price.ToString("0")
            };

            var response = await _momoService.CreatePaymentMomo(order);
            return Redirect(response.PayUrl);
        }
        [HttpGet]
        public IActionResult PaymentCallBack()
        {
            var response = _momoService.PaymentExecuteAsync(HttpContext.Request.Query);
            return View(response);
        }
    }
}
