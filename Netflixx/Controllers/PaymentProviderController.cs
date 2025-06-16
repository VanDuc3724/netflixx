using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;
using System.Threading.Tasks;

namespace Netflixx.Controllers
{
    public class PaymentProviderController : Controller
    {
        private readonly DBContext _context;

        public PaymentProviderController(DBContext context)
        {
            _context = context;
        }

        // GET: PaymentProvider
        public async Task<IActionResult> Index()
        {
            var providers = await _context.PaymentProviders.ToListAsync();
            return View(providers);
        }

        // GET: PaymentProvider/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: PaymentProvider/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name")] PaymentProvidersModel provider)
        {
            if (ModelState.IsValid)
            {
                _context.PaymentProviders.Add(provider);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(provider);
        }
    }
}