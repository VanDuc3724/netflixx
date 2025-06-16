using Microsoft.AspNetCore.Mvc;
using Netflixx.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace Netflixx.Controllers
{
    public class FilmpackageController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Billhistory()
        {
            return View();
        }

        public IActionResult Buy(string packageId, string packageName, int packagePrice, int providerId = 1)
        {
            var model = new FilmPackageViewModel
            {
                PackageId = packageId,
                PackageName = packageName,
                PackagePrice = packagePrice,
                ProviderId = providerId
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetPaymentData()
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromSeconds(10);
            try
            {
                var response = await http.GetAsync("https://script.google.com/macros/s/AKfycbyi5nMWfuHp3BigdOV9h0lq97ci0f1YTWOlULUsRgkW8_AT0e34FVu67ao3l00EXOHx/exec");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
            catch
            {
                return StatusCode(500, new { error = true, message = "Failed to fetch payment data" });
            }
        }

    }
}