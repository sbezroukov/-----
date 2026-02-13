using System.Net;
using System.Text;
using System.Text.Json;
using HomeCenter.Models;
using HomeCenter.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HomeCenter.Tests;

public class OpenAnswerGradingServiceTests
{
    private static Topic CreateTopic() => new()
    {
        Id = 1,
        Title = "Тест алгебры",
        FileName = "8 класс\\Алгебра\\Открытые_вопросы.txt",
        Type = TopicType.Open
    };

    private static List<GradingItem> CreateGradingItems() => new()
    {
        new() { Question = "Что такое уравнение?", StudentAnswer = "Равенство с переменной", CorrectAnswer = "Уравнение — равенство с неизвестной" },
        new() { Question = "Решите 2x=4", StudentAnswer = "x=2", CorrectAnswer = "x=2" }
    };

    private static ILogger<OpenAnswerGradingService> CreateNullLogger() =>
        NullLogger<OpenAnswerGradingService>.Instance;

    private static IHttpClientFactory CreateHttpClientFactory(HttpResponseMessage response)
    {
        var handler = new MockHttpMessageHandler(response);
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://test") };
        return new TestHttpClientFactory(client);
    }

    private static IConfiguration CreateConfiguration(
        string? apiKey = "sk-test",
        bool enabled = true,
        string? baseUrl = null,
        string? model = null)
    {
        var dict = new Dictionary<string, string?>
        {
            ["Qwen:ApiKey"] = apiKey,
            ["Qwen:Enabled"] = enabled.ToString().ToLowerInvariant(),
            ["Qwen:BaseUrl"] = baseUrl ?? "https://dashscope-intl.aliyuncs.com/api/v1",
            ["Qwen:Model"] = model ?? "qwen-turbo"
        };
        return new ConfigurationBuilder().AddInMemoryCollection(dict!).Build();
    }

    [Fact]
    public async Task GradeAsync_WhenDisabled_ReturnsEmpty()
    {
        var config = CreateConfiguration(enabled: false);
        var factory = CreateHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK));
        var service = new OpenAnswerGradingService(factory, config, CreateNullLogger());

        var result = await service.GradeAsync(CreateTopic(), CreateGradingItems());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GradeAsync_WhenApiKeyEmpty_ReturnsEmpty()
    {
        var config = CreateConfiguration(apiKey: "");
        var factory = CreateHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK));
        var service = new OpenAnswerGradingService(factory, config, CreateNullLogger());

        var result = await service.GradeAsync(CreateTopic(), CreateGradingItems());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GradeAsync_WhenItemsEmpty_ReturnsEmpty()
    {
        var config = CreateConfiguration();
        var factory = CreateHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK));
        var service = new OpenAnswerGradingService(factory, config, CreateNullLogger());

        var result = await service.GradeAsync(CreateTopic(), new List<GradingItem>());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GradeAsync_WhenHttpError_ReturnsEmpty()
    {
        var config = CreateConfiguration();
        var factory = CreateHttpClientFactory(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var service = new OpenAnswerGradingService(factory, config, CreateNullLogger());

        var result = await service.GradeAsync(CreateTopic(), CreateGradingItems());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GradeAsync_WhenValidResponse_ReturnsScores()
    {
        var qwenResponse = new
        {
            output = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = "[85, 90]"
                        }
                    }
                }
            }
        };
        var responseBody = JsonSerializer.Serialize(qwenResponse);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };

        var config = CreateConfiguration();
        var factory = CreateHttpClientFactory(response);
        var service = new OpenAnswerGradingService(factory, config, CreateNullLogger());

        var result = await service.GradeAsync(CreateTopic(), CreateGradingItems());

        Assert.Equal(2, result.Count);
        Assert.Equal(85, result[0]);
        Assert.Equal(90, result[1]);
    }

    [Fact]
    public async Task GradeAsync_WhenResponseHasTextWithJsonArray_ExtractsScores()
    {
        var qwenResponse = new
        {
            output = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = "Вот оценки: [70, 80, 95]"
                        }
                    }
                }
            }
        };
        var responseBody = JsonSerializer.Serialize(qwenResponse);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };

        var config = CreateConfiguration();
        var factory = CreateHttpClientFactory(response);
        var service = new OpenAnswerGradingService(factory, config, CreateNullLogger());

        var items = new List<GradingItem>
        {
            new() { Question = "Q1", StudentAnswer = "A1", CorrectAnswer = "C1" },
            new() { Question = "Q2", StudentAnswer = "A2", CorrectAnswer = "C2" },
            new() { Question = "Q3", StudentAnswer = "A3", CorrectAnswer = "C3" }
        };

        var result = await service.GradeAsync(CreateTopic(), items);

        Assert.Equal(3, result.Count);
        Assert.Equal(70, result[0]);
        Assert.Equal(80, result[1]);
        Assert.Equal(95, result[2]);
    }

    [Fact]
    public async Task GradeAsync_WhenScoresOutOfRange_ClampsTo0_100()
    {
        var responseBody = """{"output":{"choices":[{"message":{"content":"[150, 0]"}}]}}""";
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };

        var config = CreateConfiguration();
        var factory = CreateHttpClientFactory(response);
        var service = new OpenAnswerGradingService(factory, config, CreateNullLogger());

        var result = await service.GradeAsync(CreateTopic(), CreateGradingItems());

        Assert.Equal(2, result.Count);
        Assert.Equal(100, result[0]); // 150 clamped to 100
        Assert.Equal(0, result[1]);
    }

    [Fact]
    public async Task GradeAsync_WhenInvalidJson_ReturnsEmpty()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{ invalid json }", Encoding.UTF8, "application/json")
        };

        var config = CreateConfiguration();
        var factory = CreateHttpClientFactory(response);
        var service = new OpenAnswerGradingService(factory, config, CreateNullLogger());

        var result = await service.GradeAsync(CreateTopic(), CreateGradingItems());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GradeAsync_WhenContentIsArray_ExtractsText()
    {
        var qwenResponse = new
        {
            output = new
            {
                choices = new[]
                {
                    new
                    {
                        message = new
                        {
                            content = new[] { new { text = "[60, 75]" } }
                        }
                    }
                }
            }
        };
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var responseBody = JsonSerializer.Serialize(qwenResponse, options);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
        };

        var config = CreateConfiguration();
        var factory = CreateHttpClientFactory(response);
        var service = new OpenAnswerGradingService(factory, config, CreateNullLogger());

        var result = await service.GradeAsync(CreateTopic(), CreateGradingItems());

        Assert.Equal(2, result.Count);
        Assert.Equal(60, result[0]);
        Assert.Equal(75, result[1]);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public MockHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }

    private class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public TestHttpClientFactory(HttpClient client) => _client = client;

        public HttpClient CreateClient(string name) => _client;
    }
}
