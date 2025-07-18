using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Netflixx.Models;
using Netflixx.Models.ViewModel;
using Netflixx.Repositories;

public class QuizController : Controller
{
    private readonly DBContext _db;
    private readonly UserManager<AppUserModel> _um;

    public QuizController(DBContext db, UserManager<AppUserModel> um)
    {
        _db = db;
        _um = um;
    }

    // GET: /Quiz/Daily
    [Authorize]
    public async Task<IActionResult> Daily()
    {
        var today = DateTime.UtcNow.Date;
        var quiz = await _db.Quizzes
                            .Include(q => q.Questions)
                              .ThenInclude(q => q.Options)
                            .FirstOrDefaultAsync(q => q.PublishDate == today && q.IsActive);

        if (quiz == null) return View("NoQuizToday");

        var user = await _um.GetUserAsync(User);
        var attempt = await _db.UserQuizAttempts
                               .Include(a => a.Answers)
                               .FirstOrDefaultAsync(a => a.QuizId == quiz.Id && a.UserId == user.Id);

        // Get leaderboard data (top 5 for sidebar)
        var topUsers = await GetLeaderboardData(5);
        ViewBag.TopUsers = topUsers;

        if (attempt == null || attempt.CompletedAt == null)
        {
            var takeVm = new TakeQuizViewModel { Quiz = quiz, Attempt = attempt };
            return View("Daily", takeVm);
        }
        else
        {
            var resVm = new QuizResultsViewModel { Quiz = quiz, Attempt = attempt };
            return View("Daily", resVm);
        }
    }

    // POST: /Quiz/Submit
    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> Submit(int quizId, QuizResultsViewModel model)
    {
        var user = await _um.GetUserAsync(User);
        var quiz = await _db.Quizzes
                            .Include(q => q.Questions)
                              .ThenInclude(q => q.Options)
                            .FirstOrDefaultAsync(q => q.Id == quizId);
        if (quiz == null) return NotFound();

        // Create or load attempt
        var attempt = await _db.UserQuizAttempts
                               .Include(a => a.Answers)
                               .FirstOrDefaultAsync(a => a.QuizId == quizId && a.UserId == user.Id)
                      ?? new UserQuizAttempt { QuizId = quizId, UserId = user.Id };

        attempt.StartedAt = DateTime.UtcNow;
        attempt.Answers = model.SelectedAnswers
                            .Select(kvp => new UserAnswer
                            {
                                QuestionId = kvp.Key,
                                ChosenOptionId = kvp.Value
                            })
                            .ToList();

        // Score calculation
        var correctOptionIds = quiz.Questions
                                  .SelectMany(q => q.Options)
                                  .Where(o => o.IsCorrect)
                                  .Select(o => o.Id)
                                  .ToHashSet();

        attempt.Score = attempt.Answers.Count(a => correctOptionIds.Contains(a.ChosenOptionId));
        attempt.CompletedAt = DateTime.UtcNow;

        if (attempt.Id == 0)
            _db.UserQuizAttempts.Add(attempt);
        else
            _db.UserQuizAttempts.Update(attempt);

        await _db.SaveChangesAsync();

        return RedirectToAction("Daily");
    }

    // POST: /Quiz/ClaimReward
    [HttpPost, ValidateAntiForgeryToken, Authorize]
    public async Task<IActionResult> ClaimReward(int attemptId)
    {
        var attempt = await _db.UserQuizAttempts
                               .Include(a => a.User)
                               .Include(a => a.Quiz)
                                 .ThenInclude(q => q.Questions)
                               .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt == null || attempt.RewardClaimed)
            return BadRequest();

        var user = await _um.GetUserAsync(User);
        if (attempt.UserId != user.Id)
            return Forbid();

        // Calculate reward points based on score
        var pointsEarned = CalculateRewardPoints(attempt.Score, attempt.Quiz.Questions.Count);

        // Update user total points
        user.TotalQuizPoints += pointsEarned;

        // Mark reward as claimed
        attempt.RewardClaimed = true;

        // Save changes
        await _um.UpdateAsync(user);
        _db.UserQuizAttempts.Update(attempt);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Congratulations! You earned {pointsEarned} points!";
        return RedirectToAction("Daily");
    }

    // GET: /Quiz/Leaderboard (for modal)
    [Authorize]
    public async Task<IActionResult> Leaderboard()
    {
        var topUsers = await GetLeaderboardData(10);
        return PartialView("_LeaderboardModal", topUsers);
    }

    private async Task<List<LeaderboardUser>> GetLeaderboardData(int count)
    {
        return await _db.Users
                        .Where(u => u.TotalQuizPoints > 0)
                        .OrderByDescending(u => u.TotalQuizPoints)
                        .Take(count)
                        .Select(u => new LeaderboardUser
                        {
                            FullName = u.FullName,
                            AvatarUrl = u.AvatarUrl ?? "/images/avatar-04.jpg",
                            TotalPoints = u.TotalQuizPoints ?? 0
                        })
                        .ToListAsync();
    }

    private int CalculateRewardPoints(int score, int totalQuestions)
    {
        // Perfect score: 100 points
        // Good score (80%+): 75 points
        // Average score (60%+): 50 points
        // Below average (40%+): 25 points
        // Below 40%: 10 points (participation)

        var percentage = (double)score / totalQuestions;

        return percentage switch
        {
            >= 1.0 => 100,    // Perfect score
            >= 0.8 => 75,     // 80%+
            >= 0.6 => 50,     // 60%+
            >= 0.4 => 25,     // 40%+
            _ => 10           // Below 40%
        };
    }
}

public class LeaderboardUser
{
    public string FullName { get; set; }
    public string AvatarUrl { get; set; }
    public int TotalPoints { get; set; }
}