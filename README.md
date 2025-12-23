# Гоzон - Интернет-магазин

Система микросервисов для интернет-магазина с асинхронным взаимодействием между сервисами.

## Архитектура

- **API Gateway** - маршрутизация запросов с load balancing
- **Payments Service** - управление счетами и оплатами
- **Orders Service** - управление заказами (3 инстанса для масштабирования)
- **Frontend** - веб-интерфейс с WebSocket уведомлениями
- **RabbitMQ** - брокер сообщений для асинхронного взаимодействия
- **PostgreSQL** - база данных
- **Redis** - SignalR Backplane для синхронизации WebSocket уведомлений между инстансами

## Запуск

### Требования
- Docker и Docker Compose
- .NET 8.0 

### Запуск через Docker Compose

```bash
docker-compose up --build
```

Система будет доступна по следующим адресам:
- Frontend: http://localhost:5003
- API Gateway: http://localhost:5000
- Payments Service: http://localhost:5002
- Orders Service (инстанс 1): http://localhost:5001
- Orders Service (инстанс 2): http://localhost:5004
- Orders Service (инстанс 3): http://localhost:5005
- RabbitMQ Management: http://localhost:15672 (guest/guest)
- Redis: localhost:6379

### Использование

1. Откройте http://localhost:5003 в браузере
2. Введите User ID (например: `123e4567-e89b-12d3-a456-426614174000`)
3. Создайте счет
4. Пополните счет
5. Создайте заказ
6. Наблюдайте за изменением статуса заказа через WebSocket уведомления

## API Endpoints

### Payments Service

- `POST /api/payments/accounts` - Создать счет
- `POST /api/payments/accounts/topup` - Пополнить счет
- `GET /api/payments/accounts/{userId}/balance` - Получить баланс

### Orders Service

- `POST /api/orders` - Создать заказ
- `GET /api/orders?userId={userId}` - Получить список заказов
- `GET /api/orders/{orderId}?userId={userId}` - Получить заказ

## Паттерны

- **Transactional Outbox** - в Order Service и Payments Service
- **Transactional Inbox** - в Payments Service
- **Exactly-once семантика** - при списании денег через идемпотентную обработку
- **WebSocket** - для real-time уведомлений о статусе заказов
- **SignalR Backplane (Redis)** - для синхронизации WebSocket уведомлений между несколькими инстансами Orders Service
- **Load Balancing (RoundRobin)** - распределение нагрузки между инстансами через API Gateway

## Принципы проектирования

### SOLID

Проект следует принципам SOLID для обеспечения поддерживаемости и расширяемости кода:

- **S - Single Responsibility Principle (SRP)**
  - Каждый класс имеет одну четкую ответственность
  - `OrderService` - бизнес-логика заказов
  - `OrderRepository` - доступ к данным заказов
  - `RabbitMQService` - работа с очередями сообщений

- **O - Open/Closed Principle (OCP)**
  - Классы открыты для расширения через интерфейсы
  - Можно создать новые реализации (`CachedOrderRepository`, `KafkaMessageBus`) без изменения существующего кода

- **L - Liskov Substitution Principle (LSP)**
  - Все реализации интерфейсов взаимозаменяемы
  - Любая реализация `IOrderRepository` может заменить `OrderRepository`
  - Любая реализация `IMessageBus` может заменить `RabbitMQService`

- **I - Interface Segregation Principle (ISP)**
  - Интерфейсы разделены по назначению
  - `IOrderRepository` - только операции с данными заказов
  - `IMessageBus` - только операции с сообщениями
  - `INotificationService` - только уведомления

- **D - Dependency Inversion Principle (DIP)**
  - Зависимости на абстракциях (интерфейсах), а не на конкретных классах
  - Сервисы зависят от `IOrderRepository`, а не от `OrderRepository`
  - Контроллеры зависят от `IOrderService`, а не от `OrderService`

### GRASP

Проект следует принципам GRASP для правильного распределения ответственности:

- **Information Expert**
  - Классы имеют информацию, необходимую для выполнения операций
  - `OrderRepository` знает, как работать с данными заказов
  - `PaymentRepository` знает, как работать с данными платежей

- **Low Coupling**
  - Минимальные зависимости между классами
  - Связность через интерфейсы, а не через конкретные классы
  - Контроллеры → Интерфейсы сервисов → Интерфейсы репозиториев

- **High Cohesion**
  - Каждый класс имеет четкую, сфокусированную ответственность
  - `OrderService` содержит только логику работы с заказами
  - `PaymentService` содержит только логику работы с платежами

- **Controller**
  - Контроллеры координируют запросы, но не содержат бизнес-логику
  - Вся бизнес-логика находится в сервисах

- **Pure Fabrication**
  - Технические классы выделены отдельно
  - `OrderRepository`, `RabbitMQService`, `NotificationService` - технические классы для инфраструктуры

- **Indirection**
  - Интерфейсы обеспечивают косвенность между слоями
  - Dependency Injection обеспечивает косвенность при создании объектов

- **Protected Variations**
  - Интерфейсы защищают от изменений реализаций
  - Можно заменить RabbitMQ на Kafka без изменения сервисов
  - Можно заменить PostgreSQL на MongoDB без изменения бизнес-логики

## Структура проекта

Проект использует упрощенную многослойную архитектуру:

- **API Layer** (Controllers) - обработка HTTP запросов
- **Application Layer** (Services) - бизнес-логика
- **Data Layer** (Repositories, DbContext) - доступ к данным
- **Infrastructure** (RabbitMQ, SignalR) - внешние интеграции

Интерфейсы обеспечивают инверсию зависимостей и тестируемость кода.

