# Руководство по логированию AI запросов

## Обзор

Приложение теперь включает подробное логирование всех запросов и ответов к AI провайдерам (OpenRouter и Qwen). Это помогает администраторам диагностировать проблемы с AI оценкой открытых ответов.

## Что логируется

### 1. OpenRouter API

#### Запрос:
```
=== OpenRouter API Request ===
URL: https://openrouter.ai/api/v1/chat/completions
Model: openrouter/free
API Key: sk-or-v1-9...***********
Request Headers:
  - Authorization: Bearer sk-or-v1-9...***********
  - HTTP-Referer: https://github.com/HomeCenter
  - X-Title: HomeCenter Quiz App
Request Body:
{
  "model": "openrouter/free",
  "messages": [
    {
      "role": "system",
      "content": "Ты — эксперт по оценке учебных ответов..."
    },
    {
      "role": "user",
      "content": "Контекст:\n- Предмет/категория: ..."
    }
  ]
}
```

#### Ответ:
```
=== OpenRouter API Response ===
Status Code: 200
Duration: 1234ms
Response Headers:
  - x-ratelimit-limit: 100
  - x-ratelimit-remaining: 99
Response Body:
{
  "choices": [
    {
      "message": {
        "content": "[85, 90, 70]"
      }
    }
  ]
}
✓ OpenRouter API Success: Parsed 3 scores
```

### 2. Qwen API

#### Запрос:
```
=== Qwen API Request ===
URL: https://dashscope-intl.aliyuncs.com/api/v1/services/aigc/text-generation/generation
Model: qwen-turbo
API Key: sk-5133...***********
Request Body:
{
  "model": "qwen-turbo",
  "input": {
    "messages": [...]
  },
  "parameters": {
    "result_format": "message"
  }
}
```

#### Ответ:
```
=== Qwen API Response ===
Status Code: 200
Duration: 2345ms
Response Body:
{
  "output": {
    "choices": [
      {
        "message": {
          "content": "[85, 90, 70]"
        }
      }
    ]
  }
}
✓ Qwen API Success: Parsed 3 scores
```

### 3. Фоновая обработка (BackgroundGradingService)

#### Начало обработки:
```
=== Начало обработки попытки ===
Attempt ID: 123
Topic ID: 456
Topic Title: История России
Started At: 2026-02-14 19:30:00
ResultJson length: 1234 characters
Parsed 5 questions from ResultJson
Question 1: Кто был первым президентом России?...
Question 2: В каком году был принят...
```

#### Вызов AI:
```
Calling AI grading service for 5 items...
=== OpenRouter API Request ===
...
=== OpenRouter API Response ===
...
AI returned 5 scores
Score for question 1: 85%
Score for question 2: 90%
Score for question 3: 70%
Score for question 4: 95%
Score for question 5: 80%
```

#### Успешное завершение:
```
✓ Попытка ID=123 успешно обработана, средний балл: 84%
=== Конец обработки попытки ===
```

### 4. Ошибки

#### Ошибка API:
```
❌ OpenRouter API Error: 401
Response: {
  "error": {
    "message": "Invalid API key",
    "type": "invalid_request_error"
  }
}
```

#### Ошибка обработки:
```
❌ === Ошибка при обработке попытки ===
Attempt ID: 123
Topic ID: 456
Topic Title: История России
Exception Type: System.InvalidOperationException
Exception Message: AI не вернул оценки (возможно, AI отключен или недоступен)
Stack Trace:
   at HomeCenter.Services.BackgroundGradingService.ProcessAttemptAsync(...)
=== Конец обработки ошибки ===
```

## Безопасность

### Маскировка API ключей

API ключи автоматически маскируются в логах:
- **Полный ключ:** `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`
- **В логах:** `sk-or-v1-9...***********`

Показываются только первые 10 символов, остальное заменяется на звездочки.

## Просмотр логов

### Локальный запуск (dotnet run)

