# e.l.f. Beauty — Brewery API (C# Interview Assignment)

A .NET 8 Web API that lists open breweries (name, city, phone number) sourced from the
[Open Brewery DB](https://www.openbrewerydb.org/documentation) public API, with search, sorting,
caching, versioning, authentication and logging.

## Solution layout

```
ElfBeauty.BreweryApi.sln
src/
  ElfBeauty.BreweryApi.Core/            Domain models, interfaces (contracts only, no dependencies)
  ElfBeauty.BreweryApi.Infrastructure/  HTTP client, mapper, cache, repositories (in-memory + SQLite), service
  ElfBeauty.BreweryApi.Api/             Controllers, DI wiring, auth, versioning, Swagger, logging
tests/
  ElfBeauty.BreweryApi.Tests/           xUnit + Moq unit tests, plus WebApplicationFactory integration tests
```

## Acceptance criteria mapping

| Requirement | Where |
|---|---|
| REST endpoint for brewery name/city/phone | `GET /api/v1/breweries` in [BreweriesController](src/ElfBeauty.BreweryApi.Api/Controllers/V1/BreweriesController.cs) |
| In-memory storage | [InMemoryBreweryRepository](src/ElfBeauty.BreweryApi.Infrastructure/Repositories/InMemoryBreweryRepository.cs) (default) |
| Classes and interfaces | `Core/Interfaces` (IBreweryApiClient, IBreweryMapper, IBreweryRepository, IAppCache, IBreweryService) with Infrastructure implementations |
| Dependency Injection | [ServiceCollectionExtensions.AddBreweryInfrastructure](src/ElfBeauty.BreweryApi.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs), wired in `Program.cs` |
| Sort by name, distance, city | `sortBy=name|city|distance&sortDirection=asc|desc` in [BreweryService](src/ElfBeauty.BreweryApi.Infrastructure/Services/BreweryService.cs) |
| Search (combinable with sort) | `search=` query parameter (matches name/city/state); can be combined with `sortBy`/`sortDirection` in the same request |
| Map source data to generic model | [BreweryMapper](src/ElfBeauty.BreweryApi.Infrastructure/Mapping/BreweryMapper.cs) → `Brewery` domain model |
| Cache 10 minutes | `IAppCache` + `MemoryAppCache`, `Cache:DurationMinutes` in appsettings |
| SOLID architecture | Core has no outward dependencies (DIP); each class has one responsibility (SRP); storage/mapper/source are swappable (OCP/LSP) |
| Error handling | [ExceptionHandlingMiddleware](src/ElfBeauty.BreweryApi.Api/Middleware/ExceptionHandlingMiddleware.cs) → RFC7807 ProblemDetails with `traceId` |

### Bonus tasks implemented

- **Autocomplete**: `GET /api/v1/breweries/autocomplete?query=`
- **API versioning**: `Asp.Versioning` — routes are `api/v{version}/...` (currently v1.0)
- **Logging**: Serilog (console + rolling file under `logs/`), request logging, structured logs in services
- **Real database**: SQLite via EF Core — toggle with `Storage:Provider` = `InMemory` (default) or `Sqlite`
- **Authentication**: API key scheme — header `X-Api-Key`, validated at startup (fails fast if unconfigured)

## Architecture decisions and known limitations

This section exists so reviewers don't have to guess *why* something was built a certain way.

- **Cache-aside with the repository as a genuine read path, not just a fallback.** On a cache miss,
  `BreweryService` first checks the repository for data that was refreshed within the last
  `Cache:DurationMinutes`. If present, it reuses that data and re-populates the in-process cache —
  no external call is made. Only if the repository has nothing fresh does it call the Open Brewery DB
  API, persist the result (with a timestamp), and cache it. The repository is still read as a
  last-resort fallback if the external API fails. This means the database (when `Storage:Provider`
  is `Sqlite`) is read on every cache miss, not only written to, and the app avoids an unnecessary
  external call immediately after a restart if recent data is already durably stored.
- **Search is never forwarded to the external API.** All (bounded) brewery data is fetched and
  cached/stored once per refresh window, then search/sort/paging are applied entirely in our own
  code against that single dataset. There is intentionally no "search sent upstream, then filtered
  again locally" double-filtering — one filtering path, one source of truth.
- **In-memory sort/filter is bounded, not applied to an unbounded dataset.** `OpenBreweryDb:MaxPages`
  × `PageSize` caps the dataset fetched from the source (default 1,000 records) specifically so that
  in-memory sort/search/paging stays cheap and predictable for this assignment's scope. For a larger
  real-world dataset, sorting/filtering would move into the database query (EF Core `OrderBy`/`Where`)
  instead of in-memory LINQ.
- **`Database.EnsureCreated()` instead of EF Core Migrations.** The SQLite schema here is simple and
  static (no versioned schema changes), so `EnsureCreated()` is used to let a fresh clone run
  immediately without a pre-existing DB file or a separate migration step. A production system with
  evolving schema should use `dotnet ef migrations` + `Database.Migrate()` instead.
- **API key over JWT.** The assignment's security bonus is satisfied with a single static API key
  scheme (constant-time comparison, fails closed if unconfigured) rather than full JWT/login, since
  there's no multi-user/identity requirement in the assignment. The key is never committed to source
  control — see "Running the API" below for local setup via `dotnet user-secrets`.
- **No secrets in the repository.** `appsettings.json` ships with an empty `ApiKeySettings:ApiKey`
  placeholder. The app validates this at startup and refuses to start if it's left empty, rather than
  silently running wide open.

## Running the API

The API key is intentionally **not** committed to the repository. Set it locally once, from the
`src/ElfBeauty.BreweryApi.Api` directory:

```powershell
cd src\ElfBeauty.BreweryApi.Api
dotnet user-secrets init
dotnet user-secrets set "ApiKeySettings:ApiKey" "your-dev-key-here"
dotnet run
```

(Alternative: set the environment variable `ApiKeySettings__ApiKey=your-dev-key-here` instead of
user-secrets — useful for containers/CI. If `ApiKeySettings:ApiKey` is left empty, the app fails to
start with a clear `OptionsValidationException` instead of running without authentication.)

Then open the Swagger UI at the URL printed in the console (e.g. `http://localhost:5000/swagger`),
click **Authorize** and paste the same key you set above.

### Example requests

```
GET /api/v1/breweries?search=dog&sortBy=name&sortDirection=asc
GET /api/v1/breweries?sortBy=distance&latitude=39.7392&longitude=-104.9903
GET /api/v1/breweries/autocomplete?query=do
GET /health   (no API key required)
```

`sortBy` accepts `name`, `city`, `distance` (case-insensitive). `sortDirection` accepts `asc`/`desc`
or the full `Ascending`/`Descending` names. Invalid values return `400 Bad Request` with a message
naming the allowed values; out-of-range `latitude`/`longitude` also return `400`.

### Switching to SQLite storage (bonus)

In `appsettings.json`, set:

```json
"Storage": { "Provider": "Sqlite" }
```

The app will create `brewery.db` automatically on startup (via `EnsureCreated()`, see Architecture
Decisions above) and store the synced brewery data there instead of in-memory, using the same
`IBreweryRepository` contract — no other code changes required. A `RefreshMetadata` table tracks
when the data was last refreshed so the database can be reused as a genuine read source across
app restarts.

## Running tests

```powershell
cd tests\ElfBeauty.BreweryApi.Tests
dotnet test
```

36 tests, all deterministic and offline (no live network calls — the external API client is either
mocked or exercised against a stubbed `HttpMessageHandler`):

- **Unit tests**: mapping (including malformed/missing lat-long), distance calculation, search, sort
  (name/city/distance, including combined search+sort), autocomplete prioritization, cache-miss
  fallback-to-repository resilience, the repository being read as a genuine source on a cache miss
  (not just written to), out-of-range coordinate validation, and the distance-calc mutation-safety
  regression test (cloning, not mutating, cached instances).
- **HTTP client tests**: `OpenBreweryDbApiClient` correctly wraps transport failures and malformed
  JSON into `BreweryDataSourceException`, and correctly maps a valid response.
- **Request validation tests**: `BreweryQueryRequest` accepts `asc`/`desc` shorthand and full enum
  names, rejects invalid `sortBy`/`sortDirection` values, and supports search+sort together.
- **Integration tests** (`WebApplicationFactory<Program>`): the full pipeline end-to-end — missing
  API key → 401, valid key → 200 with data, invalid `sortBy` → 400, `/health` → 200 without a key.

## What is not implemented

Everything listed in the assignment (mandatory + all 5 bonus items) is implemented and tested. There
is no partially-implemented or claimed-but-missing feature in this repository.

