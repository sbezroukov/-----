# HomeCenter

Приложение для тестирования, учебников и самопроверки.

## Структура

- `HomeCenter/` — основной проект (ASP.NET Core)
- `HomeCenter.Tests/` — тесты
- `HomeCenter.sln` — решение
- `k8s/` — манифесты Kubernetes
- `docs/` — документация

## Запуск

```bash
cd HomeCenter
dotnet run
```

Или через Docker:

```bash
cd HomeCenter
docker compose up -d --build
```

Приложение доступно по http://localhost:8080

## Документация

- **[docs/DEPLOYMENT.md](docs/DEPLOYMENT.md)** — настройка секретов, User Secrets, деплой в Kubernetes
