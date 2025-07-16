using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Netflixx.Models.Souvenir;
using Netflixx.Repositories;

[Area("SouvenirShop")]
public class SouvenirController : Controller
{
    private readonly DBContext _context;
    public SouvenirController(DBContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        List<ProductSouModel> souvenirItems  = _context.ProductSous.ToList();
        return View(souvenirItems);
    }

    public IActionResult Detail(int id)
    {
        ProductSouModel souvenir = _context.ProductSous.FirstOrDefault(i => i.Id == id);
        if (souvenir == null) return RedirectToAction("Index");
        return View(souvenir);
    }

    [HttpPost]
    public IActionResult Buy(int id)
    {
        // Implement your purchase logic here
        // For now, we'll just redirect to detail
        return RedirectToAction("Detail", new { id });
    }
}

