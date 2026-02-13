-- Проверка структуры таблицы Attempts
-- Выполните: sqlite3 HomeCenter\quiz.db < verify_migration.sql

.mode column
.headers on

SELECT 
    name as ColumnName,
    type as DataType,
    "notnull" as NotNull,
    dflt_value as DefaultValue
FROM pragma_table_info('Attempts')
WHERE name IN ('GradingStatus', 'LastUpdatedAt', 'GradingError')
ORDER BY name;

-- Если вы видите все три колонки - миграция успешна!
-- GradingStatus - INTEGER, NOT NULL, DEFAULT 0
-- LastUpdatedAt - TEXT, NOT NULL, DEFAULT '2024-01-01 00:00:00'
-- GradingError - TEXT, nullable
