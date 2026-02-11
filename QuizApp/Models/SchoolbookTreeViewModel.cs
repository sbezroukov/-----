using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QuizApp.Utils;

namespace QuizApp.Models;

/// <summary>
/// Описание PDF-файла учебника для отображения в дереве.
/// </summary>
public class SchoolbookFile
{
    /// <summary>Отображаемое имя файла (без пути).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Относительный путь к файлу от папки Schoolbook (с системным разделителем).</summary>
    public string RelativePath { get; set; } = string.Empty;
}

/// <summary>
/// Узел дерева папок и учебников (PDF) для раздела "Учебники".
/// </summary>
public class SchoolbookTreeNode
{
    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx", ".epub", ".djvu", ".fb2", ".txt" };

    /// <summary>Имя категории (папки) или пусто для корня.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Полный путь папки относительно Schoolbook (пустая строка для корня).</summary>
    public string FolderPath { get; set; } = string.Empty;

    /// <summary>Подкатегории (вложенные папки).</summary>
    public List<SchoolbookTreeNode> Children { get; set; } = new();

    /// <summary>Файлы PDF, лежащие непосредственно в этой папке (не в подпапках).</summary>
    public List<SchoolbookFile> Files { get; set; } = new();

    /// <summary>
    /// Строит дерево по содержимому папки Schoolbook.
    /// В дерево попадают файлы учебников: pdf, doc, docx, epub, djvu, fb2, txt.
    /// </summary>
    public static SchoolbookTreeNode BuildFromDirectory(string rootFolder)
    {
        var root = new SchoolbookTreeNode { Name = "", FolderPath = "" };

        if (!Directory.Exists(rootFolder))
        {
            return root;
        }

        // Все файлы учебников (включая подпапки)
        var allFiles = Directory.GetFiles(rootFolder, "*.*", SearchOption.AllDirectories)
            .Where(f => AllowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToArray();
        if (allFiles.Length == 0)
        {
            return root;
        }

        // Собираем множество всех папок, в которых есть файлы (и их родителей)
        var pathSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "" };

        foreach (var file in allFiles)
        {
            var relativeFilePath = PathHelper.Normalize(Path.GetRelativePath(rootFolder, file));

            var dir = Path.GetDirectoryName(relativeFilePath);
            if (string.IsNullOrEmpty(dir)) dir = "";
            pathSet.Add(dir);

            var current = dir;
            while (!string.IsNullOrEmpty(current))
            {
                current = Path.GetDirectoryName(current);
                if (string.IsNullOrEmpty(current)) break;
                pathSet.Add(current);
            }
        }

        // Создаём узлы для всех путей
        var pathToNode = new Dictionary<string, SchoolbookTreeNode>(pathSet.Count, StringComparer.OrdinalIgnoreCase)
        {
            [""] = root
        };

        foreach (var path in pathSet)
        {
            if (path == "") continue;
            var name = Path.GetFileName(path);
            pathToNode[path] = new SchoolbookTreeNode
            {
                Name = name,
                FolderPath = path
            };
        }

        // Связываем родителей и детей
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
        {
            node.Children = node.Children
                .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // Добавляем файлы к соответствующим узлам
        foreach (var file in allFiles)
        {
            var relativeFilePath = PathHelper.Normalize(Path.GetRelativePath(rootFolder, file));

            var dir = Path.GetDirectoryName(relativeFilePath);
            if (string.IsNullOrEmpty(dir)) dir = "";

            var name = Path.GetFileName(relativeFilePath);

            var fileModel = new SchoolbookFile
            {
                Name = name,
                RelativePath = relativeFilePath
            };

            pathToNode[dir].Files.Add(fileModel);
        }

        // Сортируем файлы по имени
        foreach (var node in pathToNode.Values)
        {
            node.Files = node.Files
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return root;
    }
}

