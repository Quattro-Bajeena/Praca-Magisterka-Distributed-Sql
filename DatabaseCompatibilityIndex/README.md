# NSCI — New SQL Compatibility Index

## Overview

NSCI is a SQL feature compatibility testing framework designed to measure and compare how well various distributed (and standard) SQL database engines conform to a common set of SQL features. It runs a large battery of structured SQL tests against each configured database, collects pass/fail results, and presents a compatibility score per database.

The project is motivated by the need to understand the practical SQL compatibility surface of distributed SQL databases — systems like TiDB, CockroachDB, Citus, Vitess, and others that claim MySQL or PostgreSQL compatibility but may diverge in edge cases.

---

## Solution Structure

```
DatabaseCompatibilityIndex/
├── NewSqlCompatibility/      # Core CLI test runner (NSCI.csproj)
└── NSCI.Visualize/           # Web dashboard for results (NSCI.Visualize.csproj)
```

---

## Project 1: `NewSqlCompatibility` — Test Runner CLI

A .NET 10 console application that discovers, runs, and reports SQL compatibility tests.

### Configuration (`appsettings.json`)

The tool is driven by a JSON configuration file. It specifies:

- **General settings**: whether to display passing tests, connection string to the stats database.
- **Databases**: a list of database targets. Each entry has:
  - `Name` — human-readable label (e.g., `"CockroachDB localhost"`)
  - `Type` — wire protocol: `MySql` or `PostgreSql`
  - `ConnectionString` — ADO.NET connection string
  - `Enabled` — toggle without removing the entry
  - `Cleanup` — whether to clean up test artifacts after each test

Additionally, a `--db-root` CLI argument can point to a directory tree of `db.config.json` files, allowing per-database configs to be stored alongside the database deployments.

### Supported Databases

| Database | Wire Protocol |
|---|---|
| MySQL | MySql |
| PostgreSQL | PostgreSql |
| TiDB | MySql |
| CockroachDB | PostgreSql |
| Citus | PostgreSql |
| Yugabyte | PostgreSql |
| OceanBase | MySql |
| Neon (cloud) | PostgreSql |
| PolarDB-X | MySql |
| CrateDB | PostgreSql |
| SingleStoreDB | MySql |
| Vitess | MySql |
| ShardingSphere | MySql |

---

### Test Architecture

#### `SqlTest` (abstract base class)

Every test inherits from `SqlTest`. The base class dispatches execution to database-type-specific overrides:

- `SetupMy` / `SetupPg` — DDL and data seeding before the test
- `ExecuteMy` / `ExecutePg` — the SQL under test
- `CleanupMy` / `CleanupPg` — teardown (only runs if `Cleanup = true`)

Alternatively, a test can set string properties `SetupCommandMy`, `SetupCommandPg`, `CommandMy`, `CommandPg` for simple single-statement tests without overriding methods.

The base class also provides assertion helpers (e.g., `AssertEqual`) used to verify query results.

#### `SqlTestAttribute`

A class-level attribute that annotates each test with:
- `Category` (`SqlFeatureCategory` enum)
- `Description` — human-readable description of what is being tested
- `DatabaseTypes` — which wire protocols the test applies to (defaults to both MySQL and PostgreSQL)

PostgreSQL-specific tests use `DatabaseType.PostgreSql`; MySQL-specific tests use `DatabaseType.MySql`.

#### `TestDiscovery`

Uses reflection at startup to find all non-abstract classes that:
- Inherit from `SqlTest`
- Are decorated with `[SqlTest(...)]`

Tests are ordered by category then by class name.

#### `TestRunner`

For each enabled database:
1. Creates a **fresh, isolated test database** with a timestamped name (e.g., `test_2025-07-01T12:00:00`).
2. Iterates through all discovered tests that match the database's wire protocol.
3. For each test: opens two connections to the test database, runs `Setup → Execute → Cleanup`.
4. Catches `AssertionException` (test failure) vs. any other exception (test error).
5. Returns a `TestResult` record with name, category, pass/fail, error message, and duration.

#### `IDatabaseProvider` / `DatabaseProviderFactory`

Abstracts the two wire protocols:

- `MySqlDatabaseProvider` — uses `MySqlConnector`
- `PostgreSqlDatabaseProvider` — uses `Npgsql`

Each provider implements:
- `CreateConnection(connectionString)` — returns a `DbConnection`
- `GenerateCreateDatabaseSql(name)` — DDL to create the isolated test database
- `GenerateSetDatabaseSql(name)` — SQL to switch the session to that database

---

### SQL Feature Categories

Tests are organized into the following categories:

