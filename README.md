# Практическое занятие №3: Настройки запуска и базовая защита веб-службы

## Содержание

- [Структура проекта](#структура-проекта)
- [Инструкция по запуску](#инструкция-по-запуску)
- [Режимы работы](#режимы-работы)
- [Приоритет источников настроек](#приоритет-источников-настроек)
- [Проверки (тесты)](#проверки-тесты)
- [Критичные настройки](#критичные-настройки)
- [Раннее обнаружение ошибок конфигурации](#раннее-обнаружение-ошибок-конфигурации)
- [Архитектура: ядро и модули](#архитектура-ядро-и-модули)

---

## Структура проекта

```
├── src/
│   └── Pr3.ConfigAndSecurity/
│       ├── Config/
│       │   ├── AppSettings.cs                    # Модель конфигурации
│       │   ├── AppSettingsValidator.cs           # Валидация настроек
│       │   └── ConfigurationPriority.cs          # Приоритет источников
│       ├── Domain/
│       │   └── Item.cs                           # Модель данных
│       ├── Middlewares/
│       │   ├── OriginValidationMiddleware.cs     # Проверка доверенных источников
│       │   ├── RateLimitingMiddleware.cs         # Ограничение частоты запросов
│       │   └── SecurityHeadersMiddleware.cs      # Защитные заголовки ответа
│       ├── Services/
│       │   └── ModeContext.cs                    # Контекст режима работы
│       ├── appsettings.json                      # Базовые настройки
│       └── Program.cs                            # Точка входа
├── tests/
│   └── Pr3.ConfigAndSecurity.Tests/
│       ├── ConfigurationPriorityTests.cs         # Проверка приоритета настроек
│       ├── IntegrationSecurityTests.cs           # Интеграционные проверки безопасности
│       └── ProductionModeTests.cs               # Проверка Production-режима
└── Pr3.ConfigAndSecurity.sln
```

---

## Инструкция по запуску

### Требования

- .NET 8 SDK

### Сборка

```bash
dotnet build
```

### Запуск тестов

```bash
dotnet test
```

### Запуск службы (учебный режим по умолчанию)

```bash
dotnet run --project src/Pr3.ConfigAndSecurity/Pr3.ConfigAndSecurity.csproj
```

Служба будет доступна по адресу `http://localhost:5000` (порт из `appsettings.json`).

### Запуск в боевом режиме

```bash
dotnet run --project src/Pr3.ConfigAndSecurity/Pr3.ConfigAndSecurity.csproj -- --AppSettings:Mode=Production
```

Или через переменную окружения:

```powershell
$env:APP_AppSettings__Mode="Production"
dotnet run --project src/Pr3.ConfigAndSecurity/Pr3.ConfigAndSecurity.csproj
```

### Проверка работы

- `GET /health` — проверка состояния и текущего режима
- `GET /mode` — информация о текущем режиме работы
- `GET /items` — получение списка элементов
- `GET /items/{id}` — получение элемента по ID
- `POST /items` — создание нового элемента (тело: `{ "name": "...", "description": "..." }`)

---

## Режимы работы

| Режим | Поведение |
|---|---|
| **Study** (учебный) | Подробные сообщения об ошибках, мягкие ограничения, подробное логирование |
| **Production** (боевой) | Минималистичные сообщения, строгие ограничения, минимум информации об ошибках |

Переключение режима производится **только через настройку**, без изменения кода:

- `appsettings.json` → `"Mode": "Study"` или `"Mode": "Production"`
- Переменная окружения → `APP_AppSettings__Mode=Production`
- Аргумент командной строки → `--AppSettings:Mode=Production`

---

## Приоритет источников настроек

Приоритет от **низшего** к **высшему**:

1. **Файл настроек** (`appsettings.json`) — базовые значения
2. **Переменные окружения** (`APP_*`) — переопределяют файл
3. **Аргументы командной строки** (`--AppSettings:Key=Value`) — переопределяют всё

### Примеры переопределения

```bash
# Переопределение порта через переменную окружения
$env:APP_AppSettings__Port="8080"
dotnet run

# Переопределение доверенных источников через аргумент
 dotnet run -- --AppSettings:TrustedOrigins="https://frontend.example.com"
```

---

## Проверки (тесты)

### 1. Приоритет источников настроек

- `FileSettings_AreUsed_WhenNoOverride` — без переопределения используются значения из файла
- `EnvironmentVariables_Override_FileSettings` — переменные окружения имеют приоритет над файлом
- `CommandLineArgs_Override_EnvAndFile` — аргументы командной строки имеют наивысший приоритет

### 2. Валидация настроек

- `Validation_Fails_On_InvalidPort` — порт должен быть в диапазоне 1–65535
- `Validation_Fails_On_InvalidOrigin` — каждый доверенный источник должен быть валидным URI
- `Validation_Fails_On_WildcardInOrigin` — wildcard-символы запрещены (опечатка не создаёт дыру)
- `Validation_Fails_On_EmptyOrigins` — должен быть указан хотя бы один доверенный источник

### 3. Безопасность

- `UntrustedOrigin_IsBlocked` — запросы с недоверенного `Origin` блокируются (400 Bad Request)
- `TrustedOrigin_IsAllowed` — запросы с доверенного `Origin` проходят
- `RateLimit_ReadBlocksAfterLimit` — превышение лимита чтения возвращает 429 Too Many Requests
- `RateLimit_CreateBlocksAfterLimit` — лимит создания (более строгий) работает отдельно
- `SecurityHeaders_ArePresent` — ответы содержат защитные заголовки (`X-Content-Type-Options`, `X-Frame-Options`, `Cache-Control`)

### 4. Режимы работы

- `StudyMode_ReturnsDetailedError` — в учебном режиме ошибки содержат подробное описание
- `ProductionMode_ReturnsMinimalError` — в боевом режиме ошибки минималистичны

---

## Критичные настройки

| Настройка | Почему критична |
|---|---|
| **Port** | Неверный порт делает службу недоступной или конфликтует с другими процессами |
| **TrustedOrigins** | Опечатка или wildcard открывает CORS-дыру для сторонних сайтов; проверка формата URI предотвращает это |
| **ReadLimitPerMinute / CreateLimitPerMinute** | Нулевые или отрицательные значения ломают rate limiting; разные лимиты защищают от перегрузки операций записи |
| **Mode** | Определяет, какую информацию об ошибках получит потенциальный злоумышленник |

---

## Раннее обнаружение ошибок конфигурации

Валидация настроек происходит **до сборки приложения и до открытия портов**:

1. `Program.cs` читает конфигурацию из всех источников
2. `AppSettingsValidator.ValidateAndThrow()` проверяет логическую корректность значений
3. При ошибках приложение **немедленно завершается** с кодом `1` и понятным сообщением в консоль
4. Порт не открывается, middleware не инициализируются — служба не запускается в невалидной конфигурации

Это снижает риски:
- **Утечки информации** — невозможно запустить службу с открытыми CORS-настройками
- **DoS** — невозможно запустить с некорректными лимитами
- **Недоступности** — неверный порт обнаруживается до старта, а не после

---

## Архитектура: ядро и модули

### Ядро (неизменяемая часть)

- `Program.cs` — точка входа, последовательность: чтение конфигурации → валидация → сборка pipeline → запуск
- `ConfigurationPriority.cs` — единый порядок приоритета источников
- `AppSettingsValidator.cs` — единый набор правил валидации
- `ModeContext.cs` — единый контекст режима для всего приложения

### Модули (middleware)

- `OriginValidationMiddleware` — проверка CORS-источников
- `RateLimitingMiddleware` — ограничение частоты запросов
- `SecurityHeadersMiddleware` — добавление защитных заголовков

### Почему новый модуль не требует редактирования логики запуска

В `Program.cs` middleware подключаются через `app.UseMiddleware<T>()`. Добавление нового модуля сводится к:

1. Созданию класса `NewMiddleware : IMiddleware` (или с сигнатурой `RequestDelegate`)
2. Одной строке в `Program.cs`: `app.UseMiddleware<NewMiddleware>();`

Валидация настроек, режим работы, приоритет конфигурации и порядок pipeline остаются **неизменными**. Новый модуль может читать свои параметры из той же секции `AppSettings` или из отдельной секции, не затрагивая ядро.

---

## Автор

Работа выполнена в рамках практического занятия №3.
