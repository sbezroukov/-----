# Инструкция по развёртыванию HomeCenter

Подробное описание настройки, хранения секретов и деплоя в Kubernetes.

---

## 1. Хранение секретов (пароли, API-ключи)

Секреты не должны попадать в репозиторий. Используются следующие способы.

### 1.1. Файлы в .gitignore

Исключены из коммита:
- `appsettings.Development.json` — локальные настройки разработки
- `appsettings.Local.json` — дополнительные локальные настройки
- `k8s/secret.yaml` — секреты Kubernetes

### 1.2. Локальная разработка — User Secrets

**Назначение:** хранение паролей и ключей в профиле пользователя, не в файлах проекта.

**Инициализация (один раз):**
```bash
cd HomeCenter
dotnet user-secrets init
```

**Запись секретов:**
```bash
# Логин и пароль администратора
dotnet user-secrets set "Admin:Username" "admin"
dotnet user-secrets set "Admin:Password" "ваш_пароль"

# Ключ Qwen/DashScope API (для оценки открытых тестов)
dotnet user-secrets set "Qwen:ApiKey" "sk-ваш-ключ"
dotnet user-secrets set "Qwen:Enabled" "true"
```

**Просмотр (без значений):**
```bash
dotnet user-secrets list
```

**Очистка:**
```bash
dotnet user-secrets clear
```

### 1.3. Локальная разработка — appsettings.Development.json

**Назначение:** альтернатива User Secrets; настройки в файле (файл не коммитится).

**Создание:**
```bash
cd HomeCenter
copy appsettings.Development.json.example appsettings.Development.json
# Отредактируйте appsettings.Development.json, подставьте свои значения
```

**Содержимое (пример):**
```json
{
  "Admin": { "Username": "admin", "Password": "ваш_пароль" },
  "Qwen": { "ApiKey": "sk-ключ", "Enabled": true }
}
```

### 1.4. appsettings.json

**Назначение:** базовая конфигурация в репозитории. Секреты — пустые строки.

Не содержит паролей и ключей. Значения подставляются из User Secrets, `appsettings.Development.json` или переменных окружения.

---

## 2. Переменные окружения

В .NET используется формат `Секция__Ключ` (двойное подчёркивание) для вложенных настроек.

| Переменная окружения | Секция в appsettings | Назначение |
|---------------------|----------------------|------------|
| `Admin__Username` | Admin:Username | Логин администратора |
| `Admin__Password` | Admin:Password | Пароль администратора |
| `Qwen__ApiKey` | Qwen:ApiKey | Ключ Qwen/DashScope API |
| `Qwen__Enabled` | Qwen:Enabled | Включить оценку ИИ (true/false) |
| `Qwen__Model` | Qwen:Model | Модель (qwen-turbo, qwen-plus) |
| `Qwen__BaseUrl` | Qwen:BaseUrl | URL API |
| `ConnectionStrings__DefaultConnection` | ConnectionStrings:DefaultConnection | Строка подключения к БД |
| `ASPNETCORE_ENVIRONMENT` | — | Среда (Development, Production) |

---

## 3. Развёртывание в Kubernetes

### 3.1. Структура манифестов

| Файл | Назначение |
|------|------------|
| `k8s/secret.yaml.example` | Шаблон Secret; скопировать в secret.yaml и заполнить |
| `k8s/secret.yaml` | Секреты (в .gitignore) |
| `k8s/configmap.yaml` | Несекретные настройки |
| `k8s/pvc.yaml` | PersistentVolumeClaim для БД |
| `k8s/deployment.yaml` | Deployment и Service |

### 3.2. Создание Secret

**Способ 1 — из файла:**

```bash
# Скопировать шаблон
cp k8s/secret.yaml.example k8s/secret.yaml

# Отредактировать: указать admin-password и qwen-api-key
# Применить в кластер
kubectl apply -f k8s/secret.yaml
```

**Способ 2 — через kubectl (значения не сохраняются в файл):**

```bash
kubectl create secret generic homecenter-secrets \
  --from-literal=admin-username=admin \
  --from-literal=admin-password='ВАШ_ПАРОЛЬ' \
  --from-literal=qwen-api-key='sk-ВАШ_КЛЮЧ'
```

**Сопоставление ключей Secret и переменных:**

| Ключ в Secret | Переменная в приложении |
|---------------|-------------------------|
| admin-username | Admin__Username |
| admin-password | Admin__Password |
| qwen-api-key | Qwen__ApiKey |

### 3.3. Сборка Docker-образа

```bash
cd HomeCenter
docker build -t homecenter:latest .
```

**Minikube** — загрузить образ в кластер:
```bash
minikube image load homecenter:latest
```

**Docker Desktop Kubernetes** — образ доступен автоматически.

### 3.4. Применение манифестов

```bash
# PersistentVolumeClaim для БД
kubectl apply -f k8s/pvc.yaml

# ConfigMap с несекретными настройками
kubectl apply -f k8s/configmap.yaml

# Deployment и Service
kubectl apply -f k8s/deployment.yaml
```

Или одной командой:
```bash
kubectl apply -f k8s/pvc.yaml -f k8s/configmap.yaml -f k8s/deployment.yaml
```

### 3.5. Проверка и доступ

**Статус подов:**
```bash
kubectl get pods -l app=homecenter
```

**Логи:**
```bash
kubectl logs -f deployment/homecenter
```

**Проброс порта (доступ с localhost):**
```bash
kubectl port-forward service/homecenter 8080:80
```

Открыть http://localhost:8080

### 3.6. Обновление после изменений

```bash
# Пересобрать образ
cd HomeCenter
docker build -t homecenter:latest .

# Перезапустить Deployment
kubectl rollout restart deployment/homecenter
```

---

## 4. Локальный запуск (без Kubernetes)

**Обычный запуск:**
```bash
cd HomeCenter
dotnet run
```

**Docker Compose:**
```bash
cd HomeCenter
docker compose up -d --build
```

Для Docker Compose секреты задаются в `environment` в `docker-compose.yml` или через `.env` (файл в .gitignore).

---

## 5. Резюме — что где хранить

| Среда | Секреты хранятся в |
|-------|--------------------|
| Локальная разработка | User Secrets или appsettings.Development.json |
| Docker Compose | environment в docker-compose.yml или .env |
| Kubernetes | Secret (k8s/secret.yaml или kubectl create secret) |
| Production (хостинг) | Переменные окружения провайдера |

Через репозиторий передаются только: `appsettings.json` (без секретов), `appsettings.Development.json.example` (шаблон), `k8s/secret.yaml.example` (шаблон).
