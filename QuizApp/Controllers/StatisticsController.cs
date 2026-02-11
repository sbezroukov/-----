using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;

namespace QuizApp.Controllers;

[Authorize]
public class StatisticsController : Controller
{
    private readonly ApplicationDbContext _db;

    public StatisticsController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(int? userId = null)
    {
        var isAdmin = User.IsInRole("Admin");
        var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int? currentUserId = int.TryParse(currentUserIdClaim, out var uid) ? uid : null;

        if (currentUserId == null)
            return RedirectToAction("Login", "Account");

        // Обычный пользователь видит только свою статистику
        var targetUserId = isAdmin && userId.HasValue ? userId.Value : currentUserId.Value;

        if (isAdmin && userId.HasValue && userId.Value != currentUserId.Value)
        {
            // Админ смотрит другого пользователя
        }
        else if (!isAdmin)
        {
            // Не-админ всегда видит только себя
            targetUserId = currentUserId.Value;
        }

        var vm = new StatisticsViewModel
        {
            IsAdmin = isAdmin,
            SelectedUserId = targetUserId
        };

        // Только тесты с вариантами (MODE: Test, категория "Тестирование")
        var testAttempts = await _db.Attempts
            .Include(a => a.User)
            .Include(a => a.Topic)
            .Where(a => a.Topic.Type == TopicType.Test && a.ScorePercent != null)
            .ToListAsync();

        // Сводка по всем пользователям для админа
        if (isAdmin)
        {
            vm.UserDropdown = await _db.Users.OrderBy(u => u.UserName).ToListAsync();
            vm.AllUsersSummary = testAttempts
                .GroupBy(a => new { a.UserId, a.User!.UserName })
                .Select(g => new UserStatsSummary
                {
                    UserId = g.Key.UserId,
                    UserName = g.Key.UserName ?? "",
                    UniqueTestsCount = g.Select(a => a.TopicId).Distinct().Count(),
                    TotalAttemptsCount = g.Count(),
                    AvgScorePercent = g.Average(a => a.ScorePercent ?? 0)
                })
                .OrderByDescending(u => u.TotalAttemptsCount)
                .ToList();
        }

        // Детальная статистика выбранного пользователя
        var userAttempts = testAttempts.Where(a => a.UserId == targetUserId).ToList();
        var user = await _db.Users.FindAsync(targetUserId);

        vm.CurrentUserStats = new UserStatsViewModel
        {
            UserId = targetUserId,
            UserName = user?.UserName ?? "—",
            UniqueTestsCount = userAttempts.Select(a => a.TopicId).Distinct().Count(),
            TotalAttemptsCount = userAttempts.Count,
            AvgScorePercent = userAttempts.Count > 0
                ? userAttempts.Average(a => a.ScorePercent ?? 0)
                : 0,
            TestStats = userAttempts
                .GroupBy(a => new { a.TopicId, a.Topic!.Title, a.Topic.DisplayPath })
                .Select(g => new TestStatsRow
                {
                    TopicTitle = g.Key.Title,
                    DisplayPath = g.Key.DisplayPath,
                    AttemptsCount = g.Count(),
                    AvgScorePercent = g.Average(a => a.ScorePercent ?? 0),
                    BestScorePercent = g.Max(a => a.ScorePercent)
                })
                .OrderByDescending(t => t.AttemptsCount)
                .ThenBy(t => t.TopicTitle)
                .ToList()
        };

        return View(vm);
    }
}
