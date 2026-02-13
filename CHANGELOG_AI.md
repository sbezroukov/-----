# Changelog: Добавление поддержки OpenRouter и множественных AI провайдеров

## Дата: 13 февраля 2026

## Что добавлено

### 1. Поддержка OpenRouter
- ✅ Интеграция с OpenRouter API (https://openrouter.ai/)
- ✅ Поддержка бесплатных моделей OpenRouter
- ✅ Автоматическое определение доступных моделей
- ✅ Работающая конфигурация с моделью `openrouter/free`

### 2. Множественные AI провайдеры
- ✅ Архитектура поддержки нескольких провайдеров
- ✅ Переключение между Qwen и OpenRouter через конфигурацию
- ✅ Единый интерфейс для всех провайдеров

### 3. Обновленный сервис оценки
Файл: `HomeCenter/Services/OpenAnswerGradingService.cs`

**Изменения:**
- Добавлен метод `GradeWithOpenRouterAsync()` для работы с OpenRouter
- Добавлен метод `GradeWithQwenAsync()` для работы с Qwen
- Добавлен метод `ParseOpenAIStyleScoresFromResponse()` для парсинга ответов OpenRouter
- Рефакторинг метода `ParseQwenScoresFromResponse()` (переименован из `ParseScoresFromResponse`)
- Добавлен общий метод `ExtractScoresFromText()` для извлечения оценок из текста
- Поддержка выбора провайдера через конфигурацию `AI:Provider`

### 4. Обновленная конфигурация
Файл: `HomeCenter/appsettings.json`

**Добавлено:**
```json
"AI": {
  "Provider": "OpenRouter",
  "ApiKey": "",
  "Enabled": false,
  "Model": "openrouter/free",
  "BaseUrl": "https://openrouter.ai/api/v1"
}
```

### 5. Тестовая утилита
Файл: `QwenApiTest/Program.cs`

**Возможности:**
- Проверка доступности Qwen API (международный и китайский регионы)
- Проверка доступности OpenRouter API
- Автоматическое получение списка бесплатных моделей
- Тестирование работоспособности моделей
- Вывод рекомендуемой конфигурации

### 6. Документация

**Новые файлы:**
- `QUICKSTART_AI.md` - быстрый старт за 5 минут
- `docs/AI_SETUP.md` - подробная настройка AI провайдеров
- `OPENROUTER_MODELS.md` - список доступных бесплатных моделей
- `CHANGELOG_AI.md` - этот файл

**Обновленные файлы:**
- `README.md` - добавлена информация об AI и ссылки на документацию
- `appsettings.Development.json.example` - добавлена конфигурация OpenRouter

### 7. Рабочая конфигурация
Файл: `HomeCenter/appsettings.Development.json` (создан, в .gitignore)

**Содержит:**
- Рабочий API ключ OpenRouter
- Настроенный провайдер OpenRouter
- Модель `openrouter/free`
- Тестовые учетные данные администратора

## Результаты тестирования

### Проверка API ключей
```
✅ OpenRouter API: 5 моделей работают
   • openrouter/free
   • openrouter/aurora-alpha
   • stepfun/step-3.5-flash:free
   • arcee-ai/trinity-large-preview:free
   • upstage/solar-pro-3:free

❌ Qwen API: модели не активированы (AccessDenied.Unpurchased)
```

### Юнит-тесты
```
✅ Все 23 юнит-теста прошли успешно
```

### Интеграционные тесты
```
✅ Интеграционный тест с OpenRouter прошел успешно (5 секунд)
```

### Запуск приложения
```
✅ Приложение запускается без ошибок
✅ Доступно на http://localhost:5230 и https://localhost:7153
```

## Преимущества OpenRouter

1. **Бесплатные модели** - 33+ бесплатных модели без необходимости оплаты
2. **Простая регистрация** - вход через Google/GitHub
3. **Без активации** - модели работают сразу после получения ключа
4. **Надежность** - проверенный сервис с хорошей документацией
5. **Совместимость** - OpenAI-совместимый API

## Миграция с Qwen на OpenRouter

Для переключения с Qwen на OpenRouter:

1. Получите API ключ OpenRouter
2. Обновите `appsettings.Development.json`:
   ```json
   "AI": {
     "Provider": "OpenRouter",
     "ApiKey": "sk-or-v1-ваш-ключ",
     "Enabled": true,
     "Model": "openrouter/free",
     "BaseUrl": "https://openrouter.ai/api/v1"
   }
   ```
3. Перезапустите приложение

## Обратная совместимость

✅ Все существующие тесты работают
✅ Qwen остается доступным как альтернативный провайдер
✅ Старая конфигурация Qwen сохранена в `appsettings.json`

## Следующие шаги (опционально)

- [ ] Добавить поддержку Groq API
- [ ] Добавить поддержку Together AI
- [ ] Добавить кэширование результатов оценки
- [ ] Добавить выбор провайдера через UI
- [ ] Добавить статистику использования API

## Файлы, которые нужно настроить

Перед использованием создайте:
```bash
cp HomeCenter/appsettings.Development.json.example HomeCenter/appsettings.Development.json
```

И замените `ЗАМЕНИТЕ_НА_СВОЙ_OPENROUTER_API_KEY` на ваш ключ.

## Безопасность

⚠️ **Важно:** Файл `appsettings.Development.json` добавлен в `.gitignore` и не будет закоммичен в Git.

Для production используйте:
- User Secrets (для локальной разработки)
- Kubernetes Secrets (для деплоя)
- Переменные окружения

Подробнее: [docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)
