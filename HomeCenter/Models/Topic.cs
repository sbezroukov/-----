using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using HomeCenter.Utils;

namespace HomeCenter.Models;

public enum TopicType
{
    Test = 0,      // Варианты ответов + правильный
    Open = 1,      // Открытые вопросы без правильного ответа
    SelfStudy = 2  // Вопросы для самопроверки, без ввода ответов
}

public class Topic
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Относительный путь к txt-файлу от папки tests (включая подпапки), например "География\Урок 5\test.txt".
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    public TopicType Type { get; set; }

    [NotMapped]
    public string TypeDisplayName => Type switch
    {
        TopicType.Test => "Тестирование",
        TopicType.Open => "Открытые вопросы",
        TopicType.SelfStudy => "Вопросы перед чтением",
        _ => Type.ToString()
    };

    /// <summary>
    /// Путь для отображения: категории и название теста, например "География / Урок 5 / test".
    /// </summary>
    [NotMapped]
    public string DisplayPath
    {
        get
        {
            var dir = Path.GetDirectoryName(PathHelper.Normalize(FileName));
            if (string.IsNullOrEmpty(dir)) return Title;
            return PathHelper.ToDisplayPath(dir, Title) + " / " + Title;
        }
    }

    /// <summary>
    /// Путь папки (категории) без имени файла, для группировки в дереве.
    /// </summary>
    [NotMapped]
    public string FolderPath => Path.GetDirectoryName(PathHelper.Normalize(FileName)) ?? string.Empty;

    /// <summary>
    /// Разрешена ли тема для прохождения обычным пользователям.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Отмечен как удалённый (файл отсутствует на диске или удалён админом).
    /// </summary>
    public bool IsDeleted { get; set; }

    public ICollection<TestAttempt> Attempts { get; set; } = new List<TestAttempt>();
}

