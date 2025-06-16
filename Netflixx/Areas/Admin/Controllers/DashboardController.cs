using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;

[Area("Admin")]
public class DashboardController : Controller
{
    private readonly DBContext _context;

    public DashboardController(DBContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string period = "30days")
    {
        // Set default period if not provided
        period = string.IsNullOrEmpty(period) ? "30days" : period;
        ViewBag.SelectedPeriod = period;

        // Get data for current period
        var (currentStartDate, currentEndDate) = GetDateRange(period);
        var currentData = await GetRevenueDataForPeriod(currentStartDate, currentEndDate);

        // Calculate metrics for current period
        var totalRevenue = currentData.Sum(d => d.TotalRevenue);
        var totalTransactions = currentData.Sum(d => d.TransactionCount);
        var avgTransaction = totalTransactions > 0 ? totalRevenue / totalTransactions : 0;

        // Get data for previous period for comparison
        var (previousStartDate, previousEndDate) = GetPreviousPeriodDateRange(period);
        var previousData = await GetRevenueDataForPeriod(previousStartDate, previousEndDate);

        // Calculate metrics for previous period
        var previousRevenue = previousData.Sum(d => d.TotalRevenue);
        var previousTransactions = previousData.Sum(d => d.TransactionCount);
        var previousAvgTransaction = previousTransactions > 0 ? previousRevenue / previousTransactions : 0;

        // Calculate changes
        var revenueChange = previousRevenue != 0 ? (double)((totalRevenue - previousRevenue) / previousRevenue) : 0;
        var transactionChange = previousTransactions != 0 ? (double)(totalTransactions - previousTransactions) / previousTransactions : 0;
        var avgTransactionChange = previousAvgTransaction != 0 ? (double)((avgTransaction - previousAvgTransaction) / previousAvgTransaction) : 0;

        // Get recent transactions and popular films
        var recentTransactions = await _context.PaymentTransactions
            .Include(t => t.User)
            .OrderByDescending(t => t.TransactionDate)
            .Take(5)
            .ToListAsync();

        var popularFilms = await _context.FilmPurchases
            .Include(p => p.Film)
            .Where(p => p.PurchaseDate >= currentStartDate && p.PurchaseDate <= currentEndDate)
            .GroupBy(p => p.Film)
            .Select(g => new {
                Film = g.Key,
                PurchaseCount = g.Count()
            })
            .OrderByDescending(x => x.PurchaseCount)
            .Take(5)
            .ToListAsync();

        // Calculate new accounts
        var newAccounts = await _context.Users
            .Where(u => u.RegisteredAt >= currentStartDate && u.RegisteredAt <= currentEndDate)
            .CountAsync();

        var previousNewAccounts = await _context.Users
            .Where(u => u.RegisteredAt >= previousStartDate && u.RegisteredAt <= previousEndDate)
            .CountAsync();

        var newAccountsChange = previousNewAccounts != 0 ?
            (double)(newAccounts - previousNewAccounts) / previousNewAccounts : 0;

        // Set ViewBag values
        ViewBag.TotalRevenue = totalRevenue;
        ViewBag.RevenueChange = revenueChange;
        ViewBag.TotalTransactions = totalTransactions;
        ViewBag.TransactionChange = transactionChange;
        ViewBag.AvgTransaction = avgTransaction;
        ViewBag.AvgTransactionChange = avgTransactionChange;
        ViewBag.NewAccounts = newAccounts;
        ViewBag.NewAccountsChange = newAccountsChange;
        ViewBag.RecentTransactions = recentTransactions;
        ViewBag.PopularFilms = popularFilms;

        return View(currentData);
    }

    public async Task<IActionResult> GetRevenueData(string period)
    {
        var (startDate, endDate) = GetDateRange(period);
        var data = await GetRevenueDataForPeriod(startDate, endDate);

        var result = data.Select(d => new
        {
            date = d.RevenueDate.ToString("yyyy-MM-dd"),
            value = d.TotalRevenue,
            transactions = d.TransactionCount
        }).ToList();

        return Json(result);
    }

    #region Helper Methods

    private async Task<List<DailyRevenueModel>> GetRevenueDataForPeriod(DateTime startDate, DateTime endDate)
    {
        var dailyRevenues = await _context.DailyRevenue
            .Where(d => d.RevenueDate >= startDate && d.RevenueDate <= endDate)
            .OrderBy(d => d.RevenueDate)
            .ToListAsync();

        // Fill in missing dates
        var allDates = GetAllDatesInRange(startDate, endDate);

        return allDates.Select(date =>
        {
            var existing = dailyRevenues.FirstOrDefault(d => d.RevenueDate == date);
            return new DailyRevenueModel
            {
                RevenueDate = date,
                TotalRevenue = existing?.TotalRevenue ?? 0,
                TransactionCount = existing?.TransactionCount ?? 0
            };
        }).ToList();
    }

    private (DateTime startDate, DateTime endDate) GetDateRange(string period)
    {
        DateTime startDate, endDate = DateTime.UtcNow.Date;

        switch (period.ToLower())
        {
            case "thismonth":
                startDate = new DateTime(endDate.Year, endDate.Month, 1);
                break;
            case "lastmonth":
                startDate = new DateTime(endDate.Year, endDate.Month, 1).AddMonths(-1);
                endDate = startDate.AddMonths(1).AddDays(-1);
                break;
            default: // "30days"
                startDate = endDate.AddDays(-29);
                break;
        }

        return (startDate, endDate);
    }

    private (DateTime prevStartDate, DateTime prevEndDate) GetPreviousPeriodDateRange(string period)
    {
        DateTime prevStartDate, prevEndDate;

        switch (period.ToLower())
        {
            case "thismonth":
                var thisMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                prevStartDate = thisMonthStart.AddMonths(-1);
                prevEndDate = thisMonthStart.AddDays(-1);
                break;
            case "lastmonth":
                var lastMonthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-1);
                prevStartDate = lastMonthStart.AddMonths(-1);
                prevEndDate = lastMonthStart.AddDays(-1);
                break;
            default: // "30days"
                prevStartDate = DateTime.UtcNow.Date.AddDays(-59);
                prevEndDate = DateTime.UtcNow.Date.AddDays(-30);
                break;
        }

        return (prevStartDate, prevEndDate);
    }

    private List<DateTime> GetAllDatesInRange(DateTime startDate, DateTime endDate)
    {
        return Enumerable.Range(0, (endDate - startDate).Days + 1)
            .Select(offset => startDate.AddDays(offset))
            .ToList();
    }

    #endregion
}