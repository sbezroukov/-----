# Быстрый просмотр AI логов

## Для Docker

### Просмотр всех логов в реальном времени:
```powershell
cd HomeCenter
docker-compose logs -f homecenter
```

### Сохранить последние 1000 строк логов в файл:
```powershell
docker-compose logs -t --tail=1000 homecenter > ai_logs.txt
```

### Найти все AI запросы:
```powershell
docker-compose logs homecenter | Select-String "API Request|API Response"
```

### Найти только ошибки:
```powershell
docker-compose logs homecenter | Select-String "ERROR|❌"
```

### Найти успешные обработки:
```powershell
docker-compose logs homecenter | Select-String "успешно обработана"
```

## Для локального запуска (dotnet run)

Логи выводятся прямо в консоль. Для сохранения в файл:

```powershell
cd HomeCenter
dotnet run > app_logs.txt 2>&1
```

## Что искать в логах

### ✅ Успешный запрос к OpenRouter:
```
=== OpenRouter API Request ===
URL: https://openrouter.ai/api/v1/chat/completions
Model: openrouter/free
...
=== OpenRouter API Response ===
Status Code: 200
Duration: 1234ms
✓ OpenRouter API Success: Parsed 3 scores
```

### ❌ Ошибка API ключа:
```
❌ OpenRouter API Error: 401
Response: {"error": {"message": "Invalid API key"}}
```

### ❌ AI не вернул оценки:
```
❌ No JSON array found in AI response. Text: "..."
```

### ⚠️ Несоответствие количества оценок:
```
⚠️ Score count mismatch: expected 5, got 3
```

## Полная документация

См. **[AI-LOGGING-GUIDE.md](AI-LOGGING-GUIDE.md)** для подробной информации.
