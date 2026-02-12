namespace HomeCenter.Models;

/// <summary>
/// Тип действия в истории версий тестов.
/// </summary>
public enum TestHistoryActionType
{
    /// <summary>Тест добавлен (новый файл).</summary>
    Added = 0,

    /// <summary>Содержимое теста изменено.</summary>
    Modified = 1,

    /// <summary>Файл удалён физически с диска.</summary>
    FileDeleted = 2,

    /// <summary>Тест удалён из БД (админ).</summary>
    DeletedFromDb = 3,

    /// <summary>Папка удалена (админ).</summary>
    FolderDeleted = 4
}

/// <summary>
/// Запись в истории версий тестов для отслеживания изменений.
/// </summary>
public class TestHistoryEntry
{
    public int Id { get; set; }

    /// <summary>
    /// Относительный путь к файлу от папки tests (например "География\Урок 5\test.txt").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Путь папки (для FolderDeleted — путь удалённой папки).
    /// </summary>
    public string? FolderPath { get; set; }

    public TestHistoryActionType Action { get; set; }

    /// <summary>
    /// Содержимое файла при добавлении/изменении. Null при удалении.
    /// </summary>
    public string? Content { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
