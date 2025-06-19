using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Netflixx.Areas.ShopSouvenir.Controllers;

[Area("ShopSouvenir")]
[Authorize]
public class DashboardController : Controller
{
    private readonly DBContext context;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(DBContext context, ILogger<DashboardController> logger)
    {
        this.context = context;
        _logger = logger;
    }

    public IActionResult Index(string period = "30days")
    {
        try
        {
            period = string.IsNullOrEmpty(period) ? "30days" : period;
            ViewBag.SelectedPeriod = period;

            // Set default values
            ViewBag.TotalRevenue = 12500000;
            ViewBag.RevenueChange = 0.15; // 15%
            ViewBag.TotalTransactions = 42;
            ViewBag.TransactionChange = 0.08; // 8%
            ViewBag.AvgTransaction = 297619;
            ViewBag.AvgTransactionChange = 0.06; // 6%
            ViewBag.NewAccounts = 7;
            ViewBag.NewAccountsChange = 0.25; // 25%

            // Recent transactions (hard-coded)
            ViewBag.RecentTransactions = new List<dynamic>
            {
                new {
                    User = new { FullName = "Nguyễn Văn A" },
                    TotalAmount = 450000,
                    TransactionDate = DateTime.Now.AddDays(-1),
                    Status = "Completed"
                },
                new {
                    User = new { FullName = "Trần Thị B" },
                    TotalAmount = 320000,
                    TransactionDate = DateTime.Now.AddDays(-2),
                    Status = "Completed"
                },
                new {
                    User = new { FullName = "Lê Văn C" },
                    TotalAmount = 280000,
                    TransactionDate = DateTime.Now.AddDays(-3),
                    Status = "Pending"
                }
            };

            // Popular items (hard-coded)
            ViewBag.PopularItems = new List<dynamic>
            {
                new {
                    Product = new {
                        Id = 1,
                        Name = "Móc khóa hình thú",
                        Price = 25000,
                        ImageUrl = "/images/keychain.jpg"
                    },
                    SaleCount = 15
                },
                new {
                    Product = new {
                        Id = 2,
                        Name = "Bút bi thiết kế",
                        Price = 35000,
                        ImageUrl = "/images/pen.jpg"
                    },
                    SaleCount = 12
                },
                new {
                    Product = new {
                        Id = 3,
                        Name = "Sổ tay da",
                        Price = 120000,
                        ImageUrl = "/images/notebook.jpg"
                    },
                    SaleCount = 8
                }
            };

            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Index");
            SetDefaultViewBagValues();
            return View();
        }
    }

    [HttpGet]
    public JsonResult GetRevenueData(string period = "30days")
    {
        try
        {
            var random = new Random();
            var days = period == "30days" ? 30 : period == "thismonth" ? DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month) : 30;

            var data = Enumerable.Range(0, days)
                .Select(i => new {
                    date = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd"),
                    value = random.Next(300000, 800000)
                })
                .ToList();

            return Json(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetRevenueData");
            return Json(new { error = "Error loading revenue data" });
        }
    }

