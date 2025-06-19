// Controllers/ContactController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Netflixx.Models;
using Netflixx.Repositories;
using System.Linq;

namespace Netflixx.Controllers
{
    public class ContactController : Controller
    {
        private readonly DBContext _context;

        public ContactController(DBContext context)
        {
            _context = context;
        }

        // Hiển thị thông tin liên hệ - ai cũng xem được
        public IActionResult Index()
        {
            var contactInfo = _context.ContactInfos.FirstOrDefault();

            if (contactInfo == null)
            {
                contactInfo = new ContactInfo
                {
                    Address = "123 Main Street, City",
                    Phone = "+1 234 567 890",
                    Email = "contact@netflixx.com",
                    BusinessHours = "Monday-Friday: 9AM-5PM",
                    MapEmbedUrl = "https://www.google.com/maps/embed?pb=..."
                };
                _context.ContactInfos.Add(contactInfo);
                _context.SaveChanges();
            }

            return View(contactInfo);
        }

        // Trang chỉnh sửa - chỉ Admin/Manager
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

        // Xử lý form chỉnh sửa - chỉ Admin/Manager
        [Authorize(Roles = "Admin,Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(ContactInfo model)
        {
            if (ModelState.IsValid)
            {
                var existingContact = _context.ContactInfos.FirstOrDefault();

                if (existingContact == null)
                {
                    _context.ContactInfos.Add(model);
                }
                else
                {
                    existingContact.Address = model.Address;
                    existingContact.Phone = model.Phone;
                    existingContact.Email = model.Email;
                    existingContact.BusinessHours = model.BusinessHours;
                    existingContact.MapEmbedUrl = model.MapEmbedUrl;
                }

                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(model);
        }
    }
}