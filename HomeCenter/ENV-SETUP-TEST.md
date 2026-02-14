# Тестирование настройки .env файла

## Проверка локального запуска (без Docker)

1. Убедитесь, что файл `.env` существует:
   ```powershell
   cd HomeCenter
   Test-Path .env
   # Должно вернуть: True
   ```

2. Запустите приложение:
   ```powershell
   dotnet run
   ```

3. Проверьте логи в консоли:
   ```
   === HomeCenter Configuration ===
   Current Directory: C:\HomeRepositories\HomeCenter\HomeCenter
   Looking for .env file at: C:\HomeRepositories\HomeCenter\HomeCenter\.env
   ✓ .env file found, loading...
   ✓ .env file loaded successfully
     - Admin__Username from env: SET
     - AI__ApiKey from env: SET
   Environment: Development
   ```

4. Откройте браузер: http://localhost:5000

## Проверка Docker запуска

1. Остановите старый контейнер:
   ```powershell
   cd HomeCenter
   docker-compose down
   ```

2. Пересоберите образ:
   ```powershell
   docker-compose up -d --build
   ```

3. Проверьте логи:
   ```powershell
   docker-compose logs homecenter
   ```

4. Должны увидеть (успешная конфигурация):
   ```
   === HomeCenter Configuration ===
   Current Directory: /app
   Looking for .env file at: /app/.env
   ✓ .env file found, loading...
   ✓ .env file loaded successfully
     - Admin__Username from env: SET
     - AI__ApiKey from env: SET
   Environment: Production
   
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

5. Если есть ошибки, вы увидите **красные сообщения**:
   ```
   ❌ ERROR: Admin Password is NOT SET!
      Please set Admin__Password in .env file or environment variables
   
   ❌ ERROR: AI ApiKey is NOT SET!
      AI features will NOT work without API key
      Please set AI__ApiKey in .env file or environment variables
   
   ⚠️  WARNING: Configuration has errors! Please fix them before using the application.
   ```

5. Откройте браузер: http://localhost:8080

## Возможные проблемы

### Проблема: "✗ .env file NOT FOUND"

**Причина:** Файл `.env` не существует или находится не в той папке.

**Решение:**
```powershell
cd HomeCenter
cp .env.example .env
# Отредактируйте .env и укажите свои значения
```

### Проблема: "AI ApiKey: NOT SET"

**Причина:** Переменная окружения не загрузилась из `.env` файла.

**Решение:**
1. Проверьте формат в `.env` файле (должен быть `AI__ApiKey=...` с двойными подчеркиваниями)
2. Убедитесь, что нет лишних пробелов: `AI__ApiKey=sk-or-v1-...` (без пробелов вокруг `=`)
3. Для Docker: пересоберите образ `docker-compose up -d --build`

### Проблема: В Docker работает, локально нет

**Причина:** Возможно, используется кэш конфигурации.

**Решение:**
```powershell
# Очистите bin и obj папки
dotnet clean
# Запустите заново
dotnet run
```

## Формат переменных окружения

В `.env` файле используйте **двойные подчеркивания** (`__`) вместо двоеточий:

✅ **Правильно:**
```env
Admin__Username=admin
Admin__Password=admin123
AI__ApiKey=sk-or-v1-...
```

❌ **Неправильно:**
```env
Admin:Username=admin
Admin:Password=admin123
AI:ApiKey=sk-or-v1-...
```

## Валидация конфигурации

Приложение автоматически проверяет критичные параметры при запуске:

### ✅ Обязательные параметры:

1. **Admin__Password** - пароль администратора
   - ❌ Если не задан: приложение покажет ошибку
   - Рекомендация: используйте надежный пароль (минимум 8 символов)

2. **AI__ApiKey** - API ключ для AI провайдера
   - ❌ Если не задан: AI функции не будут работать
   - Получить: https://openrouter.ai/keys

### ⚠️ Условно обязательные:

3. **Qwen__ApiKey** - API ключ для Qwen
   - ❌ Если `Qwen__Enabled=true` но ключ не задан: ошибка
   - ✅ Если `Qwen__Enabled=false`: ключ не обязателен

### Цветовая индикация в логах:

- 🟢 **Зеленый** - все параметры настроены правильно
- 🔴 **Красный** - критичная ошибка, параметр не задан
- 🟡 **Желтый** - предупреждение о наличии ошибок

## Безопасность

- ✅ Файл `.env` добавлен в `.gitignore`
- ✅ Секреты не попадут в Git
- ✅ Используйте `.env.example` как шаблон (без реальных ключей)
- ⚠️ Никогда не коммитьте реальные API ключи и пароли!
