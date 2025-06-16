using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Models.Souvenir;
using Netflixx.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Netflixx.Areas.Admin.Controllers
{
    [Area("ShopSouvenir")]
    //[Authorize(Roles = "Manager,Admin")]
    public class DashboardController : Controller
    {
        private readonly DBContext _context;
        private readonly UserManager<AppUserModel> _userManager;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(DBContext context, UserManager<AppUserModel> userManager, ILogger<DashboardController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Current period (last 30 days)
            var startDate = DateTime.Now.AddDays(-30);
            var endDate = DateTime.Now;

            // Previous period (30 days before)
            var prevStartDate = DateTime.Now.AddDays(-60);
            var prevEndDate = DateTime.Now.AddDays(-30);

            // Total Revenue
            var totalRevenue = await _context.OrderSous
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status == "Completed")
                .SumAsync(o => o.TotalAmount);

            var prevTotalRevenue = await _context.OrderSous
                .Where(o => o.OrderDate >= prevStartDate && o.OrderDate <= prevEndDate && o.Status == "Completed")
                .SumAsync(o => o.TotalAmount);

            // Number of Orders
            var totalTransactions = await _context.OrderSous
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status == "Completed")
                .CountAsync();

            var prevTotalTransactions = await _context.OrderSous
                .Where(o => o.OrderDate >= prevStartDate && o.OrderDate <= prevEndDate && o.Status == "Completed")
                .CountAsync();

            // Average Order Value
            var avgTransaction = totalTransactions > 0 ? totalRevenue / totalTransactions : 0;
            var prevAvgTransaction = prevTotalTransactions > 0 ? prevTotalRevenue / prevTotalTransactions : 0;

            // New Customers
            var newAccounts = await _context.Users
                .Where(u => u.RegisteredAt >= startDate && u.RegisteredAt <= endDate)
                .CountAsync();

            var prevNewAccounts = await _context.Users
                .Where(u => u.RegisteredAt >= prevStartDate && u.RegisteredAt <= prevEndDate)
                .CountAsync();

            // Calculate percentage changes
            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.RevenueChange = prevTotalRevenue != 0 ? (totalRevenue - prevTotalRevenue) / prevTotalRevenue : 0;
            ViewBag.TotalTransactions = totalTransactions;
            ViewBag.TransactionChange = prevTotalTransactions != 0 ? (double)(totalTransactions - prevTotalTransactions) / prevTotalTransactions : 0;
            ViewBag.AvgTransaction = avgTransaction;
            ViewBag.AvgTransactionChange = prevAvgTransaction != 0 ? (avgTransaction - prevAvgTransaction) / prevAvgTransaction : 0;
            ViewBag.NewAccounts = newAccounts;
            ViewBag.NewAccountsChange = prevNewAccounts != 0 ? (double)(newAccounts - prevNewAccounts) / prevNewAccounts : 0;
            ViewBag.SelectedPeriod = "30days";

            // Recent Orders
            ViewBag.RecentTransactions = await _context.OrderSous
    .Include(o => o.OrderDetails)
        .ThenInclude(od => od.Product)
    .Where(o => o.OrderDate >= startDate)
    .OrderByDescending(o => o.OrderDate)
    .Take(5)
    .ToListAsync();

            // Popular Souvenir Items (based on total quantity sold)
            ViewBag.PopularItems = await _context.OrderDetailSous
                .Include(od => od.Product)
                .Where(od => od.Order.OrderDate >= startDate && od.Order.OrderDate <= endDate)
                .GroupBy(od => od.Product)
                .Select(g => new
                {
                    Product = g.Key,
                    SaleCount = g.Sum(od => od.Quantity)
                })
                .OrderByDescending(x => x.SaleCount)
                .Take(5)
                .ToListAsync();

            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetRevenueData(string period = "30days")
        {
            DateTime startDate;
            DateTime endDate = DateTime.Now;

            switch (period)
            {
                case "thismonth":
                    startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    break;
                case "lastmonth":
                    var lastMonth = DateTime.Now.AddMonths(-1);
                    startDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                    endDate = new DateTime(lastMonth.Year, lastMonth.Month,
                                         DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
                    break;
                default: // "30days"
                    startDate = DateTime.Now.AddDays(-30);
                    break;
            }

            var data = await _context.OrderSous
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status == "Completed")
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    date = g.Key.ToString("yyyy-MM-dd"),
                    value = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.date)
                .ToListAsync();

            return Json(data);
        }


        public async Task<JsonResult> GetMonthlyViews()
        {
            var data = await _context.OrderSous
                .Where(o => o.OrderDate >= DateTime.Now.AddYears(-1) && o.Status == "Completed")
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Views = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            return Json(data);
        }

        [HttpGet]
        public async Task<JsonResult> GetMonthlySales()
        {
            try
            {
                var currentDate = DateTime.Now;
                var startDate = currentDate.AddYears(-1);

                var data = await _context.OrderSous
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= currentDate && o.Status == "Completed")
                    .AsNoTracking()
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Sales = g.Count(),
                        Revenue = g.Sum(o => o.TotalAmount)
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                var result = data.Select(x => new {
                    Label = $"Tháng {x.Month}/{x.Year}",
                    Sales = x.Sales,
                    Revenue = x.Revenue
                });

                return Json(result);
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error in GetMonthlySales");
                return Json(new { error = ex.Message });
            }
        }


        [HttpGet]
        public async Task<JsonResult> GetWeeklyProfit()
        {
            var startDate = DateTime.Now.AddDays(-7);
            var data = await _context.OrderSous
                .Where(o => o.OrderDate >= startDate && o.Status == "Completed")
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Day = g.Key.DayOfWeek.ToString(),
                    Profit = g.Sum(o => o.TotalAmount) * 0.2m // Assuming 20% profit margin
                })
                .ToListAsync();

            // Ensure all days are represented even with zero values
            var allDays = Enum.GetNames(typeof(DayOfWeek));
            var result = allDays.Select(day => new
            {
                Day = day,
                Profit = data.FirstOrDefault(d => d.Day == day)?.Profit ?? 0
            }).OrderBy(x => Array.IndexOf(allDays, x.Day));

            return Json(result);
        }

        [HttpGet]
        public async Task<JsonResult> GetCategoryDistribution()
        {
            var data = await _context.ProductSous
                .Include(p => p.Category)
                .GroupBy(p => p.Category != null ? p.Category.Name : "Khác")
                .Select(g => new
                {
                    Category = g.Key,
                    Count = g.Count(),
                    Percentage = (int)((double)g.Count() / _context.ProductSous.Count() * 100)
                })
                .ToListAsync();

            return Json(data);
        }

        [HttpGet]
        public async Task<JsonResult> GetMonthlyRevenue()
        {
            var data = await _context.OrderSous
                .Where(o => o.OrderDate >= DateTime.Now.AddYears(-1) && o.Status == "Completed")
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new
                {
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Transform to include month/year label
            var result = data.Select(x => new {
                Label = $"Tháng {x.Month}/{x.Year}",
                Value = x.Revenue
            });

            return Json(result);
        }

        [HttpGet]
        public async Task<JsonResult> GetWeeklyRevenue()
        {
            var data = await _context.OrderSous
                .Where(o => o.OrderDate >= DateTime.Now.AddDays(-7) && o.Status == "Completed")
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Day = g.Key.DayOfWeek.ToString(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => (int)Enum.Parse(typeof(DayOfWeek), x.Day))
                .ToListAsync();

            return Json(data);
        }
    }
}