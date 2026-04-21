could not create a test database - oprócz dla najnowszej wersji, najnowsza wersja ma większość testów na failed

https://community.cratedb.com/t/create-new-schema/828/2

```
In CrateDB you can just create a table within a schema like

CREATE TABLE schema_name.table_name

Also, you can grant rights like so on a schema (even if it doesn’t explicitly exists yet):

GRANT DQL ON SCHEMA schema_name TO user_name;

    In CrateDB, schemas are just namespaces that are created and dropped implicitly. Therefore, when GRANT , DENY or REVOKE are invoked on a schema level, CrateDB takes the schema name provided without further validation.
```


- CrateDB 3 - Could not create a test database: 0A000: unknown function: version()