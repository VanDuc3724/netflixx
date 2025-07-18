using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Netflixx.Models;
using Netflixx.Models.ViewModel;
using Netflixx.Repositories;
using System.Linq;
using System.Threading.Tasks;

namespace Netflixx.Areas.ShopSouvenir.Controllers
{
    [Area("ShopSouvenir")]
    [Authorize]
    public class AddressController : Controller
    {
        private readonly DBContext _context;
        private readonly IConfiguration _configuration;
        private readonly UserManager<AppUserModel> _userManager;
        private readonly ILogger<AddressController> _logger;

        public AddressController(DBContext context, IConfiguration configuration, UserManager<AppUserModel> userManager, ILogger<AddressController> logger)
        {
            _context = context;
            _configuration = configuration;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found in Index action.");
                return Unauthorized();
            }

            _logger.LogInformation("Index action called for user {UserId}", user.Id);

            var addresses = await _context.UserAddresses.Where(a => a.UserId == user.Id).ToListAsync();
            return View(addresses);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateAddressViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateAddressViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value.Errors).Select(x => x.ErrorMessage);
                _logger.LogError("Model state invalid in Create action. Errors: {Errors}", string.Join(", ", errors));
                return View(viewModel);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User not found in Create action.");
                return Unauthorized();
            }

            var address = new UserAddress
            {
                Country = viewModel.Country,
                City = viewModel.City,
                State = viewModel.State,
                AddressLine = viewModel.AddressLine,
                PostalCode = viewModel.PostalCode,
                UserId = user.Id
            };

            try
            {
                _context.UserAddresses.Add(address);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Address created successfully for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving address for user {UserId}", user.Id);
                ModelState.AddModelError("", "An error occurred while saving the address.");
                return View(viewModel);
            }

            _logger.LogInformation("Redirecting to Index after creating address for user {UserId}", user.Id);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var address = await _context.UserAddresses.FindAsync(id);
            if (address == null || address.UserId != user.Id) return NotFound();

            var viewModel = new CreateAddressViewModel
            {
                Id = address.Id,
                Country = address.Country,
                City = address.City,
                State = address.State,
                AddressLine = address.AddressLine,
                PostalCode = address.PostalCode
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateAddressViewModel viewModel)
        {
            if (id != viewModel.Id) return BadRequest();

            if (!ModelState.IsValid)
            {
                var errors = ModelState.SelectMany(x => x.Value.Errors).Select(x => x.ErrorMessage);
                _logger.LogError("Model state invalid in Edit action. Errors: {Errors}", string.Join(", ", errors));
                return View(viewModel);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var address = await _context.UserAddresses.FindAsync(id);
            if (address == null || address.UserId != user.Id) return NotFound();

            // Update the address with data from the ViewModel
            address.Country = viewModel.Country;
            address.City = viewModel.City;
            address.State = viewModel.State;
            address.AddressLine = viewModel.AddressLine;
            address.PostalCode = viewModel.PostalCode;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Address updated successfully for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating address for user {UserId}", user.Id);
                ModelState.AddModelError("", "An error occurred while updating the address.");
                return View(viewModel);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetActive(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var addresses = await _context.UserAddresses.Where(a => a.UserId == user.Id).ToListAsync();
            var targetAddress = addresses.FirstOrDefault(a => a.Id == id);
            if (targetAddress == null) return NotFound();

            foreach (var addr in addresses)
            {
                addr.IsActive = (addr.Id == id);
            }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var address = await _context.UserAddresses.FindAsync(id);
            if (address == null || address.UserId != user.Id || address.IsActive) return NotFound();

            _context.UserAddresses.Remove(address);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}