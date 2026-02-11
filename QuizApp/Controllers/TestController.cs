using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using QuizApp.Services;
using QuizApp.Utils;

namespace QuizApp.Controllers;

[Authorize]
public class TestController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ITestFileService _testService;
    private readonly IAuthService _authService;

    public TestController(ApplicationDbContext db, ITestFileService testService, IAuthService authService)
    {
        _db = db;
        _testService = testService;
        _authService = authService;
    }

    public async Task<IActionResult> Index(string? folder = null)
    {
        var vm = await BuildIndexViewModel(folder);
        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Folder(string? folder = null)
    {
        var vm = await BuildIndexViewModel(folder);
        return PartialView("_TestFolderContent", vm);
    }

    private async Task<TestIndexViewModel> BuildIndexViewModel(string? folder)
    {
        // Обновляем список тем из файлов при каждом заходе для подхвата новых/изменённых файлов.
        _testService.SyncTopicsFromFiles();

        var allTopics = await _db.Topics.ToListAsync();
        var enabledTopics = allTopics.Where(t => t.IsEnabled).ToList();
        var tree = TestTreeNode.BuildTree(allTopics);

        var currentFolderPath = string.IsNullOrWhiteSpace(folder) ? string.Empty : PathHelper.Normalize(folder);
        var prefix = currentFolderPath + Path.DirectorySeparatorChar;

        var topicsInFolder = enabledTopics
            .Where(t => string.IsNullOrEmpty(currentFolderPath) ||
                t.FolderPath == currentFolderPath ||
                (!string.IsNullOrEmpty(t.FolderPath) && t.FolderPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(t => t.Title)
            .ToList();

        var totalInFolder = allTopics.Count(t => string.IsNullOrEmpty(currentFolderPath) ||
            t.FolderPath == currentFolderPath ||
            (!string.IsNullOrEmpty(t.FolderPath) && t.FolderPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));

        var display = PathHelper.ToDisplayPath(currentFolderPath, "Все тесты");

        var lastResults = new Dictionary<int, TestLastResult>();
        var userId = _authService.GetCurrentUserId();
        if (userId.HasValue && topicsInFolder.Count > 0)
        {
            var topicIds = topicsInFolder.Select(t => t.Id).ToList();
            var lastAttempts = await _db.Attempts
                .Where(a => a.UserId == userId.Value && topicIds.Contains(a.TopicId))
                .GroupBy(a => a.TopicId)
                .Select(g => g.OrderByDescending(a => a.CompletedAt ?? a.StartedAt).First())
                .ToListAsync();

            foreach (var attempt in lastAttempts)
                lastResults[attempt.TopicId] = new TestLastResult
                {
                    LastCompletedAt = attempt.CompletedAt ?? attempt.StartedAt,
                    LastScorePercent = attempt.ScorePercent
                };
        }

        return new TestIndexViewModel
        {
            TreeRoot = tree,
            TopicsInFolder = topicsInFolder,
            CurrentFolderPath = currentFolderPath,
            CurrentFolderDisplay = display,
            LastResultsByTopicId = lastResults,
            TotalTopicsInFolder = totalInFolder
        };
    }

    public async Task<IActionResult> Take(int id)
    {
        var (error, topic, questions) = await TryLoadTopicForTake(id);
        if (error != null)
            return error;

        ViewBag.Topic = topic!;
        return View((topic!, questions!));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Take(int id, string? dummy = null)
    {
        var (error, topic, questions) = await TryLoadTopicForTake(id);
        if (error != null)
            return error;

        var userId = _authService.GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login", "Account");

        var attempt = new TestAttempt
        {
            UserId = userId.Value,
            TopicId = topic!.Id,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            TotalQuestions = questions!.Count
        };

        var resultDetails = new List<object>();

        if (topic!.Type == TopicType.Test)
        {
            int correct = 0;
            for (int i = 0; i < questions!.Count; i++)
            {
                var q = questions[i];
                var value = Request.Form[$"q{i}"];
                int selectedIndex = int.TryParse(value, out var idx) ? idx : -1;

                var selectedOption = selectedIndex >= 0 && selectedIndex < q.Options.Count
                    ? q.Options[selectedIndex]
                    : null;
                var correctOption = q.Options.FirstOrDefault(o => o.IsCorrect);

                bool isCorrect = selectedOption != null && correctOption != null &&
                                 ReferenceEquals(selectedOption, correctOption);

                if (isCorrect)
                    correct++;

                resultDetails.Add(new
                {
                    Question = q.Text,
                    Selected = selectedOption?.Text,
                    Correct = correctOption?.Text,
                    IsCorrect = isCorrect
                });
            }

            attempt.CorrectAnswers = correct;
            attempt.ScorePercent = questions!.Count > 0
                ? Math.Round(correct * 100.0 / questions!.Count, 2)
                : 0;
        }
        else if (topic!.Type == TopicType.Open)
        {
            for (int i = 0; i < questions!.Count; i++)
            {
                var q = questions[i];
                var value = Request.Form[$"q{i}"].ToString();
                resultDetails.Add(new
                {
                    Question = q.Text,
                    Answer = value,
                    Correct = q.CorrectAnswer
                });
            }

            attempt.CorrectAnswers = null;
            attempt.ScorePercent = null;
        }
        else // SelfStudy
        {
            attempt.CorrectAnswers = null;
            attempt.ScorePercent = null;
        }

        attempt.ResultJson = JsonSerializer.Serialize(resultDetails, new JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        _db.Attempts.Add(attempt);
        await _db.SaveChangesAsync();

        return RedirectToAction("Result", new { id = attempt.Id });
    }

    public async Task<IActionResult> Result(int id)
    {
        var attempt = await _db.Attempts
            .Include(a => a.Topic)
            .Include(a => a.User)
            .SingleOrDefaultAsync(a => a.Id == id);

        if (attempt == null)
            return NotFound();

        if (attempt.Topic == null || attempt.User == null)
            return NotFound();

        var details = new List<object>();
        if (!string.IsNullOrEmpty(attempt.ResultJson))
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(attempt.ResultJson);
                if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    details = jsonDoc.RootElement.EnumerateArray()
                        .Select(e => (object)e.Clone())
                        .ToList();
                }
            }
            catch
            {
                // игнорируем ошибки парсинга JSON
            }
        }

        ViewBag.Details = details;
        return View(attempt);
    }

    private IActionResult NotAvailableView(int topicId, string title, string message)
    {
        Response.StatusCode = 404;
        return View("NotAvailable", new TestNotAvailableViewModel { TopicId = topicId, Title = title, Message = message });
    }

    private async Task<(IActionResult? Error, Topic? Topic, List<QuestionModel>? Questions)> TryLoadTopicForTake(int id)
    {
        var topic = await _db.Topics.FindAsync(id);
        if (topic == null)
            return (NotAvailableView(id, "Тест не найден", "Такого теста нет (возможно, он был удалён или ещё не создан)."), null, null);

        if (!topic.IsEnabled)
            return (NotAvailableView(id, "Тест отключён", "Этот тест сейчас выключен администратором и недоступен для прохождения."), null, null);

        List<QuestionModel> questions;
        try
        {
            questions = _testService.LoadQuestionsForTopic(topic).ToList();
        }
        catch
        {
            return (NotAvailableView(id, "Не удалось загрузить тест",
                "Файл теста не найден или повреждён. Проверьте, что исходный .txt файл существует в папке tests."), null, null);
        }

        return (null, topic, questions);
    }
}

