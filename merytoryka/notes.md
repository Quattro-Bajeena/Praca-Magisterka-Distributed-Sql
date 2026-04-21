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
- Basic queries
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




- Wsparcie społeczności / łatwość wdrożenia
- Wersja -> Lata ważniejsze. X lat wstecz

Opis co będzie:
10 punktów
co najmniej X baz danych
kategorie, liczba testów

Opis:
- Stworzenie programu w języku C# ułatwiającego przeprowadzanie testów funkcjonalności baz danych
- Napisanie przynajmniej 150 przypadków testowych dla bazy PostgreSQL oraz MySql
- Testy będą obejmowały obszary takie jak: agregacje, ograniczenia, CTE, typy danych, DLL, indeksy, blokowanie, dane przestrzenne, procedury składowane, podzaptania, transakcje, wyzwalacze, zarządzanie użytkownikami, widoki, funkcje analitczne i inne
- Analiza kodów źródłowych w celu przetestowania przypadków brzegowych
- Uruchomienie testów na co najmniej 6 bazach danych typu rozproszonego, które deklarują zgodność z PostgreSQL lub MySQL, m.in. CockroachDB, TiDB, YugabyteDB
- Stworzenie panelu do wizualizacji otrzymanych wyników.
- Analiza i kategoryzacja wyników. Kategoryzacja i ocena powagi znalezionych braków w zgodności.
- Porównanie wyników otrzymanych dla najnowszych wersji baz danych, ze wcześniejszymi wersjami z ostatnich kilku lat.
- Przeprowadzenie testów wydajnościowych w kontrolowanych warunkach, aby porównać czasy wykonania identycznych operacji pomiędzy tradycyjnymi a rozproszonymi bazami danych
- Ocena łatwości przeniesienia aplikacji korzystającej z tradycyjnej bazy danych na każdą z testowanych rozproszonych baz


## Pytania
Czy dodać wagi dla każdego z testu? jak dodać wagi? Czy powinny być 0.1, 0.2 czy może Ważne/ średnie/ mało ważne
**raczej nie - ewentualnie porównać grupy między bazami dla najnowsyzch wersji**

Czy zostawić wiele testów dla featurów jak GiS
- Istnieją skomplikowane zaawansowane funkcjonalności
  - Spatial PostGIS
  - GROUP BY CUBE
  - Rollup
  - TABLESAMPLE
  - CREATE RULE
  - PARTITION OF
  - LISTEN/NOTIFY
  - array subquery
  - Postgres LATERAL subqeury 
  - SUM OVER
  - LAG and LEAD window functions
  - NTILE window function


Jakie powinny być kategorie niezgodności? 
Czy powinnienem oceniać błędy pod względem tego jak łatwo je naprawić lub jak jasno są opisane, np. komunikat że dana funkcjonalność jest niewspierana - dobrze, zupełnie inne zachowanie niż podstawowa baza danych - źle.

Kategorie:
- Wiadomość wprost o niewspieranej funkcjonalności (dobrze) "PostgresException: XCEXF: temporary tables are only supported experimentally"
- Wiadomość o nierozpoznanej komendzie / składnie (nieźle) "at or near "compression": syntax error"
- Nie mówiący dużo błąd (słabo) **Nieskalfykiowany**
- Inny wynik niż w bazowej bazie danych (źle) "OverflowException: Value was either too large or too small for an Int32." - **Błąd wartości**

- Możliwy do poprawienia? Czy to osobna kategoria czy spróbować ocenić czy błąd jest możliwy do poprawienia. **ok**
- łatwe, średnie, trudnie/niemożliwe

Czy osobno mierzyć jak łatwo dany błąd byłoby poprawić?


Czy to jest okej, że tylko część baz danych będzie miała porównania pomiędzy wersjami (te z docker compose)  **ok**

Czy powinienem subiektywnie oceniać każdą bazę danych pod względem tego jak bardzo otwarci są o brakach kompatybilności? **ok**

Do pracy fajnie by wygenerować odpowiednie grafy. Czy wystarczą screenshoty ze strony czy lepiej byłoby napisać jakiś skrypt w pyplocie itp. do ich wygenerowania na podstawie wyników z testów.
**gnuplot poteżny ale skompikowany, matpllib wystarczy **

# TODO
- CrateDB - Could not create a test database - oprócz dla najnowszej wersji, najnowsza wersja ma większość testów na failed - we wcześniejszych wersjach nie wspiera CREATE SCEHEMA
- Shardingsphere - Could not create a test database dla większości wersji, większość testów sfailowało - dlaczego?. Działa ale tylko dla klientów postgres.

# TODO
- Oceanbase - Could not create a test database dla większości wersji oprócz najnowszej
- singlestore - Could not create a test database dla żadnej wersji
- TiDB - lokalnie kuberenets pełno błędów przy połączeniach
- yugabyte - lokalnie kubernetes tak samo pełno błędów przy otwieraniu połączeń, na serwerze pody się nie uruchamiają. nie chcę przejść w stan READY.



do początku lipca - do 25 czerwca
albo druga połowa września

3 miesiące od złożenia pracy obrona

https://www.overleaf.com/project/69e7ac2ec1ea7e2937d8e342