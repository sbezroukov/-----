using Microsoft.EntityFrameworkCore;

namespace HomeCenter.Data;

/// <summary>
/// Применяет схемные изменения при обновлении модели (для проектов без dotnet-ef).
/// </summary>
public static class DatabaseMigrator
{
    public static void EnsureVersioningSchema(ApplicationDbContext db)
    {
        // Добавляем колонку IsDeleted в Topics, если её нет
        try
        {
            db.Database.ExecuteSqlRaw(
                "ALTER TABLE Topics ADD COLUMN IsDeleted INTEGER NOT NULL DEFAULT 0");
        }
        catch
        {
            // Колонка уже существует
        }

        // Создаём таблицу TestHistory, если её нет
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS TestHistory (
                Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                FileName TEXT NOT NULL,
                FolderPath TEXT,
                Action INTEGER NOT NULL,
                Content TEXT,
                Timestamp TEXT NOT NULL DEFAULT (datetime('now'))
            )");

        // Добавляем колонки для асинхронной обработки AI оценок в Attempts
        try
        {
            db.Database.ExecuteSqlRaw(
                "ALTER TABLE Attempts ADD COLUMN GradingStatus INTEGER NOT NULL DEFAULT 0");
        }
        catch
        {
            // Колонка уже существует
        }

        try
        {
            db.Database.ExecuteSqlRaw(
                "ALTER TABLE Attempts ADD COLUMN LastUpdatedAt TEXT NOT NULL DEFAULT (datetime('now'))");
        }
        catch
        {
            // Колонка уже существует
        }

        try
        {
            db.Database.ExecuteSqlRaw(
                "ALTER TABLE Attempts ADD COLUMN GradingError TEXT");
        }
        catch
        {
            // Колонка уже существует
        }
    }
}
