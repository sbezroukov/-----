# Настройка AI для автоматической оценки ответов

HomeCenter поддерживает автоматическую оценку открытых ответов с помощью AI моделей. Доступны несколько провайдеров.

## Быстрый старт с OpenRouter (бесплатно)

OpenRouter предоставляет доступ к нескольким бесплатным моделям без необходимости оплаты.

### 1. Получение API ключа

1. Зарегистрируйтесь на https://openrouter.ai/
2. Перейдите в раздел "Keys": https://openrouter.ai/keys
3. Создайте новый API ключ
4. Скопируйте ключ (начинается с `sk-or-v1-...`)

### 2. Настройка приложения

Создайте файл `HomeCenter/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Admin": {
    "Username": "admin",
    "Password": "ваш_пароль"
  },
  "AI": {
    "Provider": "OpenRouter",
    "ApiKey": "sk-or-v1-ваш-ключ",
    "Enabled": true,
    "Model": "openrouter/free",
    "BaseUrl": "https://openrouter.ai/api/v1"
  }
}
```

### 3. Доступные бесплатные модели

OpenRouter предоставляет несколько бесплатных моделей:

| Модель | Описание | Рекомендуется |
|--------|----------|---------------|
| `openrouter/free` | Универсальная бесплатная модель | ✅ Да |
| `openrouter/aurora-alpha` | Экспериментальная модель | ⚠️ Может быть нестабильна |
| `stepfun/step-3.5-flash:free` | Быстрая модель | ✅ Да |
| `arcee-ai/trinity-large-preview:free` | Большая модель | ✅ Да |
| `upstage/solar-pro-3:free` | Продвинутая модель | ✅ Да |

Для смены модели измените параметр `Model` в конфигурации.

### 4. Проверка работоспособности

Запустите тестовое приложение для проверки доступности моделей:

```bash
cd QwenApiTest
dotnet run
```

Или запустите интеграционные тесты:

```bash
dotnet test --filter "Category=Integration"
```

## Альтернатива: Qwen (Alibaba DashScope)

### 1. Получение API ключа

1. Зарегистрируйтесь на https://dashscope-intl.console.aliyun.com/
2. Создайте API ключ
3. **Важно:** Активируйте модели в консоли (требуется пополнение баланса или активация пробного периода)

### 2. Настройка приложения

```json
{
  "AI": {
    "Provider": "Qwen"
  },
  "Qwen": {
    "ApiKey": "sk-ваш-ключ",
    "Enabled": true,
    "Model": "qwen-turbo",
    "BaseUrl": "https://dashscope-intl.aliyuncs.com/api/v1"
  }
}
```

### 3. Доступные модели

| Модель | Описание | Стоимость |
|--------|----------|-----------|
| `qwen-turbo` | Быстрая модель | Платная |
| `qwen-plus` | Улучшенная модель | Платная |
| `qwen-max` | Максимальная модель | Платная |

**Примечание:** Все модели Qwen требуют активации в консоли и могут быть платными.

## Другие провайдеры (требуют отдельной интеграции)

### Groq (бесплатный tier)

- Регистрация: https://console.groq.com/
- Быстрые модели на базе LPU
- Бесплатный лимит: 14,400 запросов/день

### Together AI

- Регистрация: https://api.together.xyz/
- Широкий выбор open-source моделей
- Бесплатный tier доступен

## Устранение проблем

### OpenRouter возвращает 429 (Too Many Requests)

- Превышен лимит бесплатных запросов
- Подождите некоторое время или пополните баланс на https://openrouter.ai/credits

### OpenRouter возвращает 404 (Model not found)

- Модель больше не доступна бесплатно
- Попробуйте другую модель из списка выше
- Запустите `QwenApiTest` для проверки актуальных моделей

### Qwen возвращает 403 (AccessDenied.Unpurchased)

- Модели не активированы в консоли
- Требуется пополнение баланса
- Перейдите на https://dashscope-intl.console.aliyun.com/ и активируйте модели

### Qwen возвращает 401 (InvalidApiKey)

- API ключ создан для другого региона
- Для международного ключа используйте `https://dashscope-intl.aliyuncs.com/api/v1`
- Для китайского ключа используйте `https://dashscope.aliyuncs.com/api/v1`

## Тестирование конфигурации

Используйте утилиту `QwenApiTest` для проверки доступности моделей:

```bash
cd QwenApiTest
dotnet run
```

Утилита проверит:
- Доступность Qwen моделей
- Доступность OpenRouter моделей
- Список всех бесплатных моделей
- Работоспособность API ключей

## Безопасность

⚠️ **Важно:** Не коммитьте файл `appsettings.Development.json` с реальными API ключами в Git!

Файл уже добавлен в `.gitignore`. Для примера используйте `appsettings.Development.json.example`.

Альтернативно, используйте User Secrets:

```bash
cd HomeCenter
dotnet user-secrets set "AI:ApiKey" "sk-or-v1-ваш-ключ"
dotnet user-secrets set "AI:Enabled" "true"
```

Подробнее: [DEPLOYMENT.md](DEPLOYMENT.md)