Логи выводятся в консоль:

```powershell
cd HomeCenter
dotnet run
```

### Docker

Просмотр логов в реальном времени:

```powershell
docker-compose logs -f homecenter
```

Сохранение логов в файл:

```powershell
docker-compose logs -t --tail=1000 homecenter > ai_logs.txt
```

Просмотр только ошибок:

```powershell
docker-compose logs homecenter | Select-String "ERROR|❌"
```

Просмотр только AI запросов:

```powershell
docker-compose logs homecenter | Select-String "API Request|API Response"
```

## Уровни логирования

### Information (по умолчанию)
- Запросы и ответы API
- Начало/конец обработки попыток
- Успешные результаты

### Warning
- Отсутствие JSON массива в ответе AI
- Несоответствие количества оценок
- Ошибки API (4xx, 5xx)

### Error
- Исключения при вызове API
- Ошибки парсинга ответов
- Ошибки обработки попыток

## Настройка уровня логирования

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "HomeCenter.Services.OpenAnswerGradingService": "Information",
      "HomeCenter.Services.BackgroundGradingService": "Information"
    }
  }
}
```

### Для более детального логирования (Debug):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "HomeCenter.Services.OpenAnswerGradingService": "Debug",
      "HomeCenter.Services.BackgroundGradingService": "Debug"
    }
  }
}
```

### Для минимального логирования (Warning):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "HomeCenter.Services.OpenAnswerGradingService": "Warning",
      "HomeCenter.Services.BackgroundGradingService": "Warning"
    }
  }
}
```

## Типичные проблемы и их диагностика

### Проблема: AI не возвращает оценки

**Логи:**
```
❌ ERROR: AI ApiKey is NOT SET!
```

**Решение:** Установите `AI__ApiKey` в `.env` файле.

---

### Проблема: Ошибка 401 Unauthorized

**Логи:**
```
❌ OpenRouter API Error: 401
Response: {"error": {"message": "Invalid API key"}}
```

**Решение:** Проверьте правильность API ключа в `.env` файле.

---

### Проблема: Ошибка 429 Rate Limit

**Логи:**
```
❌ OpenRouter API Error: 429
Response: {"error": {"message": "Rate limit exceeded"}}
```

**Решение:** Подождите или используйте другой API ключ.

---

### Проблема: AI возвращает неправильный формат

**Логи:**
```
❌ No JSON array found in AI response. Text: "Вот мои оценки: 85, 90, 70"
```

**Решение:** AI модель не вернула JSON массив. Попробуйте другую модель или проверьте промпт.

---

### Проблема: Несоответствие количества оценок

**Логи:**
```
⚠️ Score count mismatch: expected 5, got 3
```

**Решение:** AI вернул меньше оценок, чем вопросов. Проверьте промпт или используйте другую модель.

## Для пользователей

Пользователи видят только краткое сообщение об ошибке в интерфейсе:

```
Ошибка оценки: InvalidOperationException: AI не вернул оценки
```

Полная информация доступна только администратору в логах.

## Рекомендации

1. **Регулярно проверяйте логи** на наличие ошибок
2. **Сохраняйте логи** при возникновении проблем для анализа
3. **Используйте фильтрацию** для поиска конкретных проблем
4. **Настройте уровень логирования** в зависимости от потребностей
5. **Не публикуйте логи** с полными API ключами (хотя они и маскируются)

## Примеры команд для анализа

### Найти все ошибки за последний час:
```powershell
docker-compose logs --since 1h homecenter | Select-String "ERROR|❌"
```

### Найти все запросы к OpenRouter:
```powershell
docker-compose logs homecenter | Select-String "OpenRouter API Request" -Context 0,20
```

### Найти все попытки с ошибками:
```powershell
docker-compose logs homecenter | Select-String "Ошибка при обработке попытки"
```

### Статистика по обработанным попыткам:
```powershell
docker-compose logs homecenter | Select-String "успешно обработана"
```
