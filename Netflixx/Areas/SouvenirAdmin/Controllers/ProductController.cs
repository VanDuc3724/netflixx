using Microsoft.AspNetCore.Mvc;
using Netflixx.Models.Souvenir;
using Netflixx.Repositories;

namespace Netflixx.Areas.SouvenirAdmin.Controllers
{
    [Area("SouvenirAdmin")]
    public class ProductController : Controller
    {
        private readonly DBContext _context;
        private readonly IWebHostEnvironment _webHostEn;

        public ProductController(DBContext context, IWebHostEnvironment webHostEn)
        {
            _context = context;
            _webHostEn = webHostEn;
        }

        public async Task<IActionResult> Index()
        {
            List<ProductSouModel> product = _context.ProductSous.ToList();
            return View(product); // MVC sẽ tự động tìm View trong Areas/SouvenirAdmin/Views/Product/
        }

    }
}
