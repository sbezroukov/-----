using Microsoft.Extensions.FileProviders;
using QuizApp.Services;
using Xunit;

namespace QuizApp.Tests;

public class TestImportServiceTests
{
    private static ImportResult Parse(string text)
    {
        var env = new TestWebHostEnvironment();
        var service = new TestImportService(env);
        return service.Parse(text);
    }

    [Fact]
    public void Parse_EmptyText_ReturnsEmpty()
    {
        var result = Parse("");
        Assert.Empty(result.Items);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_ValidBlock_ReturnsItem()
    {
        var text = @"ФАЙЛ: test.txt
Q: Вопрос 1?
*1) Ответ
2) Неверно
";
        var result = Parse(text);
        Assert.Single(result.Items);
        Assert.Equal("test.txt", result.Items[0].Path);
        Assert.Contains("Q:", result.Items[0].Content);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Parse_BlockWithoutFilePrefix_AddsError()
    {
        var text = "Неверный блок\nсодержимое";
        var result = Parse(text);
        Assert.Empty(result.Items);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("ФАЙЛ", result.Errors[0]);
    }

    [Fact]
    public void Parse_EmptyPath_AddsError()
    {
        var text = @"ФАЙЛ:
content
";
        var result = Parse(text);
        Assert.Empty(result.Items);
        Assert.Single(result.Errors);
        Assert.Contains("Пустой путь", result.Errors[0]);
    }

    [Fact]
    public void Parse_InvalidPath_AddsError()
    {
        var text = @"ФАЙЛ: ../../../etc/passwd
content
";
        var result = Parse(text);
        Assert.Empty(result.Items);
        Assert.Single(result.Errors);
        Assert.Contains("Недопустимый", result.Errors[0]);
    }

    [Fact]
    public void Parse_AddsTxtExtension_WhenMissing()
    {
        var text = @"ФАЙЛ: mytest
content
";
        var result = Parse(text);
        Assert.Single(result.Items);
        Assert.Equal("mytest.txt", result.Items[0].Path);
    }

    private class TestWebHostEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string ContentRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "QuizAppTests");
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "QuizApp.Tests";
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "QuizAppTests", "wwwroot");
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