    [HttpGet]
    public JsonResult GetMonthlyRevenue()
    {
        try
        {
            var random = new Random();
            var currentDate = DateTime.Now;

            var data = Enumerable.Range(0, 12)
                .Select(i => {
                    var date = currentDate.AddMonths(-i);
                    return new
                    {
                        Label = $"Tháng {date.Month}/{date.Year}",
                        Value = random.Next(15000000, 30000000)
                    };
                })
                .OrderBy(x => x.Label)
                .ToList();

            return Json(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMonthlyRevenue");
            return Json(new { error = "Error loading monthly revenue data" });
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetMonthlySales()
    {
        try
        {
            var random = new Random();
            var currentDate = DateTime.Now;

            var data = Enumerable.Range(0, 12)
                .Select(i => {
                    var date = currentDate.AddMonths(-i);
                    return new
                    {
                        Label = $"Tháng {date.Month}/{date.Year}",
                        Sales = random.Next(50, 150),
                        Revenue = random.Next(15000000, 30000000)
                    };
                })
                .OrderBy(x => x.Label)
                .ToList();

            return Json(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetMonthlySales");
            return Json(new { error = "Error loading monthly sales data" });
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetCategoryDistribution()
    {
        try
        {
            var categories = new List<string>
            {
                "Đồ lưu niệm", "Quà tặng", "Đồ handmade",
                "Văn phòng phẩm", "Trang trí", "Khác"
            };

            var random = new Random();
            var total = 100;
            var remaining = total;
            var data = new List<dynamic>();

            for (int i = 0; i < categories.Count - 1; i++)
            {
                var max = Math.Min(40, remaining - (categories.Count - i - 1));
                var count = random.Next(10, max);
                data.Add(new
                {
                    Category = categories[i],
                    Count = count,
                    Percentage = count
                });
                remaining -= count;
            }

            // Add last category
            data.Add(new
            {
                Category = categories.Last(),
                Count = remaining,
                Percentage = remaining
            });

            return Json(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetCategoryDistribution");
            return Json(new { error = "Error loading category data" });
        }
    }

    [HttpGet]
    public async Task<JsonResult> GetWeeklyProfit()
    {
        try
        {
            var days = new[] { "Chủ Nhật", "Thứ Hai", "Thứ Ba", "Thứ Tư", "Thứ Năm", "Thứ Sáu", "Thứ Bảy" };
            var random = new Random();

            var data = days.Select(day => new {
                Day = day,
                Profit = random.Next(100000, 200000)
            }).ToList();

            return Json(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetWeeklyProfit");
            return Json(new { error = "Error loading weekly profit data" });
        }
    }

    private async Task<(decimal TotalRevenue, int TotalTransactions, decimal AvgTransaction, int NewAccounts)>
        GetPeriodStats(DateTime startDate, DateTime endDate)
    {
        var revenueData = await context.DailyRevenue
            .Where(d => d.RevenueDate >= startDate && d.RevenueDate <= endDate)
            .ToListAsync();

        var totalRevenue = revenueData.Sum(d => d.TotalRevenue);
        var totalTransactions = revenueData.Sum(d => d.TransactionCount);
        var avgTransaction = totalTransactions > 0 ? totalRevenue / totalTransactions : 0;
        var newAccounts = await context.Users
            .Where(u => u.RegisteredAt >= startDate && u.RegisteredAt <= endDate)
            .CountAsync();

        return (totalRevenue, totalTransactions, avgTransaction, newAccounts);
    }

    private List<dynamic> GetDemoPopularItems()
    {
        return new List<dynamic>
        {
            new {
                Product = new {
                    Id = 1,
                    Name = "Móc khóa hình thú",
                    Price = 25000,
                    ImageUrl = "/images/keychain.jpg"
                },
                SaleCount = 15
            },
            new {
                Product = new {
                    Id = 2,
                    Name = "Bút bi thiết kế",
                    Price = 35000,
                    ImageUrl = "/images/pen.jpg"
                },
                SaleCount = 12
            },
            new {
                Product = new {
                    Id = 3,
                    Name = "Sổ tay da",
                    Price = 120000,
                    ImageUrl = "/images/notebook.jpg"
                },
                SaleCount = 8
            },
            new {
                Product = new {
                    Id = 4,
                    Name = "Gấu bông nhỏ",
                    Price = 80000,
                    ImageUrl = "/images/teddy.jpg"
                },
                SaleCount = 6
            },
            new {
                Product = new {
                    Id = 5,
                    Name = "Hộp quà giấy",
                    Price = 45000,
                    ImageUrl = "/images/giftbox.jpg"
                },
                SaleCount = 5
            }
        };
    }

    private (DateTime startDate, DateTime endDate) GetDateRange(string period, bool isPreviousPeriod = false)
    {
        DateTime startDate, endDate = DateTime.Now.Date;

        switch (period.ToLower())
        {
            case "thismonth":
                startDate = new DateTime(endDate.Year, endDate.Month, 1);
                if (isPreviousPeriod)
                {
                    startDate = startDate.AddMonths(-1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                }
                break;
            case "lastmonth":
                startDate = new DateTime(endDate.Year, endDate.Month, 1).AddMonths(-1);
                endDate = startDate.AddMonths(1).AddDays(-1);
                if (isPreviousPeriod)
                {
                    startDate = startDate.AddMonths(-1);
                    endDate = startDate.AddMonths(1).AddDays(-1);
                }
                break;
            default: // "30days"
                startDate = endDate.AddDays(-29);
                if (isPreviousPeriod)
                {
                    startDate = endDate.AddDays(-59);
                    endDate = endDate.AddDays(-30);
                }
                break;
        }

        return (startDate, endDate);
    }

    private double CalculateChange(decimal current, decimal previous)
    {
        return previous != 0 ? (double)((current - previous) / previous) : 0;
    }

    private double CalculateChange(int current, int previous)
    {
        return previous != 0 ? (double)(current - previous) / previous : 0;
    }

    private void SetDefaultViewBagValues()
    {
        ViewBag.TotalRevenue = 0;
        ViewBag.RevenueChange = 0;
        ViewBag.TotalTransactions = 0;
        ViewBag.TransactionChange = 0;
        ViewBag.AvgTransaction = 0;
        ViewBag.AvgTransactionChange = 0;
        ViewBag.NewAccounts = 0;
        ViewBag.NewAccountsChange = 0;
        ViewBag.RecentTransactions = new List<dynamic>();
        ViewBag.PopularItems = new List<dynamic>();
    }
}