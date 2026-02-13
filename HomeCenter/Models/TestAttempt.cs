using System;

namespace HomeCenter.Models;

/// <summary>
/// Статус обработки AI для открытых ответов.
/// </summary>
public enum GradingStatus
{
    /// <summary>Не требует обработки (закрытые тесты, самопроверка).</summary>
    NotRequired = 0,

    /// <summary>Ожидает обработки AI.</summary>
    Pending = 1,

    /// <summary>В процессе обработки AI.</summary>
    Processing = 2,

    /// <summary>Обработка завершена успешно.</summary>
    Completed = 3,

    /// <summary>Ошибка при обработке AI.</summary>
    Failed = 4
}

public class TestAttempt
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int TopicId { get; set; }
    public Topic Topic { get; set; } = null!;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Количество вопросов в момент прохождения.
    /// </summary>
    public int TotalQuestions { get; set; }

    /// <summary>
    /// Количество правильных ответов (для Test).
    /// </summary>
    public int? CorrectAnswers { get; set; }

    /// <summary>
    /// Итоговый процент/оценка (0-100). Для открытых/самопроверки может быть null.
    /// </summary>
    public double? ScorePercent { get; set; }

    /// <summary>
    /// Сырые данные ответов в виде JSON (вопросы, ответы пользователя, правильные ответы и т.п.).
    /// </summary>
    public string? ResultJson { get; set; }

    /// <summary>
    /// Статус обработки AI для открытых ответов.
    /// </summary>
    public GradingStatus GradingStatus { get; set; } = GradingStatus.NotRequired;

    /// <summary>
    /// Дата последнего обновления статуса или результатов.
    /// </summary>
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Сообщение об ошибке при неудачной обработке AI.
    /// </summary>
    public string? GradingError { get; set; }
}

