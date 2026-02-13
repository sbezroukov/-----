using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using HomeCenter.Models;
using Microsoft.Extensions.Logging;

namespace HomeCenter.Services;

public class OpenAnswerGradingService : IOpenAnswerGradingService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAnswerGradingService> _logger;

    private const string DefaultQwenBaseUrl = "https://dashscope-intl.aliyuncs.com/api/v1";
    private const string DefaultQwenModel = "qwen-turbo";
    private const string DefaultOpenRouterBaseUrl = "https://openrouter.ai/api/v1";
    private const string DefaultOpenRouterModel = "openrouter/free";

    public OpenAnswerGradingService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OpenAnswerGradingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<double?>> GradeAsync(Topic topic, List<GradingItem> items, CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
            return new List<double?>();

        // Определяем провайдера
        var provider = _configuration["AI:Provider"] ?? "Qwen";
        
        return provider.ToLowerInvariant() switch
        {
            "openrouter" => await GradeWithOpenRouterAsync(topic, items, cancellationToken),
            "qwen" => await GradeWithQwenAsync(topic, items, cancellationToken),
            _ => await GradeWithQwenAsync(topic, items, cancellationToken) // По умолчанию Qwen
        };
    }

    private async Task<IReadOnlyList<double?>> GradeWithQwenAsync(Topic topic, List<GradingItem> items, CancellationToken cancellationToken)
    {
        var apiKey = _configuration["Qwen:ApiKey"] ?? Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY");
        var enabled = _configuration.GetValue<bool>("Qwen:Enabled");
        if (!enabled || string.IsNullOrWhiteSpace(apiKey))
            return new List<double?>();

        var baseUrl = _configuration["Qwen:BaseUrl"] ?? DefaultQwenBaseUrl;
        var model = _configuration["Qwen:Model"] ?? DefaultQwenModel;
        var url = $"{baseUrl.TrimEnd('/')}/services/aigc/text-generation/generation";

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(topic, items);

        var requestBody = new
        {
            model,
            input = new
            {
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
            },
            parameters = new { result_format = "message" }
        };

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey);
            request.Content = content;

            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Qwen API error {StatusCode}: {Body}", response.StatusCode, body);
                return new List<double?>();
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var scores = ParseQwenScoresFromResponse(responseJson, items.Count);
            return scores;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка вызова Qwen API для оценки открытых ответов");
            return new List<double?>();
        }
    }

    private async Task<IReadOnlyList<double?>> GradeWithOpenRouterAsync(Topic topic, List<GradingItem> items, CancellationToken cancellationToken)
    {
        var apiKey = _configuration["AI:ApiKey"] ?? _configuration["OpenRouter:ApiKey"];
        var enabled = _configuration.GetValue<bool>("AI:Enabled", true);
        if (!enabled || string.IsNullOrWhiteSpace(apiKey))
            return new List<double?>();

        var baseUrl = _configuration["AI:BaseUrl"] ?? _configuration["OpenRouter:BaseUrl"] ?? DefaultOpenRouterBaseUrl;
        var model = _configuration["AI:Model"] ?? _configuration["OpenRouter:Model"] ?? DefaultOpenRouterModel;
        var url = $"{baseUrl.TrimEnd('/')}/chat/completions";

        var systemPrompt = BuildSystemPrompt();
        var userPrompt = BuildUserPrompt(topic, items);

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(60);

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + apiKey);
            request.Headers.TryAddWithoutValidation("HTTP-Referer", "https://github.com/HomeCenter");
            request.Headers.TryAddWithoutValidation("X-Title", "HomeCenter Quiz App");
            request.Content = content;

            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("OpenRouter API error {StatusCode}: {Body}", response.StatusCode, body);
                return new List<double?>();
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var scores = ParseOpenAIStyleScoresFromResponse(responseJson, items.Count);
            return scores;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка вызова OpenRouter API для оценки открытых ответов");
            return new List<double?>();
        }
    }

    private static string BuildSystemPrompt()
    {
        return @"Ты — эксперт по оценке учебных ответов.

Для каждого вопроса оцени степень соответствия ответа ученика эталону по шкале 0–100.
Учитывай: смысловую правильность, полноту, терминологию. Синонимы и пересказ своими словами допускаются.

Верни ТОЛЬКО JSON-массив чисел, например: [85, 90, 70, 0]
Порядок соответствует порядку вопросов. Если ответ пустой или не по теме — 0.";
    }

    private static string BuildUserPrompt(Topic topic, List<GradingItem> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Контекст:");
        sb.AppendLine($"- Предмет/категория: {topic.DisplayPath}");
        sb.AppendLine($"- Тема: {topic.Title}");
        sb.AppendLine($"- Файл: {topic.FileName}");
        sb.AppendLine();
        sb.AppendLine("Вопросы и ответы:");
        sb.AppendLine();

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            sb.AppendLine($"=== Вопрос {i + 1} ===");
            sb.AppendLine($"Вопрос: {item.Question}");
            sb.AppendLine($"Ответ ученика: {item.StudentAnswer}");
            sb.AppendLine($"Эталон: {item.CorrectAnswer ?? "—"}");
            sb.AppendLine();
        }

        sb.AppendLine("Верни ТОЛЬКО JSON-массив чисел в том же порядке.");
        return sb.ToString();
    }

    private static IReadOnlyList<double?> ParseQwenScoresFromResponse(string responseJson, int expectedCount)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var content = root
                .GetProperty("output")
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content");

            string text;
            if (content.ValueKind == JsonValueKind.String)
                text = content.GetString() ?? "";
            else if (content.ValueKind == JsonValueKind.Array)
            {
                var first = content[0];
                text = first.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
            }
            else
                text = "";

            return ExtractScoresFromText(text, expectedCount);
        }
        catch (Exception)
        {
            return new List<double?>();
        }
    }

    private static IReadOnlyList<double?> ParseOpenAIStyleScoresFromResponse(string responseJson, int expectedCount)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var content = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            return ExtractScoresFromText(content, expectedCount);
        }
        catch (Exception)
        {
            return new List<double?>();
        }
    }

    private static IReadOnlyList<double?> ExtractScoresFromText(string text, int expectedCount)
    {
        // Ищем JSON-массив в ответе (модель может добавить пояснения)
        var match = Regex.Match(text, @"\[[\d\s,\.]+\]");
        if (!match.Success)
            return new List<double?>();

        var arrayJson = match.Value;
        var scores = JsonSerializer.Deserialize<double[]>(arrayJson);
        if (scores == null || scores.Length == 0)
            return new List<double?>();

        var result = new List<double?>();
        for (var i = 0; i < expectedCount; i++)
        {
            var score = i < scores.Length ? Math.Clamp(scores[i], 0, 100) : (double?)null;
            result.Add(score);
        }
        return result;
    }
}
