# MyAiFeedbackSystem

Локальний MVP для збору відгуків і роботи менеджера з AI-аналізом та чернетками відповідей. За замовчуванням використовується `Stub`: він не звертається до мережі й не потребує платного API. Чернетка **не надсилається** клієнту автоматично.

## Вимоги та перший запуск

- .NET SDK 10 (`dotnet --info`), довіра до локального HTTPS-сертифіката для браузера (`dotnet dev-certs https --trust` за потреби).
- З кореня репозиторію виконайте:

```powershell
dotnet restore MyAiFeedbackSystem.slnx
dotnet tool restore
dotnet ef database update --project src/Infrastructure/MyAiFeedbackSystem.Infrastructure.csproj --startup-project src/Infrastructure/MyAiFeedbackSystem.Infrastructure.csproj --connection "Data Source=../WebApp/reviews.db"
dotnet run --project src/WebApp --launch-profile https
```

Міграція створює `src/WebApp/reviews.db`. Design-time фабрика EF працює з каталогу `src/Infrastructure`, тому шлях у `--connection` починається з `../WebApp/`. WebApp використовує `Data Source=reviews.db` у власному каталозі. Для запуску з іншого робочого каталогу задайте абсолютний шлях у `ConnectionStrings__DefaultConnection` і застосуйте міграцію до нього.

Відкрийте `https://localhost:7084/submit-review`. Панель менеджера: `https://localhost:7084/admin/reviews`.

## Адміністратор

Публічної реєстрації немає. У терміналі запустіть `dotnet run --project src/WebApp -- --hash-admin-password`, введіть пароль завдовжки від 12 символів і збережіть виведений хеш:

```powershell
dotnet user-secrets set Admin:PasswordHash "<виведений хеш>" --project src/WebApp
```

Після запуску ввійдіть як `admin` на `/login`. Хеш можна передати через `Admin__PasswordHash` у середовищі розгортання. Пароль і хеш не комітьте. Докладніше: [ADMIN_SETUP.md](src/WebApp/ADMIN_SETUP.md).

## AI-провайдери

`AI:Provider=Stub` задано в `src/WebApp/appsettings.json`. Для локальних моделей змініть налаштування через User Secrets або змінні середовища, наприклад `AI__Provider`, `AI__BaseUrl`, `AI__Model`, `AI__ResponseFormatMode`.

| Провайдер | `AI:Provider` | Типовий `AI:BaseUrl` | Примітка |
| --- | --- | --- | --- |
| LM Studio | `LMStudio` | `http://localhost:1234/v1/` | Увімкніть локальний сервер і завантажте модель. |
| Ollama | `Ollama` | `http://localhost:11434/v1/` | Перевірте сумісність моделі з вибраним форматом відповіді. |
| OpenAI | `OpenAI` | `https://api.openai.com/v1/` | Потрібен ключ; запити можуть коштувати грошей. |

У всіх HTTP-режимах задайте точну назву моделі в `AI:Model`. `ResponseFormatMode` може бути `StrictJsonSchema`, `JsonObject` або `PromptOnly`; підтримка залежить від endpoint і моделі. `PromptOnly` для аналізу все одно очікує коректний JSON у тексті відповіді. Значення `AI:RequestTimeoutSeconds` має бути 1–300.

Для свідомого підключення OpenAI збережіть ключ окремо від репозиторію:

```powershell
dotnet user-secrets set AI:Provider OpenAI --project src/WebApp
dotnet user-secrets set AI:BaseUrl https://api.openai.com/v1/ --project src/WebApp
dotnet user-secrets set AI:Model "<назва моделі>" --project src/WebApp
dotnet user-secrets set AI:ApiKey "<ключ>" --project src/WebApp
```

Повернення до безкоштовного режиму: `dotnet user-secrets set AI:Provider Stub --project src/WebApp`.

## Перевірки та дані

```powershell
dotnet build MyAiFeedbackSystem.slnx
dotnet test MyAiFeedbackSystem.slnx
```

Публічна форма зберігає чернетку у `sessionStorage` до успішної відправки; стан панелі та редагована AI-чернетка також зберігаються в поточній вкладці. Після закриття вкладки вони можуть зникнути. Приватні відгуки доступні лише після входу; публічна форма доступна без нього. Локальні файли БД, результати збірки й User Secrets виключено з Git.
