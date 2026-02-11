namespace QuizApp.Models;

/// <summary>
/// Статистика по одному тесту для пользователя.
/// </summary>
public class TestStatsRow
{
    public string TopicTitle { get; set; } = string.Empty;
    public string DisplayPath { get; set; } = string.Empty;
    public int AttemptsCount { get; set; }
    public double AvgScorePercent { get; set; }
    public double? BestScorePercent { get; set; }
}

/// <summary>
/// Статистика пользователя по тестам с вариантами ответов.
/// </summary>
public class UserStatsViewModel
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int UniqueTestsCount { get; set; }
    public int TotalAttemptsCount { get; set; }
    public double AvgScorePercent { get; set; }
    public List<TestStatsRow> TestStats { get; set; } = new();
}

/// <summary>
/// Сводка по пользователю для списка админа.
/// </summary>
public class UserStatsSummary
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int UniqueTestsCount { get; set; }
    public int TotalAttemptsCount { get; set; }
    public double AvgScorePercent { get; set; }
}

/// <summary>
/// Страница статистики.
/// </summary>
public class StatisticsViewModel
{
    public UserStatsViewModel? CurrentUserStats { get; set; }
    public List<UserStatsSummary> AllUsersSummary { get; set; } = new();
    public List<ApplicationUser> UserDropdown { get; set; } = new();
    public int? SelectedUserId { get; set; }
    public bool IsAdmin { get; set; }
}
