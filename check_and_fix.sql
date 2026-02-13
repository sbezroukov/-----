-- Проверка структуры таблицы Attempts
PRAGMA table_info(Attempts);

-- Если вы видите колонки GradingStatus, LastUpdatedAt, GradingError - всё готово!
-- Если каких-то колонок нет, выполните соответствующие команды ниже:

-- Добавить GradingStatus (если её нет)
-- ALTER TABLE Attempts ADD COLUMN GradingStatus INTEGER NOT NULL DEFAULT 0;

-- Добавить LastUpdatedAt (если её нет)
-- ALTER TABLE Attempts ADD COLUMN LastUpdatedAt TEXT NOT NULL DEFAULT '2024-01-01 00:00:00';
-- UPDATE Attempts SET LastUpdatedAt = COALESCE(CompletedAt, StartedAt) WHERE LastUpdatedAt = '2024-01-01 00:00:00';

-- Добавить GradingError (если её нет)
-- ALTER TABLE Attempts ADD COLUMN GradingError TEXT;