| Category | Examples |
|---|---|
| `BasicQueries` | CREATE/DROP TABLE, INSERT/UPDATE/DELETE, SELECT NOW, TRUNCATE |
| `DataTypes` | INT, BIGINT, VARCHAR, TEXT, DECIMAL, BOOLEAN, DATE, DATETIME, JSON |
| `Constraints` | PRIMARY KEY, FOREIGN KEY, UNIQUE, NOT NULL, CHECK, DEFAULT, AUTO_INCREMENT |
| `Transactions` | COMMIT, ROLLBACK, SAVEPOINT, isolation levels, advisory locks |
| `Joins` | INNER, LEFT, RIGHT, CROSS, SELF join |
| `Aggregations` | COUNT, SUM, AVG, MIN, MAX, GROUP BY, HAVING, DISTINCT |
| `Subqueries` | Scalar, correlated, IN/NOT IN, EXISTS/NOT EXISTS, FROM clause |
| `WindowFunctions` | ROW_NUMBER, RANK, DENSE_RANK, LAG/LEAD, NTILE, window frames |
| `Indexes` | CREATE INDEX, UNIQUE, composite, descending, partial (PG), expression (PG) |
| `Views` | Basic views, updatable views, materialized views (PG), recursive views (PG) |
| `StoredProcedures` / `Triggers` | Stored procedures, triggers, events |
| `CTE` | Common Table Expressions, recursive CTEs |
| `FullTextSearch` | Basic, boolean mode, natural language, multi-column, phrase, ranking |
| `Locking` | SELECT FOR UPDATE, SELECT FOR SHARE, SKIP LOCKED, row-level locking |
| `Upsert` | INSERT ON DUPLICATE KEY UPDATE (MySQL), INSERT ON CONFLICT (PostgreSQL) |
| `Partitioning` | RANGE, LIST partitioning, partition pruning |
| `UserManagement` | CREATE USER/ROLE, GRANT/REVOKE, column privileges |
| `JSONOperations` | JSON creation, extraction, SET, CONTAINS, LENGTH |
| `IsolationLevels` | READ UNCOMMITTED, READ COMMITTED, REPEATABLE READ, SERIALIZABLE |
| `Spatial` | Geometry types, spatial indexes, PostGIS, spatial relationships and measurements |
| `PerformanceHints` | Index hints (USE, FORCE, IGNORE INDEX), STRAIGHT_JOIN, EXPLAIN ANALYZE |
| `DDL` | ALTER TABLE, data type conversions |
| `Misc` | CHECK TABLE, CHECKSUM TABLE, OPTIMIZE TABLE, XML functions, XA transactions |

---

### Reporting

Three output mechanisms are provided after each run:

#### Console Reporter (`ConsoleReporter`)
Real-time color-coded output during the run. Shows category, test name, description, PASS/FAIL, duration, and error messages. A final summary prints total/passed/failed counts and success rate.

#### JSON Report (`JsonReportGenerator`)
Generates a timestamped `report_<timestamp>.json` file. The structure is:
```
JsonReport
└── Reports: { configName → JsonDatabaseReport }
    ├── Summary (Total, Passed, Failed)
    └── ResultsByCategory: { category → JsonReportCategory }
        └── Tests: [ JsonReportTest ]
```
A `master_report.json` is also maintained as a cumulative report updated after each run.

#### Database Reporter (`DatabaseReporter`)
Persists results to a PostgreSQL "stats" database (`NewSqlCompatibilityIndex`) for historical tracking. Schema:
- `databases` — one row per tested database, stores the overall compatibility score (0.0–1.0).
- `test_results` — one row per test per database, with pass/fail, duration, and error text.

Results are upserted so re-running a database updates existing records.

---

## Project 2: `NSCI.Visualize` — Web Dashboard

A .NET 10 ASP.NET Core Razor Pages application that reads from the stats PostgreSQL database and presents the results visually.

### Pages

#### Index (`/`)
- Lists all tested databases with their overall compatibility score and pass/fail counts.
- Shows a cross-database comparison of pass rates per feature category (`ComparisonData`).

#### Database (`/Database?id=<id>`)
- Detailed view for a single database.
- Shows per-category statistics (`CategoryStats`) with pass rate percentages.
- Full list of individual test results with pass/fail status, duration, and error messages.

### Data Layer (`TestDataService`)
Queries the stats database using `Npgsql` and returns strongly-typed model objects (`DatabaseInfo`, `TestResultInfo`, `CategoryStats`, `ComparisonData`).

---

## Data Flow

```
appsettings.json
      │
      ▼
TestConfiguration.Load()
      │
      ▼
TestDiscovery.DiscoverTests()   ←── Reflection over assembly
      │
      ▼
foreach enabled DatabaseConfiguration
      │
      ├── TestRunner.RunAllTests()
      │       ├── CreateTestDatabase()
      │       └── foreach SqlTest
      │               ├── Setup()
      │               ├── Execute()   ←── SQL sent to target DB
      │               └── Cleanup()
      │
      ├── ConsoleReporter  ──► stdout
      ├── DatabaseReporter ──► PostgreSQL stats DB
      └── JsonReportGenerator ──► report_<timestamp>.json
                                          │
                                          ▼
                                  NSCI.Visualize (Razor Pages)
                                  reads stats DB → web dashboard
```

---

## Running the Tool

```bash
# Run with default appsettings.json
dotnet run --project NewSqlCompatibility

# Run with a custom config and database root
dotnet run --project NewSqlCompatibility -- --config path/to/config.json --db-root path/to/db/configs
```

Enable the desired databases in `appsettings.json` by setting `"Enabled": true` before running.

## Running the Dashboard

```bash
dotnet run --project NSCI.Visualize
```

Requires the stats PostgreSQL database to be populated by at least one test run.
