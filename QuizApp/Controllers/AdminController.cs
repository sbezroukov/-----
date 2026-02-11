using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using QuizApp.Services;
using QuizApp.Utils;

namespace QuizApp.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ITestFileService _testFileService;
    private readonly ITestImportService _testImportService;
    private readonly IAuthService _authService;
    private readonly IWebHostEnvironment _env;

    public AdminController(
        ApplicationDbContext db,
        IConfiguration configuration,
        ITestFileService testFileService,
        ITestImportService testImportService,
        IAuthService authService,
        IWebHostEnvironment env)
    {
        _db = db;
        _configuration = configuration;
        _testFileService = testFileService;
        _testImportService = testImportService;
        _authService = authService;
        _env = env;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        var adminUser = _configuration["Admin:Username"];
        var adminPassword = _configuration["Admin:Password"];

        if (model.UserName == adminUser && model.Password == adminPassword)
        {
            var admin = await _db.Users.FirstOrDefaultAsync(u => u.UserName == model.UserName);
            if (admin == null)
            {
                admin = new ApplicationUser { UserName = model.UserName, Password = "" };
                _db.Users.Add(admin);
                await _db.SaveChangesAsync();
            }

            await _authService.SignInAsync(admin, "Admin");

            var redirectUrl = model.ReturnUrl ?? returnUrl;
            if (!string.IsNullOrEmpty(redirectUrl) && Url.IsLocalUrl(redirectUrl))
                return Redirect(redirectUrl);

            return RedirectToAction("Index");
        }

        ModelState.AddModelError(string.Empty, "Неверный логин или пароль администратора.");
        return View(model);
    }

    public async Task<IActionResult> Index(string? folder = null, bool? includeSubfolders = null)
    {
        var (topicsInFolder, viewData) = await BuildAdminIndexData(folder, includeSubfolders);
        foreach (var (key, value) in viewData)
            ViewData[key] = value;
        return View(topicsInFolder);
    }

    [HttpGet]
    public async Task<IActionResult> Folder(string? folder = null, bool? includeSubfolders = null)
    {
        var (topicsInFolder, viewData) = await BuildAdminIndexData(folder, includeSubfolders);
        foreach (var (key, value) in viewData)
            ViewData[key] = value;
        return PartialView("_AdminFolderContent", topicsInFolder);
    }

    private async Task<(List<Topic> TopicsInFolder, Dictionary<string, object> ViewData)> BuildAdminIndexData(string? folder, bool? includeSubfolders)
    {
        _testFileService.SyncTopicsFromFiles();

        var allTopics = await _db.Topics.OrderBy(t => t.Title).ToListAsync();
        var tree = TestTreeNode.BuildTree(allTopics);

        string currentFolderPath = string.IsNullOrWhiteSpace(folder) ? string.Empty : PathHelper.Normalize(folder);

        var topicsInFolder = allTopics.AsEnumerable();
        if (!string.IsNullOrEmpty(currentFolderPath))
        {
            var prefix = currentFolderPath + Path.DirectorySeparatorChar;
            var include = includeSubfolders ?? true;
            topicsInFolder = topicsInFolder.Where(t =>
                t.FolderPath == currentFolderPath ||
                (include && !string.IsNullOrEmpty(t.FolderPath) && t.FolderPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));
        }

        var totalUsers = await _db.Users.CountAsync();
        var totalAttempts = await _db.Attempts.CountAsync();

        var viewData = new Dictionary<string, object>
        {
            ["TotalUsers"] = totalUsers,
            ["TotalAttempts"] = totalAttempts,
            ["TreeRoot"] = tree,
            ["CurrentFolderPath"] = currentFolderPath ?? string.Empty,
            ["IncludeSubfolders"] = includeSubfolders ?? true,
            ["FolderDisplay"] = PathHelper.ToDisplayPath(currentFolderPath ?? string.Empty, "Все темы")
        };

        return (topicsInFolder.ToList(), viewData);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleTopic(int id)
    {
        var topic = await _db.Topics.FindAsync(id);
        if (topic == null)
            return NotFound();

        // Переключаем флаг на сервере, не полагаясь на значение из формы.
        topic.IsEnabled = !topic.IsEnabled;
        await _db.SaveChangesAsync();

        // JSON для AJAX — без перезагрузки страницы
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { id = topic.Id, isEnabled = topic.IsEnabled });

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleFolder(string folderPath, bool includeSubfolders, bool enable)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return BadRequest();

        var normalized = PathHelper.Normalize(folderPath);
        var prefix = normalized + Path.DirectorySeparatorChar;

        var topics = await _db.Topics.ToListAsync();
        var toUpdate = topics.Where(t =>
            t.FolderPath == normalized ||
            (includeSubfolders && !string.IsNullOrEmpty(t.FolderPath) && t.FolderPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))).ToList();

        foreach (var t in toUpdate)
            t.IsEnabled = enable;
        await _db.SaveChangesAsync();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return Json(new { updated = toUpdate.Count, enable });

        return RedirectToAction("Index", new { folder = folderPath, includeSubfolders });
    }

    [HttpPost]
    public async Task<IActionResult> RateAttempt(int attemptId, List<double?> scores)
    {
        var attempt = await _db.Attempts
            .Include(a => a.Topic)
            .SingleOrDefaultAsync(a => a.Id == attemptId);
        if (attempt == null || attempt.Topic.Type != TopicType.Open)
            return NotFound();

        if (string.IsNullOrEmpty(attempt.ResultJson))
            return RedirectToAction("Result", "Test", new { id = attemptId });

        var details = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.Nodes.JsonObject>>(attempt.ResultJson);
        if (details == null)
            return RedirectToAction("Result", "Test", new { id = attemptId });

        var scoreList = scores ?? new List<double?>();
        double sum = 0;
        int count = 0;
        for (int i = 0; i < Math.Min(details.Count, scoreList.Count); i++)
        {
            var s = scoreList[i];
            if (s.HasValue)
            {
                var pct = Math.Clamp(s.Value, 0, 100);
                details[i]["ScorePercent"] = pct;
                sum += pct;
                count++;
            }
        }

        attempt.ScorePercent = count > 0 ? Math.Round(sum / count, 2) : null;
        attempt.ResultJson = System.Text.Json.JsonSerializer.Serialize(details, new System.Text.Json.JsonSerializerOptions
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        await _db.SaveChangesAsync();

        return RedirectToAction("Result", "Test", new { id = attemptId });
    }

    [HttpGet]
    public IActionResult ImportTests()
    {
        return View();
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ImportFormat()
    {
        var path = Path.Combine(_env.ContentRootPath, "TEST-IMPORT-FORMAT.md");
        if (!System.IO.File.Exists(path))
            return NotFound();
        return PhysicalFile(path, "text/markdown", "TEST-IMPORT-FORMAT.md");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ParseImportText(string importText)
    {
        var result = _testImportService.Parse(importText ?? "");
        var itemsDto = result.Items.Select(x => new { path = x.Path, content = x.Content }).ToList();
        return Json(new { items = itemsDto, errors = result.Errors });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFromImportText(string importText)
    {
        var parseResult = _testImportService.Parse(importText ?? "");
        if (parseResult.Errors.Count > 0)
            return Json(new { success = false, message = "Ошибки разбора: " + string.Join("; ", parseResult.Errors) });

        var (created, failed) = await _testImportService.CreateFilesAsync(parseResult.Items);

        if (created.Count > 0)
            _testFileService.SyncTopicsFromFiles(force: true);

        var message = created.Count > 0
            ? $"Тесты успешно созданы! Создано файлов: {created.Count}"
            : "Ничего не создано";
        return Json(new
        {
            success = failed.Count == 0,
            created,
            failed,
            message
        });
    }

    public async Task<IActionResult> History()
    {
        var attempts = await _db.Attempts
            .Include(a => a.Topic)
            .Include(a => a.User)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync();

        return View(attempts);
    }
}

