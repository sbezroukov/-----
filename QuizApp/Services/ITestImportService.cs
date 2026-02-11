namespace QuizApp.Services;

public record ImportItem(string Path, string Content);

public record ImportResult(IReadOnlyList<ImportItem> Items, IReadOnlyList<string> Errors);

public interface ITestImportService
{
    /// <summary>
    /// Разбирает текст импорта на блоки ФАЙЛ: путь — содержимое.
    /// </summary>
    ImportResult Parse(string text);

    /// <summary>
    /// Создаёт файлы тестов в папке tests. Возвращает пути созданных и ошибки.
    /// </summary>
    Task<(IReadOnlyList<string> Created, IReadOnlyList<string> Failed)> CreateFilesAsync(
        IReadOnlyList<ImportItem> items,
        CancellationToken cancellationToken = default);
}
