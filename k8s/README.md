# Деплой HomeCenter в Kubernetes

## 1. Создать Secret с паролями и API-ключами

Скопируйте шаблон и заполните реальными значениями:

```bash
cp k8s/secret.yaml.example k8s/secret.yaml
# Отредактируйте secret.yaml (admin-password, qwen-api-key)
```

**Важно:** файл `secret.yaml` в .gitignore — не коммитьте его!

Применить Secret:

```bash
kubectl apply -f k8s/secret.yaml
```

**Альтернатива** — создать Secret через kubectl (значения не сохранятся в файл):

```bash
kubectl create secret generic homecenter-secrets \
  --from-literal=admin-username=admin \
  --from-literal=admin-password='ВАШ_ПАРОЛЬ' \
  --from-literal=qwen-api-key='sk-ВАШ_КЛЮЧ'
```

## 2. Собрать образ и применить манифесты

```bash
# Сборка образа (из папки HomeCenter)
cd HomeCenter
docker build -t homecenter:latest .

# Для Minikube — загрузить образ в кластер
minikube image load homecenter:latest

# Или для Docker Desktop Kubernetes — образ уже доступен
```

## 3. Развернуть приложение

```bash
kubectl apply -f k8s/pvc.yaml
kubectl apply -f k8s/configmap.yaml
kubectl apply -f k8s/deployment.yaml
```

## 4. Проверить

```bash
kubectl get pods
kubectl logs -f deployment/homecenter
kubectl port-forward service/homecenter 8080:80
```

Откройте http://localhost:8080

## Переменные из Secret

| Kubernetes Secret key | Переменная в приложении |
|----------------------|-------------------------|
| admin-username       | Admin__Username         |
| admin-password       | Admin__Password         |
| qwen-api-key         | Qwen__ApiKey            |

## Переменные из ConfigMap

- ASPNETCORE_ENVIRONMENT
- Qwen__Enabled, Qwen__Model, Qwen__BaseUrl
- ConnectionStrings__DefaultConnection
