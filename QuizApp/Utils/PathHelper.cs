using System.IO;

namespace QuizApp.Utils;

/// <summary>
/// Утилита для работы с путями. Приводит пути к формату текущей ОС.
/// </summary>
public static class PathHelper
{
    /// <summary>
    /// Нормализует путь к разделителю текущей ОС (в БД пути хранятся с \ для совместимости).
    /// </summary>
    public static string Normalize(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        return path.Replace('\\', Path.DirectorySeparatorChar)
                   .Replace('/', Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// Путь для отображения: "folder1 / folder2 / folder3".
    /// </summary>
    public static string ToDisplayPath(string path, string emptyLabel = "Все")
    {
        if (string.IsNullOrEmpty(path)) return emptyLabel;
        var parts = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" / ", parts);
    }
}
