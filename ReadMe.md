# AspireApp - Example Project
This is an example project for the AspireApp application. It demonstrates the basic structure and functionality of the application, including user authentication, data management, and UI components.


## Getting Started (köra lokalt)

Fungerar på macOS, Linux och Windows. StateStore använder **SQLite som standard**, så ingen databasserver behövs.

### 1. Förutsättningar (en gång)

1. **.NET 10 SDK** (10.0.303 eller senare, se `global.json`)
   ```bash
   # macOS
   brew install --cask dotnet-sdk
   dotnet --list-sdks
   ```
2. **Aspire CLI**. Krävs eftersom AppHost använder `AspireUseCliBundle` (annars fel `ASPIRE009` vid build).
   ```bash
   curl -sSL https://aspire.dev/install.sh | bash
   # alternativt: dotnet tool install -g Aspire.Cli
   ```
3. **Lita på HTTPS-utvecklarcertifikatet**
   ```bash
   dotnet dev-certs https --trust
   ```

### 2. Bygg och starta

```bash
git clone <repo-url>
cd AspireAppExampleApiServices
dotnet build AspireApp1.slnx
aspire run          # eller: dotnet run --project AspireApp1.AppHost
```

1. Terminalen skriver ut en länk till **Aspire Dashboard** (med inloggningstoken). Öppna den.
2. Klicka på endpointen för **webfrontend** för att öppna appen.
3. Gå till `/flowdemo` och starta ett flöde. Följ det i `/flowruns` och `/processflow`.

SQLite-databasen skapas automatiskt i LocalApplicationData, dvs.
`~/.local/share/AspireApp1/statestore.db` på macOS/Linux och `%LOCALAPPDATA%\AspireApp1\statestore.db` på Windows.
Radera filen om du vill börja om med en tom databas.

### Bygga och köra i VS Code

1. Installera tillägget **C# Dev Kit** (VS Code föreslår det automatiskt via `.vscode/extensions.json`).
2. Öppna projektet från terminalen så att VS Code ärver din `PATH` (viktigt om .NET ligger i `~/.dotnet`):
   ```bash
   code .
   ```
3. **Bygg:** `⌘⇧B` (kör tasken `build` = `dotnet build AspireApp1.slnx`).
4. **Kör/debugga:** `F5` och välj **AppHost (Aspire)**. Dashboard-länken visas i *Debug Console*.
5. **Tester:** `⌘⇧P` → *Tasks: Run Test Task*, eller Testing-panelen i C# Dev Kit.

### Felsökning: HTTPS-certifikatet på macOS

Om `dotnet dev-certs https --trust` misslyckas:

- **`There was an error saving the HTTPS developer certificate...`**: lås upp nyckelringen och försök igen:
  ```bash
  security unlock-keychain ~/Library/Keychains/login.keychain-db
  dotnet dev-certs https --clean
  dotnet dev-certs https --trust
  ```
- **`The authorization was denied since no user interaction was possible`**: macOS kräver att du godkänner
  i en ruta på skärmen. Det går **inte** via SSH (t.ex. Termius från iPad), inte ens med `sudo`.
  Kör `dotnet dev-certs https --trust` i **Terminal.app direkt på datorn**, eller öppna
  **Nyckelhanterare → login → Certifikat**, sök `localhost`, dubbelklicka och välj **Lita på → Lita alltid på**.
- Verifiera med `dotnet dev-certs https --check --trust` (ska visa "A trusted certificate was found").
- Kör **inte** `--clean` efteråt, då skapas ett nytt (obetrott) certifikat.

Om du bara kommer åt datorn via SSH kan du köra utan betrott certifikat och tunnla portarna
(Dashboard `15259`, Web Frontend `5004`) med SSH port forwarding:

```bash
export ASPIRE_ALLOW_UNSECURED_TRANSPORT=true
dotnet run --project AspireApp1.AppHost --launch-profile http
```

### 3. Kör testerna

```bash
dotnet run --project AspireApp1.Tests
```

Testprojektet använder Microsoft.Testing.Platform, så `dotnet test` (VSTest-läget) fungerar inte på .NET 10 SDK.

## Arkitekturöversikt

För en samlad helhetsbeskrivning av hur alla tjänster, flöden, spårning och datalagring hänger ihop, se:

- [CLAUDE.md](CLAUDE.md)

## DIGG + W3C Trace Context (spårbarhet)
- Tjänsterna använder W3C Trace Context (`traceparent`, `tracestate`) via .NET `Activity`/OpenTelemetry.
- `trace_id`, `span_id`, `service.name`, `timestamp_utc` och `correlation_id` loggas strukturerat.
- Korrelationskontext skickas från `apiserviceforecast` till `workerservice1` som jobbmeddelande.
- Worker fortsätter samma trace med `traceparent` och propagaterar vidare vid utgående anrop.
- Spans är namngivna per steg (t.ex. `ApiService.CallApiServiceForecast`, `ApiServiceForecast.CallStaticWeather`, `Worker.ProcessJob`, `Worker.CallStaticWeather`) för tydliga Aspire-grafer.
- Felvägar (`/errorcall`, `/errorcall2`) loggar var i kedjan felet uppstår med `trace_id`, `span_id`, `parent_span_id` och `correlation_id`.

