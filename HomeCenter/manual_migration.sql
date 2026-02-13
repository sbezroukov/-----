-- Ручная миграция для добавления полей асинхронной обработки AI
-- Выполните этот скрипт, если автоматическая миграция не сработала

-- Добавляем колонку GradingStatus
ALTER TABLE Attempts ADD COLUMN GradingStatus INTEGER NOT NULL DEFAULT 0;

-- Добавляем колонку LastUpdatedAt (SQLite не поддерживает функции в DEFAULT)
ALTER TABLE Attempts ADD COLUMN LastUpdatedAt TEXT NOT NULL DEFAULT '2024-01-01 00:00:00';

-- Обновляем существующие записи, используя StartedAt/CompletedAt
UPDATE Attempts SET LastUpdatedAt = COALESCE(CompletedAt, StartedAt) WHERE LastUpdatedAt = '2024-01-01 00:00:00';

-- Добавляем колонку GradingError
ALTER TABLE Attempts ADD COLUMN GradingError TEXT;

-- Проверка: показать структуру таблицы Attempts
-- PRAGMA table_info(Attempts);
