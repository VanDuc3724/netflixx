using Microsoft.AspNetCore.Mvc;

namespace Netflixx.Controllers
{
    public class BrandManagerController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
