# Docker Setup Guide

## Конфигурация через .env файл

Все настройки (пароли, API ключи) хранятся в `.env` файле, который работает как для локальной разработки, так и для Docker.

## Быстрый старт

1. Скопируйте файл с переменными окружения:
   ```bash
   cd HomeCenter
   cp .env.example .env
   ```

2. Отредактируйте `.env` и укажите свои значения:
   ```env
   Admin__Username=admin
   Admin__Password=ваш-безопасный-пароль
   AI__ApiKey=ваш-openrouter-ключ
   AI__Enabled=true
   ```

3. Запустите Docker:
   ```bash
   docker-compose up -d --build
   ```

**Важно:** `.env` файл добавлен в `.gitignore` и не будет коммититься в репозиторий!

## Локальная разработка

Тот же `.env` файл работает и для локальной разработки:

```bash
cd HomeCenter
cp .env.example .env
# Отредактируйте .env
dotnet run
```

Приложение автоматически загрузит переменные из `.env` при запуске.

## Проверка конфигурации

После запуска контейнера проверьте логи:

```bash
docker-compose logs homecenter
```

### Успешная конфигурация:

```
=== HomeCenter Configuration ===
Current Directory: /app
Looking for .env file at: /app/.env
✓ .env file found, loading...
✓ .env file loaded successfully
  - Admin__Username from env: SET
  - AI__ApiKey from env: SET
Environment: Production
ContentRootPath: /app

=== Configuration Status ===
Admin Username: admin
Admin Password: SET (length: 9)

AI Provider: OpenRouter
AI Enabled: true
AI Model: openrouter/free
AI ApiKey: SET (length: 67, starts with: sk-or-v1-9...)

Qwen Enabled: false
Qwen ApiKey: NOT SET (Qwen is disabled)

Connection String: Data Source=/app/data/quiz.db

✓ All critical configuration parameters are set correctly
================================
```

### Ошибки конфигурации:

Если критичные параметры не заданы, вы увидите **красные ошибки**:

```
=== Configuration Status ===
Admin Username: admin
❌ ERROR: Admin Password is NOT SET!
   Please set Admin__Password in .env file or environment variables

AI Provider: OpenRouter
AI Enabled: true
AI Model: openrouter/free
❌ ERROR: AI ApiKey is NOT SET!
   AI features will NOT work without API key
   Please set AI__ApiKey in .env file or environment variables

Qwen Enabled: true
❌ ERROR: Qwen is enabled but ApiKey is NOT SET!
   Qwen features will NOT work without API key
   Please set Qwen__ApiKey in .env file or disable Qwen (Qwen__Enabled=false)

⚠️  WARNING: Configuration has errors! Please fix them before using the application.
================================
```

## Устранение проблем

### Проблема: "✗ .env file NOT FOUND"

**Решение:**
1. Файл `.env` существует в папке `HomeCenter/`
2. Пересоберите образ: `docker-compose up -d --build`

### Проблема: "❌ ERROR: Admin Password is NOT SET!"

**Решение:** Добавьте в `.env` файл:
```env
Admin__Password=your-secure-password-here
```

### Проблема: "❌ ERROR: AI ApiKey is NOT SET!"

**Решение:** Добавьте в `.env` файл:
```env
AI__ApiKey=sk-or-v1-your-key-here
```

Получить ключ: https://openrouter.ai/keys

## Почему .env вместо appsettings?

Раньше секреты хранились в `appsettings.Development.json`, который нужно было добавлять в `.gitignore`. Теперь используется `.env` файл:

**Преимущества:**
- Один файл для всех окружений (Development и Production)
- Стандартный подход (как в Node.js, Python, etc.)
- `appsettings.Development.json` теперь можно коммитить (нет секретов)
- Не нужно пересобирать Docker образ при изменении настроек

## Приоритет конфигурации в .NET

.NET загружает конфигурацию в следующем порядке (последующие переопределяют предыдущие):

1. `appsettings.json`
2. `appsettings.Development.json` (только структура, без секретов)
3. Переменные окружения (из `.env` файла)

Переменные окружения имеют наивысший приоритет!

## Бэкап базы данных

Для защиты данных используйте систему автоматического резервного копирования.

См. подробное руководство: **[BACKUP-GUIDE.md](BACKUP-GUIDE.md)**

### Быстрый старт:

```powershell
# Создать бэкап
.\scripts\backup.ps1

# Восстановить из бэкапа
.\scripts\restore.ps1

# Настроить автоматический бэкап (ежедневно)
.\scripts\auto-backup.ps1
```
