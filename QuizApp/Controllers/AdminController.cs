using System.IO;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ITestFileService _testFileService;
    private readonly IWebHostEnvironment _env;

    public AdminController(
        ApplicationDbContext db,
        IConfiguration configuration,
        ITestFileService testFileService,
        IWebHostEnvironment env)
    {
        _db = db;
        _configuration = configuration;
        _testFileService = testFileService;
        _env = env;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login(string userName, string password, string? returnUrl = null)
    {
        var adminUser = _configuration["Admin:Username"];
        var adminPassword = _configuration["Admin:Password"];

        if (userName == adminUser && password == adminPassword)
        {
            var admin = await _db.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (admin == null)
            {
                admin = new ApplicationUser { UserName = userName, Password = "" };
                _db.Users.Add(admin);
                await _db.SaveChangesAsync();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index");
        }

        ModelState.AddModelError(string.Empty, "Неверный логин или пароль администратора.");
        return View();
    }

    public async Task<IActionResult> Index(string? folder = null, bool? includeSubfolders = null)
    {
        // Каждый раз при входе в админку перечитываем файлы из папки tests
        // и синхронизируем список тем.
        _testFileService.SyncTopicsFromFiles();

        var allTopics = await _db.Topics.OrderBy(t => t.Title).ToListAsync();
        var tree = TestTreeNode.BuildTree(allTopics);

        string currentFolderPath = string.IsNullOrWhiteSpace(folder) ? string.Empty
            : folder.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

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

        ViewBag.TotalUsers = totalUsers;
        ViewBag.TotalAttempts = totalAttempts;
        ViewBag.TreeRoot = tree;
        ViewBag.CurrentFolderPath = currentFolderPath ?? string.Empty;
        ViewBag.IncludeSubfolders = includeSubfolders ?? true;
        ViewBag.FolderDisplay = string.IsNullOrEmpty(currentFolderPath)
            ? "Все темы"
            : string.Join(" / ", currentFolderPath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries));

        return View(topicsInFolder.ToList());
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

        var normalized = folderPath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
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
        var (items, errors) = ParseImportFormat(importText ?? "");
        return Json(new { items, errors });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFromImportText(string importText)
    {
        var (items, errors) = ParseImportFormat(importText ?? "");
        if (errors.Count > 0)
            return Json(new { success = false, message = "Ошибки разбора: " + string.Join("; ", errors) });

        var testsFolder = Path.Combine(_env.ContentRootPath, "tests");
        var created = new List<string>();
        var failed = new List<string>();

        foreach (var item in items)
        {
            var fullPath = Path.Combine(testsFolder, item.Path.Replace('/', Path.DirectorySeparatorChar));
            var dir = Path.GetDirectoryName(fullPath)!;
            try
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllTextAsync(fullPath, item.Content, System.Text.Encoding.UTF8);
                created.Add(item.Path);
            }
            catch (Exception ex)
            {
                failed.Add($"{item.Path}: {ex.Message}");
            }
        }

        if (created.Count > 0)
            _testFileService.SyncTopicsFromFiles();

        return Json(new
        {
            success = failed.Count == 0,
            created,
            failed,
            message = created.Count > 0 ? $"Создано файлов: {created.Count}" : "Ничего не создано"
        });
    }

    private static (List<(string Path, string Content)> items, List<string> errors) ParseImportFormat(string text)
    {
        var items = new List<(string Path, string Content)>();
        var errors = new List<string>();
        var blocks = Regex.Split(text, @"(?m)^==+\s*$", RegexOptions.Multiline);

        foreach (var block in blocks)
        {
            var trimmed = block.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            var firstLineEnd = trimmed.IndexOf('\n');
            var firstLine = firstLineEnd >= 0 ? trimmed.Substring(0, firstLineEnd).Trim() : trimmed;
            if (!firstLine.StartsWith("ФАЙЛ:", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Блок без ФАЙЛ:: \"{firstLine.Substring(0, Math.Min(50, firstLine.Length))}...\"");
                continue;
            }

            var path = firstLine.Substring(5).Trim(':', ' ', '\t');
            if (string.IsNullOrWhiteSpace(path))
            {
                errors.Add("Пустой путь в ФАЙЛ:");
                continue;
            }
            if (!path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                path += ".txt";
            if (path.Contains("..") || path.StartsWith("/") || path.StartsWith("\\"))
            {
                errors.Add($"Недопустимый путь: {path}");
                continue;
            }

            var content = firstLineEnd >= 0 ? trimmed.Substring(firstLineEnd + 1).Trim() : "";
            if (string.IsNullOrWhiteSpace(content))
            {
                errors.Add($"Пустое содержимое для {path}");
                continue;
            }

            items.Add((path, content));
        }

        return (items, errors);
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

