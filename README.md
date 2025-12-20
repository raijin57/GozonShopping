# Gozon Shopping — Orders & Payments (Async, RabbitMQ, Ledger, SignalR)

## Что это
Два микросервиса (Orders, Payments) + API Gateway (YARP) + минимальный фронтенд (Nginx + статичный HTML/JS) + WebSocket уведомления (SignalR). Сообщения между сервисами через RabbitMQ, at-least-once с идемпотентностью и паттернами Transactional Outbox / Inbox. Баланс реализован через ledger + материализованный баланс с optimistic concurrency (effectively exactly-once списание).

## Архитектура
- ApiGateway (`http://localhost:8080`) маршрутизирует REST и WebSocket.
- OrdersService (`http://localhost:8081`) — заказы, outbox → RabbitMQ, SignalR-хаб для статусов.
- PaymentsService (`http://localhost:8082`) — счета, пополнение, списание; inbox/outbox, ledger, идемпотентность по `orderId`.
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
- `Services/OrdersService` — заказы, outbox, consumer, SignalR.
- `Services/PaymentsService` — счета, ledger, inbox/outbox.
- `ApiGateway` — YARP маршрутизация (REST, Swagger, WebSocket).
- `Frontend/` — статика (Nginx) для минимального UI.
- `docker-compose.yml` — весь стек (RabbitMQ, Postgres x2, сервисы, фронт).
- `Shared/Contracts` — DTO сообщений между сервисами.
- `Tests/OrdersService.Tests`, `Tests/PaymentsService.Tests` — unit-тесты (AAA).