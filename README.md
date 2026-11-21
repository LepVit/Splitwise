# Splitwise API

REST API для обліку спільних витрат (аналог Splitwise) на **ASP.NET Core (.NET 10)**, **EF Core + PostgreSQL** та **ASP.NET Core Identity**.

Живий Swagger: https://splitwise-0gxd.onrender.com/swagger

## Архітектура

```
src/
  Splitwise.Core            сутності, DTO, винятки, інтерфейси, сервіси (бізнес-логіка), валідатори
  Splitwise.Infrastructure  EF Core DbContext, репозиторії, UnitOfWork, Identity-користувач
  Splitwise.Api             контролери (тонкі), обробка помилок, Identity, Swagger, Program.cs
tests/
  Splitwise.Tests           unit-тести xUnit + Moq
```

- **Контролери** не містять логіки: беруть `currentUserId` з токена, викликають сервіс.
- **Сервіси** (`GroupService`, `ExpenseService`, `BalanceService`, `UserService`) залежать від `IUnitOfWork`, а не від `DbContext`, тому тестуються без бази.
- **Помилки**: ієрархія `DomainException` (`NotFound` → 404, `Conflict` → 409, `Validation` → 400), єдиний `GlobalExceptionHandler` повертає `ProblemDetails`.
- **Валідація**: FluentValidation для кожного вхідного DTO, підключена через `ValidationFilter`.
- **Безпека**: `[Authorize]` на всіх контролерах, перевірка членства в групі в сервісах, секрети поза git, CORS із конфігурації.

### Модель даних

`ApplicationUser` · `Group` · `GroupMember` (складений ключ) · `Expense` (`GroupId` необов'язковий, `IsPayment`) · `ExpenseSplit` (`PaidShare` / `OwedShare`).

- Платник записаний у частках, а не у витраті, тому чек можуть оплатити кілька людей.
- Закриття боргу (Settle Up) — це `Expense` з `IsPayment = true` у тому самому леджері.
- Баланс користувача = Σ(`PaidShare` − `OwedShare`); сума балансів групи завжди дорівнює 0.
- Гроші лише в `decimal(18,2)`; при діленні порівну копійки, що залишилися, роздаються по одній (100 / 3 → 33.34 + 33.33 + 33.33).

## Запуск локально

Потрібні: [.NET 10 SDK](https://dotnet.microsoft.com/download) і Docker (для PostgreSQL).

```bash
docker compose up -d                 # PostgreSQL на localhost:5432
dotnet run --project src/Splitwise.Api
```

Swagger відкриється на `http://localhost:5080/swagger`. Схема бази створюється автоматично під час старту.

Рядок підключення для Development лежить в `appsettings.Development.json` (це лише локальна БД з docker-compose). Для власного рядка:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=...;Database=...;Username=...;Password=..." --project src/Splitwise.Api
```

### Міграції EF Core

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/Splitwise.Infrastructure --startup-project src/Splitwise.Api --output-dir Migrations
```

Після цього закоміть папку `Migrations`: застосунок застосовуватиме її під час старту (`Migrate`). Поки міграцій немає, схема створюється через `EnsureCreated`.

## Авторизація

Використовується Microsoft Identity API endpoints (`/api/auth/...`):

1. `POST /api/auth/register` — `{ "email": "...", "password": "..." }` (пароль: мінімум 6 символів, велика й мала літера, цифра, спецсимвол)
2. `POST /api/auth/login` — повертає `accessToken`
3. У Swagger натисни **Authorize** і встав `accessToken`
4. `PUT /api/users/me` — додати ім'я та прізвище

## Основні ендпоінти (усі, крім `/api/auth/*` і `/health`, потребують токена)

| Метод | Шлях | Опис |
|---|---|---|
| GET / POST | `/api/groups` | мої групи / створити групу |
| GET | `/api/groups/{id}` | група з учасниками (лише для учасників) |
| POST | `/api/groups/{id}/members` | додати учасника за Email |
| POST | `/api/expenses` | витрата з явними частками |
| POST | `/api/expenses/equal` | витрата порівну (копійки без втрат) |
| POST | `/api/expenses/settle` | закрити борг (Settle Up) |
| GET | `/api/expenses/{id}` | витрата |
| GET | `/api/groups/{groupId}/expenses` | витрати групи |
| GET | `/api/groups/{groupId}/balances` | баланси і мінімальний набір переказів |
| GET / PUT | `/api/users/me` | мій профіль |
| GET | `/health` | перевірка стану |

Помилки повертаються як `application/problem+json`: `400` (валідація), `401` (немає токена), `404` (не знайдено), `409` (бізнес-конфлікт, наприклад користувач не в групі).

## Тести

```bash
dotnet test
```

## CI/CD

`.github/workflows/ci.yml`:

- на кожен push і Pull Request: restore → build → test → `docker build`;
- на push у `main`, лише після зелених тестів: виклик deploy hook (секрет `DEPLOY_HOOK_URL`).

## Деплой: Render (API) + Neon (PostgreSQL)

**База (Neon)**
1. Створи проєкт на [neon.tech](https://neon.tech), скопіюй **connection string** (`postgresql://user:password@host/db?sslmode=require`). Застосунок розуміє цей формат.

**API (Render)**
1. Render → New → **Web Service** → підключи репозиторій GitHub, Runtime: **Docker**.
2. У Environment додай змінні:
   - `ConnectionStrings__Default` = рядок із Neon
   - `Cors__AllowedOrigins__0` = адреса фронтенду (за потреби `Cors__AllowedOrigins__1`, ...)
3. Health Check Path: `/health`.
4. У налаштуваннях сервісу скопіюй **Deploy Hook URL**.

**Автодеплой із pipeline**
1. GitHub → Settings → Secrets and variables → Actions → New repository secret: `DEPLOY_HOOK_URL` = Deploy Hook URL із Render.
2. Вимкни в Render автодеплой (Auto-Deploy: Off), щоб деплой ішов лише після зелених тестів.

Секрети (рядок підключення, токени) ніколи не комітяться: локально — `user-secrets`, на сервері — змінні середовища, у pipeline — GitHub Secrets.
