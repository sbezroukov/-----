using QuizApp.Utils;
using Xunit;

namespace QuizApp.Tests;

public class PathHelperTests
{
    [Theory]
    [InlineData("", "")]
    [InlineData("folder1", "folder1")]
    [InlineData("folder1/folder2", "folder1")]
    [InlineData("folder1\\folder2", "folder1")]
    public void Normalize_ConvertsSlashes(string input, string expectedFirstPart)
    {
        var result = PathHelper.Normalize(input);
        Assert.DoesNotContain("\\", result.Replace(Path.DirectorySeparatorChar.ToString(), ""));
        Assert.Contains(expectedFirstPart, result);
    }

    [Fact]
    public void Normalize_Empty_ReturnsEmpty()
    {
        Assert.Equal("", PathHelper.Normalize(""));
    }

    [Theory]
    [InlineData("", "Все", "Все")]
    [InlineData("folder1", "Все", "folder1")]
    public void ToDisplayPath_FormatsCorrectly(string path, string emptyLabel, string expected)
    {
        var result = PathHelper.ToDisplayPath(path, emptyLabel);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToDisplayPath_NestedPath_JoinsWithSlash()
    {
        var path = "folder1" + Path.DirectorySeparatorChar + "folder2";
        var result = PathHelper.ToDisplayPath(path, "Все");
        Assert.Equal("folder1 / folder2", result);
    }
}
