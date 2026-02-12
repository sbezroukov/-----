using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HomeCenter.Data;
using HomeCenter.Models;
using HomeCenter.Services;

namespace HomeCenter.Controllers;

[Authorize]
public class StatisticsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuthService _authService;

    public StatisticsController(ApplicationDbContext db, IAuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    public async Task<IActionResult> Index(int? userId = null)
    {
        var isAdmin = User.IsInRole("Admin");
        var currentUserId = _authService.GetCurrentUserId();
        if (currentUserId == null)
            return RedirectToAction("Login", "Account");

        // Админ может смотреть другого пользователя (userId), иначе — только себя.
        var targetUserId = isAdmin && userId.HasValue ? userId.Value : currentUserId.Value;

        var vm = new StatisticsViewModel
        {
            IsAdmin = isAdmin,
            SelectedUserId = targetUserId
        };

        // Всего тестов с вариантами (enabled)
        var totalTestsCount = await _db.Topics.CountAsync(t => t.Type == TopicType.Test && t.IsEnabled && !t.IsDeleted);

        // Только тесты с вариантами (MODE: Test, категория "Тестирование")
        var testAttempts = await _db.Attempts
            .Include(a => a.User)
            .Include(a => a.Topic)
            .Where(a => a.Topic.Type == TopicType.Test && a.ScorePercent != null)
            .ToListAsync();

        // Сводка по всем пользователям для админа
        if (isAdmin)
        {
            var usersWithAttempts = testAttempts
                .GroupBy(a => new { a.UserId, a.User!.UserName })
                .ToDictionary(g => g.Key.UserId, g => new { Unique = g.Select(a => a.TopicId).Distinct().Count(), Count = g.Count(), Avg = g.Average(a => a.ScorePercent ?? 0) });

            vm.AllUsersSummary = (await _db.Users.OrderBy(u => u.UserName).ToListAsync())
                .Select(u =>
                {
                    var hasData = usersWithAttempts.TryGetValue(u.Id, out var data);
                    var unique = hasData ? data!.Unique : 0;
                    var notPassed = totalTestsCount - unique;
                    return new UserStatsSummary
                    {
                        UserId = u.Id,
                        UserName = u.UserName ?? "",
                        TotalTestsCount = totalTestsCount,
                        UniqueTestsCount = unique,
                        NotPassedTestsCount = notPassed,
                        NotPassedPercent = totalTestsCount > 0 ? notPassed * 100.0 / totalTestsCount : 0,
                        TotalAttemptsCount = hasData ? data!.Count : 0,
                        AvgScorePercent = hasData ? data!.Avg : 0
                    };
                })
                .OrderByDescending(u => u.TotalAttemptsCount)
                .ToList();
        }

        // Детальная статистика выбранного пользователя
        var userAttempts = testAttempts.Where(a => a.UserId == targetUserId).ToList();
        var user = await _db.Users.FindAsync(targetUserId);
        var uniqueTestsCount = userAttempts.Select(a => a.TopicId).Distinct().Count();
        var notPassedCount = totalTestsCount - uniqueTestsCount;
        var notPassedPercent = totalTestsCount > 0 ? notPassedCount * 100.0 / totalTestsCount : 0;

        vm.CurrentUserStats = new UserStatsViewModel
        {
            UserId = targetUserId,
            UserName = user?.UserName ?? "—",
            TotalTestsCount = totalTestsCount,
            UniqueTestsCount = uniqueTestsCount,
            NotPassedTestsCount = notPassedCount,
            NotPassedPercent = notPassedPercent,
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
