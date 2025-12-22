# Notification Sender — распределённый сервис уведомлений

Проект выполнен в рамках курса **«Продвинутая разработка микросервисов на C#»**.  
Цель проекта — разработка масштабируемого распределённого сервиса отправки уведомлений через различные каналы (Email, SMS, Push) с использованием микросервисной архитектуры, брокера сообщений и централизованного мониторинга.

---

## Описание проекта

**Notification Sender** — это микросервисная система для отправки уведомлений в реальном времени через разные каналы доставки:

- Email (SMTP)
- SMS (через внешний SMS-провайдер)
- Push-уведомления (через тестовый Push Tester)

Система поддерживает:
- асинхронную обработку уведомлений,
- масштабирование отдельных сервисов,
- повторные попытки отправки (retry),
- хранение статусов доставки,
- централизованное логирование и мониторинг.

---

## Участники команды и вклад

Ежов Никита Олегович
- Создание общего решения и общих библиотек
- Реализация кастомного API Gateway
- Настройка подключения Gateway к RabbitMQ и публикация в очередь notifications
- Реализация Notification Router Service (подписка на notifications, маршрутизация по типам)
- Dockerfile для всех сервисов
- docker-compose инфраструктура
- Централизированное логирование
- Интеграция с sms-провайдером

Косторной Дмитрий Вадимович
- Реализация Email Service (smtp)
- Реализация SMS Service
- Реализация Push Service
- Реализация retry-механизма отправки
- Настройка базы данных и записи статусов
- Тестирование отправки уведомлений

Примечание:
Часть коммитов была выполнена с корпоративной почты, из-за чего в GitHub статистике contributors вклад отображается некорректно.
Фактический объем работы подтверждается историей коммитов

Басырова Алина Радмировна
- Реализация Notification Status Service
- Агрегация статусов уведомлений
- Интеграционные и unit-тесты
- Интеграция Prometheus
- Настройка Grafana
- Swagger-документация
- Подготовка README

## Архитектура системы

Общая архитектура построена по событийной модели с использованием RabbitMQ.

### Основные компоненты:

1. **Notification Gateway**
    - REST API для приёма запросов на отправку уведомлений
    - Публикует сообщения в общую очередь `notifications`

2. **Notification Router Service**
    - Подписывается на очередь `notifications`
    - Определяет тип уведомления
    - Маршрутизирует сообщения в специализированные очереди:
        - `email_queue`
        - `sms_queue`
        - `push_queue`

3. **Сервисы отправки уведомлений**
    - `Email Service`
    - `SMS Service`
    - `Push Service`
    - Каждый сервис:
        - подписывается на свою очередь,
        - выполняет отправку,
        - сохраняет статус доставки в БД,
        - поддерживает retry-механику.

4. **Notification Status Service**
    - REST API для получения статуса уведомления по `notificationId`
    - Агрегирует данные из общей базы

5. **Инфраструктура**
    - RabbitMQ — брокер сообщений
    - PostgreSQL — хранение статусов уведомлений
    - Prometheus — сбор метрик
    - Grafana — визуализация метрик
    - Docker / docker-compose — контейнеризация

---

## Переменные окружения (.env)

Для корректной работы Email и SMS сервисов необходимо в корне проекта создать файл .env вида:
```
# Email Service
SMTP_USERNAME=sender.notification@yandex.ru
SMTP_PASSWORD=*********
SMTP_SENDER_EMAIL=sender.notification@yandex.ru

# Sms Service
SMS_USERNAME=*********
SMS_PASSWORD=*********
```

## Локальный запуск

В корневой директории проекта выполнить:

```
docker-compose up -d
```

После запуска будут доступны:

Gateway API: http://localhost:8080

Status API: http://localhost:8081

Push Tester (Web): http://localhost:8082

RabbitMQ UI: http://localhost:15672

Prometheus: http://localhost:9090

Grafana: http://localhost:3000

login: admin

password: admin

## Проверка работы сервиса (Postman)

Все уведомления отправляются через единый endpoint Gateway API:

```
POST http://localhost:8080/api/notifications
```

### Email уведомление:
```
{
    "type": "email",
    "recipient": "D1.imosa@yandex.ru", 
    "subject": "Test Email from Notification Service",
    "message": "This is a test email message"
}
```

Пример полученного письма:
![img_2.png](img_2.png)

### SMS уведомление:
```
{
    "type": "sms",
    "recipient": "+1234567890", 
    "subject": "Test SMS",
    "message": "This is a test SMS message"
}
```

Пример полученного сообщения:
![img_1.png](img_1.png)

### Push уведомление:
```
{
    "type": "push",
    "recipient": "test-device",
    "subject": "Test Push Notification",
    "message": "This is a test push message",
    "metadata": {
        "platform": "web"
    }
}
```

Push-уведомления доставляются в тестовый сервис Push Tester, который представляет собой веб-страницу, отображающую уведомления в браузере (эмуляция push-уведомлений).

Для проверки можно открыть веб-страницу http://localhost:8082, разрешить уведомления и отправить запрос через postman, после чего придёт уведомление вида:

![img.png](img.png)

## Мониторинг и логирование
- Все сервисы отдают метрики в формате Prometheus (/metrics)
- Prometheus собирает метрики
- Grafana используется для построения дашбордов
- Логирование реализовано через Serilog
- Все попытки отправки (успешные и неуспешные) логируются