# NSCI - New SQL Compatibility Index

Praca magisterska Mateusza Oleszka. System mierzący i porównujący stopień zgodności różnych rozproszonych systemów bazodanowych ze standardami MySQL i PostgreSQL.

## Technologie

- **C# / .NET 10** — główny język
- **ASP.NET Core Razor Pages** — dashboard webowy (NSCI.Visualize)
- **Entity Framework Core** (Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4) — warstwa danych
- **MySqlConnector** v2.5.0, **Npgsql** v10.0.1 — sterowniki baz
- **Docker Compose** — wdrażanie baz danych
- **PowerShell** — orkiestracja (`orchestration/Orchestrator.ps1`)

## Struktura projektu

```
DatabaseCompatibilityIndex/
├── NewSqlCompatibility/          # Aplikacja CLI (.NET 10)
│   ├── Configuration/            # TestConfiguration, DatabaseConfiguration, DatabaseFolderConfiguration
│   ├── Data/                     # EF Core: NsciDbContext, DatabaseEntity, TestResultEntity
│   ├── Database/                 # IDatabaseProvider, MySqlDatabaseProvider, PostgreSqlDatabaseProvider
│   ├── Reporting/                # ConsoleReporter, JsonReportGenerator, DatabaseReporter (EF Core)
│   ├── Testing/                  # SqlTest (klasa bazowa), SqlTestAttribute, TestRunner, TestDiscovery
│   └── Tests/                    # ~195 testów w 23 kategoriach
│
└── NSCI.Visualize/               # Web dashboard (Razor Pages)
    ├── Pages/                    # Index (lista baz), Database (szczegóły)
    ├── Services/TestDataService  # Odczyt przez IDbContextFactory<NsciDbContext>
    └── Models/TestModels         # DatabaseInfo, TestResultInfo, CategoryStats, ComparisonData

cockroachdb/, citus/, cratedb/, tidb/, ...   # Docker Compose + db.config.json per baza
orchestration/Orchestrator.ps1               # -Start | -Check | -Build | -Run | -Stop
merytoryka/                                  # Notatki i literatura
```

## Architektura testowania

- **`SqlTest`** — abstrakcyjna klasa bazowa; metody `SetupMy/Pg`, `ExecuteMy/Pg`, `CleanupMy/Pg`
- **`[SqlTest(category, description)]`** — atrybut z metadanymi testu
- **`TestDiscovery`** — odkrywa testy przez Reflection
- **`TestRunner`** — tworzy izolowaną bazę (timestamp), uruchamia Setup→Execute→Cleanup
- Dwa protokoły: **MySQL** i **PostgreSQL** — każdy test implementuje odpowiednie warianty

### Przepływ

```
db.config.json (per baza) → TestConfiguration.Load()
  → TestDiscovery (Reflection) → TestRunner
  → ConsoleReporter + JsonReportGenerator + DatabaseReporter
  → PostgreSQL stats DB (NewSqlCompatibilityIndex)
  → NSCI.Visualize odczytuje przez EF Core → web dashboard
```

## Schemat bazy danych (EF Core, tabele tworzone przez EnsureCreated)

**`databases`** — jedna instancja testowanej bazy:
- `id`, `name`, `type` (MySql/PostgreSql), `product`, `version`, `release_year` (nullable), `result` (pass rate 0–1)

**`test_results`** — wynik jednego testu dla jednej bazy:
- `id`, `database_id`, `name`, `class_name`, `category`, `description`, `passed`, `duration`, `error`

## Konfiguracja baz danych

Każdy folder bazy zawiera `db.config.json`:
```json
{
  "name": "CockroachDB",
  "startupType": "docker-compose",
  "databaseType": "PostgreSql",
  "enabled": true,
  "instances": [
    {
      "id": "cockroach-v26.1",
      "displayName": "CockroachDB 26.1",
      "version": "26.1",
      "releaseYear": 2025,
      "enabled": true,
      "connectionString": "...",
      "healthCheck": { "host": "localhost", "port": 26257 }
    }
  ]
}
```

## Testowane bazy danych

| Produkt | Wersje | Protokół |
|---------|--------|----------|
| PostgreSQL | 18 | PostgreSql |
| MySQL | 9.6 | MySql |
| CockroachDB | 22.2, 23.2, 24.3, 25.4, 26.1 | PostgreSql |
| Citus | 10–14 (najnowszy patch per major) | PostgreSql |
| CrateDB | 3–6 (najnowszy patch per major) | PostgreSql |
| TiDB | 8.5 | MySql |
| YugabyteDB | 2.25 | PostgreSql |
| OceanBase | CE latest | MySql |
| SingleStoreDB | latest | MySql |
| Vitess | 16.0 | MySql |
| ShardingSphere | latest | MySql |
| PolarDB-X | 2.4 | MySql |
| Neon | cloud | PostgreSql |

## Zasady pracy z kodem

- **Po każdej zmianie w NSCI.Visualize zawsze uruchom `dotnet build`** żeby upewnić się że kod się kompiluje przed zgłoszeniem gotowości.
  ```bash
  cd DatabaseCompatibilityIndex/NSCI.Visualize && dotnet build
  ```

## Uruchamianie

```bash
# Testy
dotnet run --project DatabaseCompatibilityIndex/NewSqlCompatibility

# Dashboard
dotnet run --project DatabaseCompatibilityIndex/NSCI.Visualize

# Orkiestracja (PowerShell)
./orchestration/Orchestrator.ps1 -Start
./orchestration/Orchestrator.ps1 -Run
./orchestration/Orchestrator.ps1 -Stop
```

## Kategorie testów (23)

Advanced, Aggregations, BasicQueries, CTE, Constraints, CustomTypes, DDL, DataTypes, FullTextSearch, Indexes, IsolationLevels, JSONOperations, Joins, Locking, Misc, Partitioning, Performance, Spatial, Subqueries, Transactions, Upsert, UserManagement, Views, WindowFunctions
