using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using QuizApp.Utils;

namespace QuizApp.Services;

public class TestImportService : ITestImportService
{
    private const string ImportFilePrefix = "ФАЙЛ:";
    private readonly string _testsFolder;

    public TestImportService(IWebHostEnvironment env)
    {
        _testsFolder = Path.Combine(env.ContentRootPath, "tests");
    }

    public ImportResult Parse(string text)
    {
        var items = new List<ImportItem>();
        var errors = new List<string>();
        var blocks = Regex.Split(text ?? "", @"(?m)^==+\s*$", RegexOptions.Multiline);

        foreach (var block in blocks)
        {
            var trimmed = block.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            var firstLineEnd = trimmed.IndexOf('\n');
            var firstLine = firstLineEnd >= 0 ? trimmed.Substring(0, firstLineEnd).Trim() : trimmed;
            if (!firstLine.StartsWith(ImportFilePrefix, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Блок без ФАЙЛ:: \"{firstLine.Substring(0, Math.Min(50, firstLine.Length))}...\"");
                continue;
            }

            var path = firstLine.Substring(ImportFilePrefix.Length).Trim(':', ' ', '\t');
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

            items.Add(new ImportItem(path, content));
        }

        return new ImportResult(items, errors);
    }

    public async Task<(IReadOnlyList<string> Created, IReadOnlyList<string> Failed)> CreateFilesAsync(
        IReadOnlyList<ImportItem> items,
        CancellationToken cancellationToken = default)
    {
        var created = new List<string>();
        var failed = new List<string>();

        foreach (var item in items)
        {
            var fullPath = Path.Combine(_testsFolder, PathHelper.Normalize(item.Path));
            var dir = Path.GetDirectoryName(fullPath)!;
            try
            {
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                await File.WriteAllTextAsync(fullPath, item.Content, System.Text.Encoding.UTF8, cancellationToken);
                created.Add(item.Path);
            }
            catch (Exception ex)
            {
                failed.Add($"{item.Path}: {ex.Message}");
            }
        }

        return (created, failed);
    }
}
