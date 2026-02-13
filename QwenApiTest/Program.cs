using System.Net.Http;
using System.Text;
using System.Text.Json;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("=== Проверка доступных AI моделей ===");
Console.ResetColor();
Console.WriteLine();

var successfulModels = new List<(string Provider, string Model, string BaseUrl, string ApiKey)>();

// ============================================
// 1. QWEN (Alibaba DashScope)
// ============================================
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("1. QWEN (Alibaba DashScope)");
Console.ResetColor();

var qwenApiKey = "sk-51335b6e2ede4d5596b7e41fae16bcae";
var qwenRegions = new[]
{
    new { Name = "Международный", BaseUrl = "https://dashscope-intl.aliyuncs.com/api/v1" }
};

var qwenModels = new[] { "qwen-turbo", "qwen-plus" };

foreach (var region in qwenRegions)
{
    Console.WriteLine($"  Регион: {region.Name}");
    foreach (var model in qwenModels)
    {
        var result = await TestQwenModel(region.BaseUrl, model, qwenApiKey);
        if (result.Success)
        {
            successfulModels.Add(("Qwen", model, region.BaseUrl, qwenApiKey));
        }
    }
}

Console.WriteLine();

// ============================================
// 2. OPENROUTER - Сначала получаем список моделей
// ============================================
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("2. OpenRouter.ai - Получение списка доступных моделей");
Console.ResetColor();

var openRouterApiKey = "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";
var openRouterBaseUrl = "https://openrouter.ai/api/v1";

var availableModels = await GetOpenRouterModels(openRouterApiKey);
if (availableModels.Count > 0)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"  Найдено {availableModels.Count} бесплатных моделей");
    Console.ResetColor();
    Console.WriteLine();
    
    // Тестируем первые 5 бесплатных моделей
    var modelsToTest = availableModels.Take(5).ToList();
    foreach (var model in modelsToTest)
    {
        var result = await TestOpenRouterModel(openRouterBaseUrl, model, openRouterApiKey);
        if (result.Success)
        {
            successfulModels.Add(("OpenRouter", model, openRouterBaseUrl, openRouterApiKey));
        }
        await Task.Delay(500); // Задержка между запросами
    }
}
else
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("  Не удалось получить список моделей, пробуем известные бесплатные модели");
    Console.ResetColor();
    
    // Пробуем актуальные бесплатные модели без :free суффикса
    var fallbackModels = new[]
    {
        "meta-llama/llama-3.2-3b-instruct",
        "google/gemini-flash-1.5",
        "google/gemini-2.0-flash-exp",
        "qwen/qwen-2-7b-instruct",
        "mistralai/mistral-7b-instruct"
    };
    
    foreach (var model in fallbackModels)
    {
        var result = await TestOpenRouterModel(openRouterBaseUrl, model, openRouterApiKey);
        if (result.Success)
        {
            successfulModels.Add(("OpenRouter", model, openRouterBaseUrl, openRouterApiKey));
        }
        await Task.Delay(500);
    }
}

Console.WriteLine();

// ============================================
// ИТОГОВЫЙ ОТЧЕТ
// ============================================
Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("=== ИТОГОВЫЙ ОТЧЕТ ===");
Console.ResetColor();
Console.WriteLine();

if (successfulModels.Count > 0)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"✓ Найдено {successfulModels.Count} доступных моделей:");
    Console.ResetColor();
    Console.WriteLine();
    
    var groupedByProvider = successfulModels.GroupBy(x => x.Provider);
    foreach (var group in groupedByProvider)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"{group.Key}:");
        Console.ResetColor();
        foreach (var item in group)
        {
            Console.WriteLine($"  • {item.Model}");
        }
        Console.WriteLine();
    }
    
    var firstSuccess = successfulModels[0];
    
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Рекомендуемая конфигурация для appsettings.json:");
    Console.ResetColor();
    Console.WriteLine();
    
    if (firstSuccess.Provider == "OpenRouter")
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("  \"AI\": {");
        Console.WriteLine($"    \"Provider\": \"OpenRouter\",");
        Console.WriteLine($"    \"ApiKey\": \"{firstSuccess.ApiKey}\",");
        Console.WriteLine("    \"Enabled\": true,");
        Console.WriteLine($"    \"Model\": \"{firstSuccess.Model}\",");
        Console.WriteLine($"    \"BaseUrl\": \"{firstSuccess.BaseUrl}\"");
        Console.WriteLine("  }");
        Console.ResetColor();
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("  \"Qwen\": {");
        Console.WriteLine($"    \"ApiKey\": \"{firstSuccess.ApiKey}\",");
        Console.WriteLine("    \"Enabled\": true,");
        Console.WriteLine($"    \"Model\": \"{firstSuccess.Model}\",");
        Console.WriteLine($"    \"BaseUrl\": \"{firstSuccess.BaseUrl}\"");
        Console.WriteLine("  }");
        Console.ResetColor();
    }
}
else
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("✗ Ни одна модель не доступна!");
    Console.ResetColor();
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("Рекомендации:");
    Console.ResetColor();
    Console.WriteLine("1. Активируйте модели Qwen: https://dashscope-intl.console.aliyun.com/");
    Console.WriteLine("2. Проверьте ключ OpenRouter: https://openrouter.ai/keys");
    Console.WriteLine("3. Проверьте кредиты на OpenRouter: https://openrouter.ai/credits");
    Console.WriteLine("4. Попробуйте альтернативные провайдеры:");
    Console.WriteLine("   • Groq (бесплатный): https://console.groq.com/");
    Console.WriteLine("   • Together AI: https://api.together.xyz/");
}