### Felsökning via `trace_id`
1. Starta `AspireApp1.AppHost`.
2. Kör anrop från `webfrontend` till backend (exempel: väderflödet).
3. Öppna trace-vyn i Aspire dashboard och följ samma `trace_id` genom tjänstekedjan.
4. På sidan **Processflöde** kan du söka med:
   - ren `trace_id` (32 hex-tecken)
   - full `traceparent` (`00-<trace_id>-<span_id>-<flags>`)
   - Aspire URL-format, t.ex. `https://.../traces/detail/<trace_id>`
5. Processflöde visar stegindikering i formatet **Steg X/N** samt markerar var flödet fastnat med tjänst och felorsak.
6. Kontrollera worker-loggar för samma `trace_id` och `correlation_id` vid async-jobb/retry/finalt fel.

## Frontend-visualisering av processflöde

För krav, specifikation och implementationsplan gällande frontend-visualisering av processflöde via `traceId`/`correlationId`, se:

- [Kravspecifikation: Frontend-visualisering av processflöde](docs/kravspecifikation-frontend-processflode.md)
- [Implementationsplan: Frontend-visualisering av processflöde](docs/implementationsplan-frontend-processflode.md)

## StateStore databas (SQLite eller SQL Server)

StateStore kan köras med både SQLite och SQL Server via konfiguration i `AspireApp1.AppHost/appsettings.Development.json` (globalt för hela lösningen).

```json
{
  "StateStore": {
    "Provider": "Sqlite"
  },
  "ConnectionStrings": {
    "statestoreSqlServer": "Server=.;Database=AspireApp1StateStore;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

- **Default är `Provider = "Sqlite"`** (även om nyckeln saknas eller har okänt värde)
- `Provider = "SqlServer"` använder `ConnectionStrings:statestoreSqlServer`
- `Provider = "Sqlite"` använder delad fil i LocalApplicationData (sätts i AppHost om `ConnectionStrings:statestore` saknas)
- Databasen och tabellerna skapas automatiskt vid uppstart om de saknas (gäller både SQL Server och SQLite)
- AppHost skickar alltid båda nycklarna till tjänsterna:
  - `ConnectionStrings:statestore` = SQLite-anslutning
  - `ConnectionStrings:statestoreSqlServer` = SQL Server-anslutning

### Så växlar du provider
1. Öppna `AspireApp1.AppHost/appsettings.Development.json`.
2. Sätt `StateStore:Provider` till `Sqlite` eller `SqlServer`.
3. Kontrollera att motsvarande connection string är satt.
4. Starta om `AspireApp1.AppHost`.

Du kan också växla utan att ändra filen, via miljövariabel:

```bash
export StateStore__Provider=SqlServer
aspire run
```

> **SQL Server på macOS/Linux:** `Trusted_Connection=True` (Windows-autentisering) fungerar inte där.
> Kör SQL Server i Docker (`mcr.microsoft.com/mssql/server`) och använd en connection string med
> `User Id=sa;Password=...;TrustServerCertificate=True` i `ConnectionStrings:statestoreSqlServer`.

Startsidan i frontend visar nu aktiv provider under rubriken **Aktiv StateStore DB**.

Sidan `/flowruns` visar alla senaste flödeskörningar och länkar vidare till `/processflow`.

## Retry- och Intermittent-demo med konfigurerbar simulering

Formulären på `/retrydemo` och `/intermittentdemo` hämtar default-profiler från WorkerService1 (`GET /flow/simulation/profiles`) och skickar valda värden vid start av flöde.

Default-profilerna sätts i `AspireApp1.WorkerService1/appsettings.json` under `FlowSimulationProfiles`:

```json
{
  "FlowSimulationProfiles": {
    "RetryDemo": {
      "RetryAttempts": 3,
      "RetryDelayMs": 10000
    },
    "IntermittentDemo": {
      "RetryAttempts": 3,
      "NormalMinDelayMs": 10,
      "NormalMaxDelayMs": 500,
      "SlowMinDelayMs": 5000,
      "SlowMaxDelayMs": 30000,
      "SlowCallProbabilityPercent": 20,
      "Http500ProbabilityPercent": 15,
      "RetryDelayMs": 1000
    }
  }
}
```

- **RetryDemo** kör legacy-beteende som default: 3 försök med 10 sekunder mellan försök.
- **IntermittentDemo** kör sannolikhetsstyrd intermittent simulering (slow + HTTP 500) med 1–3 försök.
- `RetryAttempts` klampas till 1–3 innan körning.
