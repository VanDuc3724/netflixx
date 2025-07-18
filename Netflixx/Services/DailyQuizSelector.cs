using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Netflixx.Services
{
    public class DailyQuizSelector : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyQuizSelector> _logger;

        public DailyQuizSelector(IServiceProvider serviceProvider, ILogger<DailyQuizSelector> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<DBContext>();
                        var today = DateTime.UtcNow.Date;

                        var existing = await dbContext.DailyQuiz.FirstOrDefaultAsync(d => d.Date == today);
                        if (existing == null)
                        {
                            var recentQuizzes = await dbContext.DailyQuiz
                                .Where(d => d.Date >= today.AddDays(-7))
                                .Select(d => d.QuizId)
                                .ToListAsync();

                            var availableQuizzes = await dbContext.Quizzes
                                .Where(q => !recentQuizzes.Contains(q.Id))
                                .ToListAsync();

                            if (!availableQuizzes.Any())
                            {
                                availableQuizzes = await dbContext.Quizzes.ToListAsync();
                            }

                            if (availableQuizzes.Any())
                            {
                                var random = new Random();
                                var selectedQuiz = availableQuizzes[random.Next(availableQuizzes.Count)];
                                dbContext.DailyQuiz.Add(new DailyQuiz { Date = today, QuizId = selectedQuiz.Id });
                                await dbContext.SaveChangesAsync();
                                _logger.LogInformation("Selected quiz ID: {QuizId} for date: {Date}", selectedQuiz.Id, today.ToString("yyyy-MM-dd"));
                            }
                            else
                            {
                                _logger.LogWarning("No quizzes available to select for date: {Date}", today.ToString("yyyy-MM-dd"));
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while selecting the daily quiz, continuing to next iteration.");
                }

                var now = DateTime.UtcNow;
                var nextRun = now.Date.AddDays(1);
                var delay = nextRun - now;
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}