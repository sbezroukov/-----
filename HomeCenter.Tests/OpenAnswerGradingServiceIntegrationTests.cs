using System.Reflection;
using HomeCenter.Models;
using HomeCenter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HomeCenter.Tests;

/// <summary>
/// Интеграционный тест — отправляет реальный запрос к Qwen API.
/// Ключ берётся из appsettings.Development.json (см. appsettings.Development.json.example).
/// Запуск: dotnet test --filter "Category=Integration"
/// </summary>
[Trait("Category", "Integration")]
public class OpenAnswerGradingServiceIntegrationTests
{
    [Fact]
    public async Task GradeAsync_RealQwenApi_ReturnsScores()
    {
        var homeCenterDir = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "..", "..", "..", "..", "HomeCenter"));
        var devSettingsPath = Path.Combine(homeCenterDir, "appsettings.Development.json");

        if (!File.Exists(devSettingsPath))
        {
            return; // Пропуск: appsettings.Development.json не найден (скопируйте из .example)
        }

        var config = new ConfigurationBuilder()
            .SetBasePath(homeCenterDir)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var apiKey = config["Qwen:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("ЗАМЕНИТЕ"))
        {
            return; // Пропуск: Qwen:ApiKey не задан в appsettings.Development.json
        }

        var services = new ServiceCollection();
        services.AddHttpClient();
        var provider = services.BuildServiceProvider();
        var httpFactory = provider.GetRequiredService<IHttpClientFactory>();

        var service = new OpenAnswerGradingService(
            httpFactory,
            config,
            NullLogger<OpenAnswerGradingService>.Instance);

        var topic = new Topic
        {
            Id = 1,
            Title = "Тест интеграции",
            FileName = "Test\\integration.txt",
            Type = TopicType.Open
        };

        var items = new List<GradingItem>
        {
            new() { Question = "Сколько будет 2+2?", StudentAnswer = "4", CorrectAnswer = "4" },
            new() { Question = "Назовите столицу России", StudentAnswer = "Москва", CorrectAnswer = "Москва" }
        };

        var result = await service.GradeAsync(topic, items);

        Assert.NotEmpty(result);
        Assert.Equal(items.Count, result.Count);
        foreach (var score in result)
        {
            Assert.NotNull(score);
            Assert.InRange(score!.Value, 0, 100);
        }
    }
}
