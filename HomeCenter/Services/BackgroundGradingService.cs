using HomeCenter.Data;
using HomeCenter.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HomeCenter.Services;

/// <summary>
/// Фоновый сервис для асинхронной обработки оценки открытых ответов через AI.
/// </summary>
public class BackgroundGradingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundGradingService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    public BackgroundGradingService(
        IServiceProvider serviceProvider,
        ILogger<BackgroundGradingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundGradingService запущен");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingAttemptsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в BackgroundGradingService");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("BackgroundGradingService остановлен");
    }

    private async Task ProcessPendingAttemptsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var gradingService = scope.ServiceProvider.GetRequiredService<IOpenAnswerGradingService>();
        var testFileService = scope.ServiceProvider.GetRequiredService<ITestFileService>();

        // Получаем попытки в статусе Pending
        var pendingAttempts = await db.Attempts
            .Include(a => a.Topic)
            .Where(a => a.GradingStatus == GradingStatus.Pending)
            .OrderBy(a => a.StartedAt)
            .Take(10) // Обрабатываем до 10 попыток за раз
            .ToListAsync(cancellationToken);

        if (pendingAttempts.Count == 0)
            return;

        _logger.LogInformation("Найдено {Count} попыток для обработки AI", pendingAttempts.Count);

        foreach (var attempt in pendingAttempts)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            await ProcessAttemptAsync(attempt, db, gradingService, testFileService, cancellationToken);
        }
    }

    private async Task ProcessAttemptAsync(
        TestAttempt attempt,
        ApplicationDbContext db,
        IOpenAnswerGradingService gradingService,
        ITestFileService testFileService,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Обработка попытки ID={AttemptId}, TopicId={TopicId}", attempt.Id, attempt.TopicId);

            // Устанавливаем статус "В обработке"
            attempt.GradingStatus = GradingStatus.Processing;
            attempt.LastUpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);

            // Парсим результаты
            if (string.IsNullOrEmpty(attempt.ResultJson))
            {
                throw new InvalidOperationException("ResultJson пуст");
            }

            var detailsList = JsonSerializer.Deserialize<List<JsonElement>>(attempt.ResultJson);
            if (detailsList == null || detailsList.Count == 0)
            {
                throw new InvalidOperationException("Не удалось распарсить ResultJson");
            }

            // Формируем список вопросов для оценки
            var gradingItems = new List<GradingItem>();
            foreach (var detail in detailsList)
            {
                var question = detail.TryGetProperty("Question", out var q) ? q.GetString() : "";
                var answer = detail.TryGetProperty("Answer", out var a) ? a.GetString() : "";
                var correct = detail.TryGetProperty("Correct", out var c) ? c.GetString() : "";

                gradingItems.Add(new GradingItem
                {
                    Question = question ?? "",
                    StudentAnswer = answer ?? "",
                    CorrectAnswer = correct ?? ""
                });
            }

            // Вызываем AI для оценки
            var scores = await gradingService.GradeAsync(attempt.Topic, gradingItems, cancellationToken);

            if (scores == null || scores.Count == 0)
            {
                throw new InvalidOperationException("AI не вернул оценки (возможно, AI отключен или недоступен)");
            }

            // Обновляем результаты с оценками
            var updatedDetailsList = new List<Dictionary<string, object>>();
            for (int i = 0; i < detailsList.Count; i++)
            {
                var detail = detailsList[i];
                var dict = new Dictionary<string, object>
                {
                    ["Question"] = detail.TryGetProperty("Question", out var q) ? q.GetString() ?? "" : "",
                    ["Answer"] = detail.TryGetProperty("Answer", out var a) ? a.GetString() ?? "" : "",
                    ["Correct"] = detail.TryGetProperty("Correct", out var c) ? c.GetString() ?? "" : ""
                };

                if (i < scores.Count && scores[i].HasValue)
                {
                    dict["ScorePercent"] = Math.Round(scores[i]!.Value, 2);
                }

                updatedDetailsList.Add(dict);
            }

            // Сохраняем обновленный JSON
            attempt.ResultJson = JsonSerializer.Serialize(updatedDetailsList, new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            // Вычисляем средний балл
            var scoredCount = scores.Count(s => s.HasValue);
            if (scoredCount > 0)
            {
                attempt.ScorePercent = Math.Round(scores.Where(s => s.HasValue).Average(s => s!.Value), 2);
            }

            // Устанавливаем статус "Завершено"
            attempt.GradingStatus = GradingStatus.Completed;
            attempt.LastUpdatedAt = DateTime.UtcNow;
            attempt.GradingError = null;

            await db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Попытка ID={AttemptId} успешно обработана, средний балл: {Score}%",
                attempt.Id, attempt.ScorePercent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при обработке попытки ID={AttemptId}", attempt.Id);

            // Устанавливаем статус "Ошибка"
            attempt.GradingStatus = GradingStatus.Failed;
            attempt.LastUpdatedAt = DateTime.UtcNow;
            attempt.GradingError = $"{ex.GetType().Name}: {ex.Message}";

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Не удалось сохранить статус ошибки для попытки ID={AttemptId}", attempt.Id);
            }
        }
    }
}
