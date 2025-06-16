using Microsoft.AspNetCore.Mvc;
using Netflixx.Models;

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

    }
}
