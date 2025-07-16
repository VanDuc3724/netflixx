using Microsoft.AspNetCore.Mvc;

namespace Netflixx.Controllers
{
    public class PackagesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
