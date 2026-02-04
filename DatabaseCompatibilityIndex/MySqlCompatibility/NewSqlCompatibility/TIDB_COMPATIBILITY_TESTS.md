# TiDB MySQL Compatibility Tests

This document describes the tests created to verify MySQL features that are **unsupported in TiDB** but work correctly in MySQL.

## Tests Created Based on TiDB MySQL Compatibility Article

### 1. **TriggerTest.cs** (Modified)
- **Category**: Triggers
- **Feature**: MySQL Triggers (BEFORE INSERT, AFTER UPDATE, etc.)
- **TiDB Status**: ? Unsupported
- **Test**: Creates a trigger that doubles values before insert and verifies it executes correctly
- **Expected**: ? PASS on MySQL, ? FAIL on TiDB

### 2. **StoredProcedureTest.cs** (Modified)
- **Category**: Stored Procedures
- **Feature**: MySQL Stored Procedures with IN/OUT parameters
- **TiDB Status**: ? Unsupported
- **Test**: Creates a stored procedure that calculates sum and calls it to verify result
- **Expected**: ? PASS on MySQL, ? FAIL on TiDB

### 3. **EventTest.cs** (New)
- **Category**: Events
- **Feature**: MySQL Events (scheduled tasks)
- **TiDB Status**: ? Unsupported
- **Test**: Creates a scheduled event that inserts a row after 1 second and verifies execution
- **Expected**: ? PASS on MySQL, ? FAIL on TiDB

### 4. **SkipLockedTest.cs** (New)
- **Category**: Locking
- **Feature**: `SKIP LOCKED` syntax for row locking
- **TiDB Status**: ? Unsupported ([Issue #18207](https://github.com/pingcap/tidb/issues/18207))
- **Test**: Locks a row and verifies SKIP LOCKED skips it and returns next available row
- **Expected**: ? PASS on MySQL, ? FAIL on TiDB

### 5. **CreateTableAsSelectTest.cs** (New)
- **Category**: DDL
- **Feature**: `CREATE TABLE tblName AS SELECT stmt` syntax
- **TiDB Status**: ? Unsupported ([Issue #4754](https://github.com/pingcap/tidb/issues/4754))
- **Test**: Creates a new table from SELECT results and verifies data
- **Expected**: ? PASS on MySQL, ? FAIL on TiDB

### 6. **CheckTableTest.cs** (New)
- **Category**: Maintenance
- **Feature**: `CHECK TABLE` syntax
- **TiDB Status**: ? Unsupported ([Issue #4673](https://github.com/pingcap/tidb/issues/4673))
- **Test**: Runs CHECK TABLE and verifies it returns status results
- **Expected**: ? PASS on MySQL, ? FAIL on TiDB

### 7. **ChecksumTableTest.cs** (New)
- **Category**: Maintenance
- **Feature**: `CHECKSUM TABLE` syntax
- **TiDB Status**: ? Unsupported ([Issue #1895](https://github.com/pingcap/tidb/issues/1895))
- **Test**: Runs CHECKSUM TABLE and verifies it returns checksum value
- **Expected**: ? PASS on MySQL, ? FAIL on TiDB

### 8. **OptimizeTableTest.cs** (New)
- **Category**: Maintenance
- **Feature**: `OPTIMIZE TABLE` syntax
- **TiDB Status**: ? Unsupported
- **Test**: Runs OPTIMIZE TABLE and verifies it returns results
- **Expected**: ? PASS on MySQL, ? FAIL on TiDB

### 9. **DescendingIndexTest.cs** (New)
- **Category**: Indexes
- **Feature**: Descending Index (INDEX col DESC)
- **TiDB Status**: ? Unsupported ([Issue #2519](https://github.com/pingcap/tidb/issues/2519))
- **Test**: Creates descending index and verifies it can be used in queries
- **Expected**: ? PASS on MySQL, ? FAIL on TiDB

### 10. **SelectIntoVariableTest.cs** (New)
- **Category**: SELECT Syntax
- **Feature**: `SELECT ... INTO @variable` syntax
- **TiDB Status**: ? Unsupported
- **Test**: Selects values into user variables and verifies they can be read
- **Expected**: ? PASS on MySQL, ? FAIL on TiDB

### 11. **UpdatableViewTest.cs** (New)
- **Category**: Views
- **Feature**: Updatable Views (INSERT/UPDATE/DELETE through views)
- **TiDB Status**: ? Not updatable in TiDB
- **Test**: Creates view and performs INSERT, UPDATE, DELETE operations through it
- **Expected**: ? PASS on MySQL, ? FAIL on TiDB

### 12. **XATransactionTest.cs** (New)
- **Category**: Transactions
- **Feature**: XA Transaction syntax (distributed transactions)
- **TiDB Status**: ? Unsupported (uses internal 2PC, not exposed via SQL)
- **Test**: Executes XA START, XA END, XA PREPARE, XA COMMIT sequence
- **Expected**: ? PASS on MySQL, ? FAIL on TiDB

### 13. **XmlFunctionsTest.cs** (New)
- **Category**: Functions
- **Feature**: XML Functions (ExtractValue, UpdateXML)
- **TiDB Status**: ? Unsupported
- **Test**: Uses ExtractValue and UpdateXML functions to manipulate XML data
- **Expected**: ? PASS on MySQL, ? FAIL on TiDB

## Features Not Tested (Noted in Article)

The following features are mentioned in the article but not included in tests:

1. **User-defined functions** - Complex to test, requires C/C++ plugins
2. **FULLTEXT indexes** - Only partially unsupported (supported in some TiDB Cloud regions)
3. **SPATIAL functions** - Would require GIS/geometry data type support
4. **Character sets** (beyond utf8mb4, gbk) - More of a configuration test
5. **Optimizer trace** - Internal optimization feature
6. **X-Protocol** - Protocol-level feature, not SQL
7. **Column-level privileges** - Would require user management setup
8. **`HANDLER` statement** - Low-level table access
9. **`CREATE TABLESPACE`** - Storage engine specific
10. **Lateral derived tables** - Complex query feature
11. **JOIN ON subquery** - Would need specific query patterns

## Summary

? **13 comprehensive tests created** covering major MySQL features unsupported in TiDB
? All tests include proper setup, execution, and cleanup
? Tests verify actual functionality, not just syntax
? Tests will **pass on MySQL 5.7/8.0** and **fail on TiDB**

## Usage

These tests can be used to:
1. Verify MySQL compatibility in testing environments
2. Identify TiDB limitations when migrating from MySQL
3. Validate distributed database feature parity with MySQL
4. Create compatibility reports for database selection decisions
