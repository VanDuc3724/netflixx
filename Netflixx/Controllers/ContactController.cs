using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;
using System.Threading.Tasks;

namespace Netflixx.Controllers
{
    public class ContactController : Controller
    {
        private readonly DBContext _context;

        public ContactController(DBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var contactInfo = await _context.ContactInfos.FirstOrDefaultAsync();
            if (contactInfo == null)
            {
                contactInfo = new ContactInfo();
            }
            return View(contactInfo);
        }

        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Edit()
        {
            var contactInfo = _context.ContactInfos.FirstOrDefault();
            if (contactInfo == null)
            {
                contactInfo = new ContactInfo();
            }
            return View(contactInfo);
        }

        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ContactInfo model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var allContacts = await _context.ContactInfos.ToListAsync();
                    _context.ContactInfos.RemoveRange(allContacts);

                    await _context.ContactInfos.AddAsync(model);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi khi lưu dữ liệu: " + ex.Message);
                }
            }
            return View(model);
        }
    }
}