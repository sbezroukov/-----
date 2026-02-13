# QwenApiTest - Утилита для проверки AI моделей

Консольное приложение для проверки доступности и работоспособности AI моделей.

## Возможности

- ✅ Проверка Qwen API (международный и китайский регионы)
- ✅ Проверка OpenRouter API
- ✅ Автоматическое получение списка бесплатных моделей OpenRouter
- ✅ Тестирование работоспособности моделей
- ✅ Вывод рекомендуемой конфигурации для `appsettings.json`

## Использование

```bash
cd QwenApiTest
dotnet run
```

## Что проверяется

### Qwen API
- Международный регион: `https://dashscope-intl.aliyuncs.com/api/v1`
- Китайский регион: `https://dashscope.aliyuncs.com/api/v1`
- Модели: qwen-turbo, qwen-plus, qwen2.5-7b-instruct

### OpenRouter API
- Автоматическое получение списка всех бесплатных моделей
- Тестирование первых 5 моделей из списка
- Проверка работоспособности каждой модели

## Пример вывода

```
=== Проверка доступных AI моделей ===

1. QWEN (Alibaba DashScope)
  Регион: Международный
    qwen-turbo... ✗ 403 (AccessDenied.Unpurchased)
    qwen-plus... ✗ 403 (AccessDenied.Unpurchased)

2. OpenRouter.ai - Получение списка доступных моделей
  Найдено 33 бесплатных моделей

  openrouter/free... ✓ РАБОТАЕТ (ответ: OK)
  stepfun/step-3.5-flash:free... ✓ РАБОТАЕТ

=== ИТОГОВЫЙ ОТЧЕТ ===

✓ Найдено 5 доступных моделей:

OpenRouter:
  • openrouter/free
  • stepfun/step-3.5-flash:free

Рекомендуемая конфигурация для appsettings.json:

  "AI": {
    "Provider": "OpenRouter",
    "ApiKey": "sk-or-v1-...",
    "Enabled": true,
    "Model": "openrouter/free",
    "BaseUrl": "https://openrouter.ai/api/v1"
  }
```

## Изменение API ключей

Отредактируйте файл `Program.cs`:

```csharp
var qwenApiKey = "sk-ваш-qwen-ключ";
var openRouterApiKey = "sk-or-v1-ваш-openrouter-ключ";
```

## Когда использовать

- При первой настройке AI в HomeCenter
- При смене API ключей
- При проблемах с доступностью моделей
- Для проверки актуальных бесплатных моделей OpenRouter
- Для диагностики проблем с API

## Устранение проблем

### Qwen возвращает 403 (AccessDenied.Unpurchased)
- Модели не активированы в консоли Alibaba Cloud
- Требуется пополнение баланса
- Решение: https://dashscope-intl.console.aliyun.com/

### OpenRouter возвращает 429 (Too Many Requests)
- Превышен лимит бесплатных запросов
- Подождите или пополните баланс: https://openrouter.ai/credits

### OpenRouter возвращает 404 (Model not found)
- Модель больше не доступна бесплатно
- Запустите утилиту для получения актуального списка

## Связь с основным приложением

Эта утилита использует те же API ключи и конфигурацию, что и основное приложение HomeCenter.

После проверки скопируйте рекомендуемую конфигурацию в:
- `HomeCenter/appsettings.Development.json` (для разработки)
- Kubernetes Secrets (для production)

## Дополнительная информация

- [Быстрый старт](../QUICKSTART_AI.md)
- [Подробная настройка](../docs/AI_SETUP.md)
- [Список моделей OpenRouter](../OPENROUTER_MODELS.md)
