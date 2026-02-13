# Руководство по миграции базы данных

## Ошибка: "no such column: a0.LastUpdatedAt"

Эта ошибка возникает, если приложение было запущено до применения миграции базы данных для новых полей асинхронной обработки AI.

## Решение

### Вариант 1: Перезапуск приложения (рекомендуется)

Просто перезапустите приложение. При старте автоматически выполнится миграция через `DatabaseMigrator.EnsureVersioningSchema()`.

**Для локальной разработки:**
```bash
# Остановите приложение (Ctrl+C)
cd HomeCenter
dotnet run
```

**Для Docker:**
```bash
cd HomeCenter
docker compose down
docker compose up -d --build
```

### Вариант 2: Ручное применение миграции

Если автоматическая миграция не работает, примените SQL скрипт вручную:

**1. Найдите файл базы данных:**
```bash
# Обычно находится в:
HomeCenter/quiz.db
```

**2. Откройте БД в SQLite клиенте:**
```bash
# Используйте sqlite3 или любой GUI клиент
sqlite3 HomeCenter/quiz.db
```

**3. Выполните SQL команды:**
```sql
-- Добавляем новые колонки
ALTER TABLE Attempts ADD COLUMN GradingStatus INTEGER NOT NULL DEFAULT 0;
ALTER TABLE Attempts ADD COLUMN LastUpdatedAt TEXT NOT NULL DEFAULT '2024-01-01 00:00:00';
UPDATE Attempts SET LastUpdatedAt = COALESCE(CompletedAt, StartedAt) WHERE LastUpdatedAt = '2024-01-01 00:00:00';
ALTER TABLE Attempts ADD COLUMN GradingError TEXT;
```

**4. Проверьте структуру таблицы:**
```sql
PRAGMA table_info(Attempts);
```

Вы должны увидеть новые колонки:
- `GradingStatus` (INTEGER)
- `LastUpdatedAt` (TEXT)
- `GradingError` (TEXT)

**5. Перезапустите приложение**

### Вариант 3: Использование готового скрипта

Используйте готовый SQL скрипт:

```bash
# Из корня проекта
sqlite3 HomeCenter/quiz.db < HomeCenter/manual_migration.sql
```

## Проверка успешной миграции

После применения миграции проверьте:

**1. Структура таблицы Attempts:**
```sql
PRAGMA table_info(Attempts);
```

Должны быть колонки:
- Id
- UserId
- TopicId
- StartedAt
- CompletedAt
- TotalQuestions
- CorrectAnswers
- ScorePercent
- ResultJson
- **GradingStatus** ← новая
- **LastUpdatedAt** ← новая
- **GradingError** ← новая

**2. Запустите приложение и откройте страницу тестов:**
```
http://localhost:8080/Test
```

Если ошибка исчезла — миграция успешна! ✅

## Откат миграции (если нужно)

Если нужно откатить изменения:

```sql
-- ВНИМАНИЕ: Это удалит данные из новых колонок!
-- Создайте резервную копию БД перед выполнением!

-- SQLite не поддерживает DROP COLUMN напрямую
-- Нужно пересоздать таблицу:

-- 1. Создайте резервную копию
.backup quiz_backup.db

-- 2. Пересоздайте таблицу без новых колонок
-- (это сложная операция, лучше восстановить из бэкапа)
```

## Предотвращение проблемы в будущем

1. **Всегда перезапускайте приложение** после обновления кода с изменениями в моделях
2. **Создавайте резервные копии БД** перед обновлением:
   ```bash
   cp HomeCenter/quiz.db HomeCenter/quiz.db.backup
   ```
3. **Проверяйте логи** при старте приложения на наличие ошибок миграции

## Дополнительная информация

- Миграции выполняются в `HomeCenter/Data/DatabaseMigrator.cs`
- Миграция запускается в `HomeCenter/Program.cs` при старте приложения
- Используется SQLite, который не поддерживает все возможности ALTER TABLE

## Контакты

Если проблема не решается:
1. Проверьте логи приложения
2. Убедитесь, что файл `quiz.db` не заблокирован другим процессом
3. Попробуйте удалить `quiz.db` и запустить приложение заново (данные будут потеряны!)
