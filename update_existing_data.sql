-- Обновление существующих записей в таблице Attempts
-- Это нужно выполнить ОДИН РАЗ после добавления новых колонок

-- Обновляем LastUpdatedAt для существующих записей
-- Используем CompletedAt если есть, иначе StartedAt
UPDATE Attempts 
SET LastUpdatedAt = COALESCE(CompletedAt, StartedAt) 
WHERE LastUpdatedAt = '2024-01-01 00:00:00' OR LastUpdatedAt IS NULL;

-- Проверяем результат
SELECT 
    Id,
    StartedAt,
    CompletedAt,
    LastUpdatedAt,
    GradingStatus,
    GradingError
FROM Attempts
LIMIT 5;