// ============================================
// ФУНКЦИИ ТЕСТИРОВАНИЯ
// ============================================

async Task<List<string>> GetOpenRouterModels(string apiKey)
{
    var freeModels = new List<string>();
    
    try
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://openrouter.ai/api/v1/models");
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        
        using var response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(responseBody);
            
            if (jsonDoc.RootElement.TryGetProperty("data", out var data))
            {
                foreach (var model in data.EnumerateArray())
                {
                    if (model.TryGetProperty("id", out var id) && 
                        model.TryGetProperty("pricing", out var pricing))
                    {
                        // Проверяем, что модель бесплатная
                        var promptPrice = pricing.TryGetProperty("prompt", out var prompt) 
                            ? prompt.GetString() 
                            : "1";
                        var completionPrice = pricing.TryGetProperty("completion", out var completion) 
                            ? completion.GetString() 
                            : "1";
                        
                        if (promptPrice == "0" && completionPrice == "0")
                        {
                            freeModels.Add(id.GetString() ?? "");
                        }
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"  Ошибка получения списка моделей: {ex.Message}");
        Console.ResetColor();
    }
    
    return freeModels;
}

async Task<(bool Success, string Message)> TestQwenModel(string baseUrl, string model, string apiKey)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write($"    {model}... ");
    Console.ResetColor();

    var url = $"{baseUrl}/services/aigc/text-generation/generation";
    
    var requestBody = new
    {
        model = model,
        input = new
        {
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant." },
                new { role = "user", content = "Say 'OK'" }
            }
        },
        parameters = new { result_format = "message" }
    };

    try
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(15);

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        request.Content = content;

        using var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ РАБОТАЕТ");
            Console.ResetColor();
            return (true, "Success");
        }
        else
        {
            var jsonDoc = JsonDocument.Parse(responseBody);
            var errorCode = jsonDoc.RootElement.TryGetProperty("code", out var code) 
                ? code.GetString() ?? "Unknown" 
                : "Unknown";
            
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ {(int)response.StatusCode} ({errorCode})");
            Console.ResetColor();
            return (false, errorCode);
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ {ex.GetType().Name}");
        Console.ResetColor();
        return (false, ex.Message);
    }
}

async Task<(bool Success, string Message)> TestOpenRouterModel(string baseUrl, string model, string apiKey)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write($"  {model}... ");
    Console.ResetColor();

    var url = $"{baseUrl}/chat/completions";
    
    var requestBody = new
    {
        model = model,
        messages = new[]
        {
            new { role = "user", content = "Say 'OK'" }
        },
        max_tokens = 10
    };

    try
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {apiKey}");
        request.Headers.TryAddWithoutValidation("HTTP-Referer", "https://github.com/HomeCenter");
        request.Headers.TryAddWithoutValidation("X-Title", "HomeCenter Quiz App");
        request.Content = content;

        using var response = await client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("✓ РАБОТАЕТ");
            Console.ResetColor();
            
            try
            {
                var jsonDoc = JsonDocument.Parse(responseBody);
                var modelResponse = jsonDoc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
                
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($" (ответ: {modelResponse?.Trim()})");
                Console.ResetColor();
            }
            catch
            {
                Console.WriteLine();
            }
            
            return (true, "Success");
        }
        else
        {
            try
            {
                var jsonDoc = JsonDocument.Parse(responseBody);
                var errorMsg = jsonDoc.RootElement.TryGetProperty("error", out var error)
                    ? (error.TryGetProperty("message", out var msg) ? msg.GetString() : error.GetString())
                    : "Unknown error";
                
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ {(int)response.StatusCode}");
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"    {errorMsg}");
                Console.ResetColor();
                return (false, errorMsg ?? "Unknown");
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"✗ {(int)response.StatusCode}: {responseBody}");
                Console.ResetColor();
                return (false, responseBody);
            }
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ {ex.GetType().Name}: {ex.Message}");
        Console.ResetColor();
        return (false, ex.Message);
    }
}
