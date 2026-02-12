using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using HomeCenter.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HomeCenter.Data;
using HomeCenter.Models;

namespace HomeCenter.Services;

/// <summary>
/// Чтение и разбор txt-файлов из папки tests.
///
/// Формат файла:
///   Первая непустая строка: MODE: Test | Open | Self
///   Остальное — блоки вопросов, разделенные пустой строкой.
///
///   Пример для теста с вариантами:
///     MODE: Test
///
///     Q: Сколько будет 2+2?
///     1) 3
///     *2) 4
///     3) 5
///
///   Строка с * в начале варианта считается правильным ответом.
///   Несколько вариантов с * — вопрос с множественным выбором (чекбоксы).
///   Для MODE: Open и MODE: Self после "Q:" дополнительных строк не требуется.
/// </summary>
public class TestFileService : ITestFileService
{
    private readonly ApplicationDbContext _db;
    private readonly string _testsFolder;
    private readonly ILogger<TestFileService> _logger;
    private readonly ITestHistoryService _historyService;

    public TestFileService(
        ApplicationDbContext db,
        ILogger<TestFileService> logger,
        IWebHostEnvironment env,
        ITestHistoryService historyService)
    {
        _db = db;
        _logger = logger;
        _historyService = historyService;

        // Папка tests теперь ищется относительно корня приложения (ContentRoot),
        // а не bin/Debug..., чтобы одинаково работать и локально, и в Docker.
        var contentRoot = env.ContentRootPath;
        _testsFolder = Path.Combine(contentRoot, "tests");

        if (!Directory.Exists(_testsFolder))
        {
            Directory.CreateDirectory(_testsFolder);
        }
    }

