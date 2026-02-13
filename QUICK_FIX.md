# 🚨 Быстрое исправление ошибки "no such column: LastUpdatedAt"

## Проблема

```
SqliteException: SQLite Error 1: 'no such column: a0.LastUpdatedAt'
```

## ⚡ Быстрое решение

### Шаг 1: Остановите приложение

Нажмите `Ctrl+C` в терминале, где запущено приложение, или остановите процесс.

### Шаг 2: Примените миграцию вручную

Выполните команды в PowerShell:

```powershell
# Перейдите в папку проекта
cd C:\HomeRepositories\HomeCenter

# Примените миграцию к базе данных
sqlite3 HomeCenter\quiz.db "ALTER TABLE Attempts ADD COLUMN GradingStatus INTEGER NOT NULL DEFAULT 0;"
sqlite3 HomeCenter\quiz.db "ALTER TABLE Attempts ADD COLUMN LastUpdatedAt TEXT NOT NULL DEFAULT '2024-01-01 00:00:00';"
sqlite3 HomeCenter\quiz.db "UPDATE Attempts SET LastUpdatedAt = COALESCE(CompletedAt, StartedAt) WHERE LastUpdatedAt = '2024-01-01 00:00:00';"
sqlite3 HomeCenter\quiz.db "ALTER TABLE Attempts ADD COLUMN GradingError TEXT;"
```

**Если sqlite3 не установлен**, скачайте с https://www.sqlite.org/download.html или используйте альтернативный способ:

### Альтернатива: Через DB Browser for SQLite

1. Скачайте DB Browser for SQLite: https://sqlitebrowser.org/dl/
2. Откройте файл `HomeCenter\quiz.db`
3. Перейдите на вкладку "Execute SQL"
4. Вставьте и выполните:

```sql
ALTER TABLE Attempts ADD COLUMN GradingStatus INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Attempts ADD COLUMN LastUpdatedAt TEXT NOT NULL DEFAULT '2024-01-01 00:00:00';
UPDATE Attempts SET LastUpdatedAt = COALESCE(CompletedAt, StartedAt) WHERE LastUpdatedAt = '2024-01-01 00:00:00';
ALTER TABLE Attempts ADD COLUMN GradingError TEXT;
```

5. Нажмите "Write Changes" (💾)

### Шаг 3: Перезапустите приложение

```powershell
cd HomeCenter
dotnet run
```

## ✅ Проверка

Откройте http://localhost:8080/Test

Если страница загружается без ошибок — всё работает! 🎉

## 📝 Примечание

Эта ошибка возникла потому что:
1. Вы обновили код с новыми полями в модели `TestAttempt`
2. Приложение было запущено до применения миграции БД
3. При следующем запуске миграция применится автоматически

**В будущем:** Просто перезапускайте приложение после обновления кода, и миграция применится автоматически.

## 🆘 Если не помогло

1. Проверьте, что файл `HomeCenter\quiz.db` не открыт в другой программе
2. Создайте резервную копию и удалите `quiz.db` (данные будут потеряны):
   ```powershell
   cd HomeCenter
   Copy-Item quiz.db quiz.db.backup
   Remove-Item quiz.db
   dotnet run
   ```
3. Смотрите подробную инструкцию в [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)
