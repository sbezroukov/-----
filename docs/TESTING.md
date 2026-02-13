# Запуск тестов HomeCenter

## Все тесты

```bash
dotnet test
```

## Только юнит-тесты

Интеграционные тесты помечены категорией `Integration`. Чтобы исключить их (например, в CI без API-ключа):

```bash
dotnet test --filter "Category!=Integration"
```

## Интеграционный тест Qwen

Тест `GradeAsync_RealQwenApi_ReturnsScores` отправляет реальный запрос к Qwen API и проверяет, что ответ получен и содержит оценки.

**Требования:** файл `HomeCenter/appsettings.Development.json` с ключом `Qwen:ApiKey`. Скопируйте `appsettings.Development.json.example` в `appsettings.Development.json` и укажите свой API-ключ.

**Запуск:**
```bash
dotnet test --filter "Category=Integration"
```

**Без ключа или без файла:** тест пройдёт без выполнения (ранний return).

## Запуск конкретного теста

```bash
dotnet test --filter "FullyQualifiedName~OpenAnswerGradingService"
```