    public void SyncTopicsFromFiles(bool force = false)
    {
        if (!Directory.Exists(_testsFolder))
        {
            return;
        }

        var filesOnDisk = Directory.GetFiles(_testsFolder, "*.txt", SearchOption.AllDirectories)
            .Select(f => NormalizeRelativePath(f))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Рекурсивно обходим папку tests и все подпапки (категории)
        foreach (var file in Directory.GetFiles(_testsFolder, "*.txt", SearchOption.AllDirectories))
        {
            var relativePath = NormalizeRelativePath(file);

            try
            {
                var fileContent = File.ReadAllText(file);
                var (type, _) = ParseFile(file);

                var existing = _db.Topics.SingleOrDefault(t => t.FileName == relativePath);
                if (existing == null)
                {
                    var title = Path.GetFileNameWithoutExtension(relativePath);
                    var topic = new Topic
                    {
                        Title = title,
                        FileName = relativePath,
                        Type = type,
                        IsEnabled = false
                    };
                    _db.Topics.Add(topic);
                    _db.SaveChanges();
                    _historyService.LogAdded(relativePath, fileContent);
                }
                else
                {
                    if (existing.IsDeleted)
                    {
                        existing.IsDeleted = false;
                        _db.SaveChanges();
                        _historyService.LogAdded(relativePath, fileContent); // Восстановление
                    }
                    else
                    {
                        var lastContent = _db.TestHistory
                            .Where(h => h.FileName == relativePath && h.Content != null)
                            .OrderByDescending(h => h.Timestamp)
                            .Select(h => h.Content)
                            .FirstOrDefault();
                        if (lastContent != fileContent)
                        {
                            existing.Title = Path.GetFileNameWithoutExtension(relativePath);
                            existing.Type = type;
                            _db.SaveChanges();
                            _historyService.LogModified(relativePath, fileContent);
                        }
                        else
                        {
                            existing.Title = Path.GetFileNameWithoutExtension(relativePath);
                            existing.Type = type;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка разбора файла теста {FileName}", relativePath);
            }
        }

        _db.SaveChanges();

        // Файлы, которые есть в БД, но отсутствуют на диске — помечаем как удалённые
        var topicsInDb = _db.Topics.Where(t => !t.IsDeleted).ToList();
        foreach (var topic in topicsInDb)
        {
            if (!filesOnDisk.Contains(topic.FileName))
            {
                topic.IsDeleted = true;
                var lastContent = _db.TestHistory
                    .Where(h => h.FileName == topic.FileName && h.Content != null)
                    .OrderByDescending(h => h.Timestamp)
                    .Select(h => h.Content)
                    .FirstOrDefault();
                _historyService.LogFileDeleted(topic.FileName, lastContent);
            }
        }

        _db.SaveChanges();
    }

    private string NormalizeRelativePath(string fullPath)
    {
        var relativePath = Path.GetRelativePath(_testsFolder, fullPath);
        if (Path.DirectorySeparatorChar != '\\')
            relativePath = relativePath.Replace(Path.DirectorySeparatorChar, '\\');
        return relativePath;
    }

    public IReadOnlyList<QuestionModel> LoadQuestionsForTopic(Topic topic)
    {
        // FileName хранит относительный путь (в т.ч. с подпапками)
        var path = Path.Combine(_testsFolder, PathHelper.Normalize(topic.FileName));
        var (_, questions) = ParseFile(path);
        return questions;
    }

    private static (TopicType Type, List<QuestionModel> Questions) ParseFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("Файл теста не найден", path);

        var allLines = File.ReadAllLines(path)
            .Select(l => l.TrimEnd('\r', '\n'))
            .ToList();

        int index = 0;
        TopicType type = TopicType.Test;

        // Найти первую строку MODE:
        while (index < allLines.Count && string.IsNullOrWhiteSpace(allLines[index]))
        {
            index++;
        }

        if (index < allLines.Count && allLines[index].StartsWith("MODE:", StringComparison.OrdinalIgnoreCase))
        {
            var modeValue = allLines[index].Substring("MODE:".Length).Trim();
            type = modeValue.ToLower() switch
            {
                "test" => TopicType.Test,
                "open" => TopicType.Open,
                "self" => TopicType.SelfStudy,
                "selfstudy" => TopicType.SelfStudy,
                _ => TopicType.Test
            };
            index++;
        }

        var questions = new List<QuestionModel>();

        while (index < allLines.Count)
        {
            // Пропускаем пустые строки между блоками
            while (index < allLines.Count && string.IsNullOrWhiteSpace(allLines[index]))
            {
                index++;
            }

            if (index >= allLines.Count)
                break;

            // Для MODE: Open — встретили разделитель "---", выходим из цикла вопросов
            if (type == TopicType.Open && allLines[index].Trim().StartsWith("---", StringComparison.Ordinal))
                break;

            if (!allLines[index].StartsWith("Q:", StringComparison.OrdinalIgnoreCase))
            {
                index++;
                continue;
            }

            var questionText = allLines[index].Substring(2).Trim(':', ' ', '\t');
            index++;

            var question = new QuestionModel
            {
                Text = questionText
            };

            if (type == TopicType.Test)
            {
                // Читаем варианты до пустой строки или следующего Q:
                while (index < allLines.Count && !string.IsNullOrWhiteSpace(allLines[index]) &&
                       !allLines[index].StartsWith("Q:", StringComparison.OrdinalIgnoreCase))
                {
                    var line = allLines[index];
                    bool isCorrect = false;
                    if (line.StartsWith("*"))
                    {
                        isCorrect = true;
                        line = line.Substring(1).TrimStart();
                    }

                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        question.Options.Add(new AnswerOption
                        {
                            Text = line,
                            IsCorrect = isCorrect
                        });
                    }

                    index++;
                }
            }
            else
            {
                // Для Open / SelfStudy дополнительных строк не требуется; просто переходим к следующему вопросу
                while (index < allLines.Count && !string.IsNullOrWhiteSpace(allLines[index]) &&
                       !allLines[index].StartsWith("Q:", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                }
            }

            if (!string.IsNullOrWhiteSpace(question.Text))
            {
                questions.Add(question);
            }
        }

        // Для MODE: Open — парсим секцию "---" и "Ответы" с правильными ответами
        if (type == TopicType.Open && questions.Count > 0)
        {
            while (index < allLines.Count && !allLines[index].Trim().StartsWith("---", StringComparison.Ordinal))
            {
                index++;
            }
            if (index < allLines.Count)
            {
                index++;
                while (index < allLines.Count && string.IsNullOrWhiteSpace(allLines[index]))
                    index++;
                var answersLine = index < allLines.Count ? allLines[index].Trim() : "";
                if (index < allLines.Count && (answersLine.StartsWith("ОТВЕТЫ", StringComparison.Ordinal) || answersLine.StartsWith("Ответы", StringComparison.Ordinal)))
                {
                    index++;
                    var answersByNum = new Dictionary<int, string>();
                    while (index < allLines.Count)
                    {
                        var line = allLines[index].Trim();
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            index++;
                            continue;
                        }
                        var numEnd = 0;
                        while (numEnd < line.Length && (char.IsDigit(line[numEnd]) || line[numEnd] == '.' || line[numEnd] == ')'))
                            numEnd++;
                        if (numEnd > 0 && int.TryParse(line.Substring(0, numEnd).TrimEnd('.', ')', ' ', '\t'), out var num))
                        {
                            var ans = line.Substring(numEnd).TrimStart('.', ')', ' ', '\t', ':');
                            if (!string.IsNullOrEmpty(ans))
                                answersByNum[num] = ans;
                        }
                        index++;
                    }
                    for (var i = 0; i < questions.Count; i++)
                    {
                        if (answersByNum.TryGetValue(i + 1, out var ans))
                            questions[i].CorrectAnswer = ans;
                    }
                }
            }
        }

        return (type, questions);
    }
}

