# Gozon Shopping — Orders & Payments (Async, RabbitMQ, Ledger, SignalR)

## Что это
Два микросервиса (Gozon.OrdersService, Gozon.PaymentsService) + API Gateway (Gozon.ApiGateway) + минимальный фронтенд (Nginx + статичный HTML/JS) + WebSocket уведомления (SignalR). Сообщения между сервисами через RabbitMQ, at-least-once с идемпотентностью и паттернами Transactional Outbox / Inbox. Баланс реализован через ledger + материализованный баланс с optimistic concurrency (effectively exactly-once списание).

## Архитектура
- Gozon.ApiGateway (`http://localhost:8080`) маршрутизирует REST и WebSocket.
- Gozon.OrdersService (`http://localhost:8081`) — заказы, outbox → RabbitMQ, SignalR-хаб для статусов.
- Gozon.PaymentsService (`http://localhost:8082`) — счета, пополнение, списание; inbox/outbox, ledger, идемпотентность по `orderId`.
- RabbitMQ (5672/15672), Postgres (orders-db:5433, payments-db:5434).
- Frontend (`http://localhost:3000`) — простая страница: создать счет, пополнить, создать заказ, смотреть статусы по пушу.

### Поток создания заказа
1) Orders API: создаёт `Order` и пишет задачу в Outbox (same txn).  
2) OutboxDispatcher шлёт `OrderPaymentRequested` в RabbitMQ.  
3) Payments Inbox сохраняет входящее сообщение и обрабатывает.  
4) Ledger TryDebit: если нет счёта или недостаточно денег — fail, иначе списание с уникальным `orderId` (идемпотентность по уникальному индекс и повторные сообщения не спишут дважды).  
5) Payments Outbox пишет `OrderPaymentStatusChanged` и шлёт в RabbitMQ.  
6) Orders consumer обновляет статус (идемпотентно) и пушит в SignalR-хаб; фронт получает уведомление.

### Паттерны и гарантии
- At-least-once доставка RabbitMQ + идемпотентность бизнес-логики.  
- Transactional Outbox: Orders, Payments.  
- Transactional Inbox: Payments (уникальный `MessageId`, `ProcessedAtUtc`).  
- Exactly-once списание: ledger + уникальный индекс по `OrderId` в `AccountTransactions`.  
- Баланс: `AccountTransactions` (ledger) + материализация `AccountBalance` с rowversion/optimistic concurrency и ретраями.

## Запуск
Требования: Docker / Docker Compose, свободны порты 8080, 8081, 8082, 3000, 5672, 15672, 5433, 5434.

```bash
docker compose up --build
```

Локальная сборка/тесты (по сервисам):
```bash
# Orders
dotnet build src/Gozon.OrdersService/Gozon.OrdersService.sln
dotnet test  src/Gozon.OrdersService/Gozon.OrdersService.sln

# Payments
dotnet build src/Gozon.PaymentsService/Gozon.PaymentsService.sln
dotnet test  src/Gozon.PaymentsService/Gozon.PaymentsService.sln

# Gateway
dotnet build src/Gozon.ApiGateway/Gozon.ApiGateway.sln
```

Основные эндпоинты:
- Gateway: `http://localhost:8080`
- Orders Swagger: `http://localhost:8080/orders/swagger/index.html` (напрямую: `http://localhost:8081/swagger/index.html`)
- Payments Swagger: `http://localhost:8080/payments/swagger/index.html` (напрямую: `http://localhost:8082/swagger/index.html`)
- Frontend: `http://localhost:3000`
- RabbitMQ UI: `http://localhost:15672` (guest/guest)

Тесты:
```bash
dotnet test
```

## Использование (ручной сценарий через фронт)
1. Открыть `http://localhost:3000`.  
2. Ввести `userId` (GUID).  
3. Нажать “Create account”.  
4. “Top up” — пополнить баланс.  
5. “Create order” — создать заказ, автоматически запустится оплата.  
6. “Load orders” — список заказов. Статус меняется в real-time через SignalR.
Если счёт уже был создан для userId, сервер вернёт 409, фронт покажет существующий баланс/ид аккаунта.

## API (кратко)
- Payments:  
  - `POST /api/accounts` — создать счёт (уникален на user).  
  - `POST /api/accounts/{id}/topup` — пополнение.  
  - `GET /api/accounts/{id}/balance` — баланс по id.  
  - `GET /api/accounts/by-user/{userId}/balance` — баланс по user.  
- Orders:  
  - `POST /api/orders` — создать заказ.  
  - `GET /api/orders?userId=...` — список заказов пользователя.  
  - `GET /api/orders/{id}` — статус заказа.  
- WebSocket (SignalR): `/hubs/orders`, метод `JoinOrder(orderId)` для подписки, сервер шлёт `OrderStatusChanged`.

## Где что лежит
- `src/Gozon.OrdersService/` — заказы, outbox, consumer, SignalR. Solution: `src/Gozon.OrdersService/Gozon.OrdersService.sln` (включает OrdersService, Gozon.Shared, Gozon.OrdersService.Tests).
- `src/Gozon.PaymentsService/` — счета, ledger, inbox/outbox. Solution: `src/Gozon.PaymentsService/Gozon.PaymentsService.sln` (включает PaymentsService, Gozon.Shared, Gozon.PaymentsService.Tests).
- `src/Gozon.ApiGateway/` — YARP маршрутизация (REST, Swagger, WebSocket). Solution: `src/Gozon.ApiGateway/Gozon.ApiGateway.sln`.
- `src/Gozon.Shared/` — DTO сообщений между сервисами.
- `Tests/Gozon.OrdersService.Tests/`, `Tests/Gozon.PaymentsService.Tests/` — unit-тесты (AAA, AutoFixture).
- `Frontend/` — статика (Nginx) для минимального UI.
- `docker-compose.yml` — весь стек (RabbitMQ, Postgres x2, сервисы, фронт).
- `Gozon.Shopping.sln` — главное решение со всеми проектами.

> Структура организована по конвенциям: `src/` для исходного кода, `Tests/` для тестов, отдельные solution файлы для каждого сервиса. Проекты названы по схеме `Gozon.ServiceName`.