using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuizApp.Utils;

namespace QuizApp.Models;

/// <summary>
/// Узел дерева категорий и тестов для отображения списка тем с учётом подпапок.
/// </summary>
public class TestTreeNode
{
    /// <summary>Имя категории (папки) или пусто для корня.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Полный путь папки относительно tests (пустая строка для корня).</summary>
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>Подкатегории (вложенные папки).</summary>
    public List<TestTreeNode> Children { get; set; } = new();

    /// <summary>Тесты, лежащие непосредственно в этой папке (не в подпапках).</summary>
    public List<Topic> Topics { get; set; } = new();

    /// <summary>
    /// Строит дерево из плоского списка тем по путям папок.
    /// </summary>
    public static TestTreeNode BuildTree(IEnumerable<Topic> topics)
    {
        var root = new TestTreeNode { Name = "", FolderPath = "" };

        var pathSet = new HashSet<string> { "" };
        foreach (var t in topics)
        {
            var fileNameNorm = PathHelper.Normalize(t.FileName);
            var dir = Path.GetDirectoryName(fileNameNorm);
            if (string.IsNullOrEmpty(dir)) dir = "";
            pathSet.Add(dir);
            // Добавляем все родительские пути для вложенных папок
            var current = dir;
            while (!string.IsNullOrEmpty(current))
            {
                current = Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(current)) break;
                pathSet.Add(current);
            }
        }

        var pathToNode = new Dictionary<string, TestTreeNode>(pathSet.Count) { [""] = root };
        foreach (var path in pathSet)
        {
            if (path == "") continue;
            var name = Path.GetFileName(path);
            pathToNode[path] = new TestTreeNode { Name = name, FolderPath = path };
        }

        // Связываем узлы: родитель — дети
        foreach (var path in pathSet)
        {
            if (path == "") continue;
            var parentPath = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(parentPath)) parentPath = "";
            var node = pathToNode[path];
            var parent = pathToNode[parentPath];
            parent.Children.Add(node);
        }

        // Сортируем детей по имени
        foreach (var node in pathToNode.Values)
            node.Children = node.Children.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();

        // Раскладываем темы по узлам
        foreach (var t in topics)
        {
            var fileNameNorm = PathHelper.Normalize(t.FileName);
            var dir = Path.GetDirectoryName(fileNameNorm);
            if (string.IsNullOrEmpty(dir)) dir = "";
            pathToNode[dir].Topics.Add(t);
        }

        // Сортируем темы по заголовку
        foreach (var node in pathToNode.Values)
            node.Topics = node.Topics.OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase).ToList();

        return root;
    }
}
