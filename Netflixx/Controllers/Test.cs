using Microsoft.AspNetCore.Mvc;

namespace Netflixx.Controllers
{
    public class Test : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
