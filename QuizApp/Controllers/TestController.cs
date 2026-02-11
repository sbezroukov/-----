using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.Controllers;

[Authorize]
    public class TestController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ITestFileService _testService;

        public TestController(ApplicationDbContext db, ITestFileService testService)
        {
            _db = db;
            _testService = testService;
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
            // На всякий случай обновляем список тем из файлов при каждом заходе
            // на страницу "Тесты", чтобы новые/измененные файлы сразу подтягивались.
            _testService.SyncTopicsFromFiles();

            // Для дерева слева учитываем все темы (включая отключённые),
            // чтобы структура папок полностью соответствовала файловой.
            var allTopics = await _db.Topics.ToListAsync();
            var enabledTopics = allTopics.Where(t => t.IsEnabled).ToList();

            var tree = TestTreeNode.BuildTree(allTopics);

            // Нормализуем путь папки из query-параметра, чтобы он совпадал с Topic.FolderPath.
            string currentFolderPath;
            if (string.IsNullOrWhiteSpace(folder))
            {
                currentFolderPath = string.Empty;
            }
            else
            {
                var normalized = folder
                    .Replace('\\', System.IO.Path.DirectorySeparatorChar)
                    .Replace('/', System.IO.Path.DirectorySeparatorChar);
                currentFolderPath = normalized;
            }

            // Справа показываем только включённые темы.
            var topicsInFolderQuery = enabledTopics.AsEnumerable();

            if (!string.IsNullOrEmpty(currentFolderPath))
            {
                var prefix = currentFolderPath + System.IO.Path.DirectorySeparatorChar;
                topicsInFolderQuery = topicsInFolderQuery
                    .Where(t => t.FolderPath == currentFolderPath ||
                                (!string.IsNullOrEmpty(t.FolderPath) &&
                                 t.FolderPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
            }

            var topicsInFolder = topicsInFolderQuery
                .OrderBy(t => t.Title)
                .ToList();

            var allTopicsInFolder = allTopics.AsEnumerable();
            if (!string.IsNullOrEmpty(currentFolderPath))
            {
                var prefix = currentFolderPath + System.IO.Path.DirectorySeparatorChar;
                allTopicsInFolder = allTopicsInFolder.Where(t =>
                    t.FolderPath == currentFolderPath ||
                    (!string.IsNullOrEmpty(t.FolderPath) && t.FolderPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
            }
            var totalInFolder = allTopicsInFolder.Count();

            string display;
            if (string.IsNullOrEmpty(currentFolderPath))
            {
                display = "Все тесты";
            }
            else
            {
                var parts = currentFolderPath
                    .Split(System.IO.Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
                display = string.Join(" / ", parts);
            }

            // Последние результаты текущего пользователя по этим тестам
            var lastResults = new Dictionary<int, TestLastResult>();
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                var topicIds = topicsInFolder.Select(t => t.Id).ToList();
                if (topicIds.Count > 0)
                {
                    var lastAttempts = await _db.Attempts
                        .Where(a => a.UserId == userId && topicIds.Contains(a.TopicId))
                        .GroupBy(a => a.TopicId)
                        .Select(g => g
                            .OrderByDescending(a => a.CompletedAt ?? a.StartedAt)
                            .First())
                        .ToListAsync();

                    foreach (var attempt in lastAttempts)
                    {
                        lastResults[attempt.TopicId] = new TestLastResult
                        {
                            LastCompletedAt = attempt.CompletedAt ?? attempt.StartedAt,
                            LastScorePercent = attempt.ScorePercent
                        };
                    }
                }
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
        var topic = await _db.Topics.FindAsync(id);
        if (topic == null)
        {
            Response.StatusCode = 404;
            return View("NotAvailable", new TestNotAvailableViewModel
            {
                TopicId = id,
                Title = "Тест не найден",
                Message = "Такого теста нет (возможно, он был удалён или ещё не создан)."
            });
        }

        if (!topic.IsEnabled)
        {
            Response.StatusCode = 404;
            return View("NotAvailable", new TestNotAvailableViewModel
            {
                TopicId = id,
                Title = "Тест отключён",
                Message = "Этот тест сейчас выключен администратором и недоступен для прохождения."
            });
        }

        try
        {
            var questions = _testService.LoadQuestionsForTopic(topic).ToList();
            ViewBag.Topic = topic;
            return View((topic, questions));
        }
        catch
        {
            Response.StatusCode = 404;
            return View("NotAvailable", new TestNotAvailableViewModel
            {
                TopicId = id,
                Title = "Не удалось загрузить тест",
                Message = "Файл теста не найден или повреждён. Проверьте, что исходный .txt файл существует в папке tests."
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Take(int id, string? dummy = null)
    {
        var topic = await _db.Topics.FindAsync(id);
        if (topic == null)
        {
            Response.StatusCode = 404;
            return View("NotAvailable", new TestNotAvailableViewModel
            {
                TopicId = id,
                Title = "Тест не найден",
                Message = "Такого теста нет (возможно, он был удалён или ещё не создан)."
            });
        }

        if (!topic.IsEnabled)
        {
            Response.StatusCode = 404;
            return View("NotAvailable", new TestNotAvailableViewModel
            {
                TopicId = id,
                Title = "Тест отключён",
                Message = "Этот тест сейчас выключен администратором и недоступен для прохождения."
            });
        }

        List<QuestionModel> questions;
        try
        {
            questions = _testService.LoadQuestionsForTopic(topic).ToList();
        }
        catch
        {
            Response.StatusCode = 404;
            return View("NotAvailable", new TestNotAvailableViewModel
            {
                TopicId = id,
                Title = "Не удалось загрузить тест",
                Message = "Файл теста не найден или повреждён. Проверьте, что исходный .txt файл существует в папке tests."
            });
        }
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return RedirectToAction("Login", "Account");

        var attempt = new TestAttempt
        {
            UserId = userId,
            TopicId = topic.Id,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            TotalQuestions = questions.Count
        };

        var resultDetails = new List<object>();

        if (topic.Type == TopicType.Test)
        {
            int correct = 0;
            for (int i = 0; i < questions.Count; i++)
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
            attempt.ScorePercent = questions.Count > 0
                ? Math.Round(correct * 100.0 / questions.Count, 2)
                : 0;
        }
        else if (topic.Type == TopicType.Open)
        {
            for (int i = 0; i < questions.Count; i++)
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
}

