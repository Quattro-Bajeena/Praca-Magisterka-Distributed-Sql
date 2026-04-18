using NSCI.Testing;

namespace NSCI.Tests.Misc;

// TODO
// https://www.postgresql.org/docs/current/sql-notify.html
//[SqlTest(SqlFeatureCategory.Misc, "Test LISTEN/NOTIFY delivers payloads between connections and UNLISTEN stops delivery", DatabaseType.PostgreSql)]
public class PostgresListenNotifyTest : SqlTest
{

}
