using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace HomeCenter.Data;

/// <summary>
/// Применяет схемные изменения при обновлении модели (для проектов без dotnet-ef).
/// </summary>
public static class DatabaseMigrator
{
    public static void EnsureVersioningSchema(ApplicationDbContext db)
    {
        // Добавляем колонку IsDeleted в Topics, если её нет
        AddColumnIfNotExists(db, "Topics", "IsDeleted", "INTEGER NOT NULL DEFAULT 0");

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
        AddColumnIfNotExists(db, "Attempts", "GradingStatus", "INTEGER NOT NULL DEFAULT 0");
        
        if (AddColumnIfNotExists(db, "Attempts", "LastUpdatedAt", "TEXT NOT NULL DEFAULT '2024-01-01 00:00:00'"))
        {
            // Обновляем существующие записи, используя StartedAt/CompletedAt как базу
            db.Database.ExecuteSqlRaw(
                "UPDATE Attempts SET LastUpdatedAt = COALESCE(CompletedAt, StartedAt) WHERE LastUpdatedAt = '2024-01-01 00:00:00'");
        }
        
        AddColumnIfNotExists(db, "Attempts", "GradingError", "TEXT");
    }

    /// <summary>
    /// Добавляет колонку в таблицу, если её ещё нет.
    /// </summary>
    /// <returns>True если колонка была добавлена, False если уже существовала</returns>
    private static bool AddColumnIfNotExists(ApplicationDbContext db, string tableName, string columnName, string columnDefinition)
    {
        // Проверяем существование колонки
        var checkSql = $"SELECT COUNT(*) FROM pragma_table_info('{tableName}') WHERE name = '{columnName}'";
        
        using var connection = db.Database.GetDbConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = checkSql;
        var count = Convert.ToInt32(command.ExecuteScalar());
        
        if (count > 0)
        {
            // Колонка уже существует
            return false;
        }

        // Добавляем колонку
        try
        {
            db.Database.ExecuteSqlRaw($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition}");
            return true;
        }
        catch (SqliteException)
        {
            // Колонка уже существует (race condition)
            return false;
        }
    }
}
