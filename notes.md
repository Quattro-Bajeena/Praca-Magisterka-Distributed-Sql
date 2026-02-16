https://github.com/secp256k1-sha256/postgres-compatibility-index

Kompatybilność
- https://pgscorecard.com/
- https://www.cockroachlabs.com/docs/stable/postgresql-compatibility
- https://docs.pingcap.com/tidb/stable/mysql-compatibility/ https://docs.pingcap.com/tidb/stable/tidb-limitations/
- https://docs.yugabyte.com/preview/develop/postgresql-compatibility/#unsupported-postgresql-features
- https://docs.voltdb.com/UsingVoltDB/AppxSQL.php


Testowanie SQL:
- https://github.com/elliotchance/sqltest
- https://github.com/secp256k1-sha256/postgres-compatibility-index

# Przeprowadzona praca:

## Zaimplementowane testy
Kategorie:
- Aggregations
- Basic queires
- constraints
- CTE
- Data Types
- dll
- Full text search
- indexes 
- Joins
- Json
- Locking
- Misc
- Partitioning
- Performance hints
- Spatial
- Stored procedures
- subqueries
- transactions
- triggers
- upserts
- user management
- views
- window functions

Ilość:
- Mysql 119
- Postgres 144

## Uruchomione i przetestowane bazy danych
Tradycyjne (baseline):
- MySql	6,6
- Postgres	18
  
Rozproszone:
- CockroachDB	25, 21
- Yugabyte	v2025.2?
- TiDB	8,52
- Oceanbase	4,5
- Neon	
- Oceanbase	3,1
- Citus	13

## Dashboard
Na razie podstawowy dashboard pokazujący statystyki testów.

# Obaszary do dalszych badań

## Rozwinięcie dashboardu

## Dodatkowe testy
Brakujące obszary:
- testy_do_zaimplementowania.md
- Dokładniejsze testy transakcji i wielowątkowości


## Mierzenie czasu testów
- Potrzebne są kontrolowane warunki. Docker z dokładnie określonymi zasobami.
- Wielo node'owość zalecana żeby zauważyć wpływ jaki ma na  
- Normalna baza danych (Postgres/Mysql) jako baseline od którego reszta jest mierzona.
- 

## Analiza błędów
- Przyporządkowania nieudanych testów do kategorii błędów:
  - Składnia jest niezaimplementowana
  - Inny typ zwracanych danych
  - Feature eksperymentalny, który można włączyć flagą czy pluginem
  - Feature działa subtelnie inaczej

## Analiza kodu źródłowego
- Przeszukanie kodu źródłowego w poszukiwaniu miejsc rzucania błędów w stylu "not supported" aby 

## Uruchomienie i odpalenie testów na starszych wersjach baz danych
- Na razie CockroachDB 21 vs 25
- Historyczne dane jak wsparcie się poprawiało
  
## Przetestowanie więcej baz danych
Zastępstw za klasyczne RDBMs:
  - VoltDB
  - Vitess
  - PolarDb-X
Bardziej analityczne bazy danych:
  - ShardingSphere proxy
  - SingleStore
  - Crate

## Podpięcie rozproszonej bazy danych do open sourcowej aplikacji i sprawdzenie działania w praktyce