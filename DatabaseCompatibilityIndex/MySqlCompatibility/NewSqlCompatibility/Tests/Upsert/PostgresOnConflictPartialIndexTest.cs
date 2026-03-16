using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Upsert;

[SqlTest(SqlFeatureCategory.Upsert, "Test PostgreSQL ON CONFLICT with partial indexes", DatabaseType.PostgreSql)]
public class PostgresOnConflictPartialIndexTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = @"CREATE TABLE subscriptions_pg (
                            id SERIAL PRIMARY KEY,
                            user_id INT,
                            subscription_type VARCHAR(50),
                            status VARCHAR(20),
                            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE UNIQUE INDEX unique_active_subscription 
                           ON subscriptions_pg (user_id, subscription_type) 
                           WHERE status = 'active'";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"INSERT INTO subscriptions_pg (user_id, subscription_type, status) 
                           VALUES (1, 'premium', 'active')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO subscriptions_pg (user_id, subscription_type, status) 
                           VALUES (1, 'premium', 'active')
                           ON CONFLICT (user_id, subscription_type) WHERE status = 'active' 
                           DO UPDATE SET created_at = CURRENT_TIMESTAMP";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM subscriptions_pg WHERE user_id = 1 AND subscription_type = 'premium' AND status = 'active'";
        object? count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "Should have only 1 active premium subscription");

        cmd.CommandText = @"INSERT INTO subscriptions_pg (user_id, subscription_type, status) 
                           VALUES (1, 'premium', 'cancelled')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM subscriptions_pg WHERE user_id = 1 AND subscription_type = 'premium'";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Should allow cancelled subscription (partial index doesn't apply)");

        cmd.CommandText = @"INSERT INTO subscriptions_pg (user_id, subscription_type, status) 
                           VALUES (2, 'basic', 'active')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO subscriptions_pg (user_id, subscription_type, status) 
                           VALUES (2, 'basic', 'active')
                           ON CONFLICT (user_id, subscription_type) WHERE status = 'active' 
                           DO NOTHING";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM subscriptions_pg WHERE user_id = 2";
        count = cmd.ExecuteScalar();
        AssertEqual(1L, Convert.ToInt64(count!), "DO NOTHING should prevent duplicate active subscription");

        cmd.CommandText = @"INSERT INTO subscriptions_pg (user_id, subscription_type, status) 
                           VALUES (3, 'premium', 'active')
                           ON CONFLICT (user_id, subscription_type) WHERE status = 'active' 
                           DO UPDATE SET status = 'renewed'
                           RETURNING id, user_id, subscription_type, status";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should insert new subscription for user 3");
            AssertEqual("active", reader.GetString(3), "Should be active (no conflict)");
        }

        cmd.CommandText = "SELECT COUNT(*) FROM subscriptions_pg";
        count = cmd.ExecuteScalar();
        AssertTrue(Convert.ToInt64(count!) >= 4, "Should have multiple subscriptions");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS subscriptions_pg CASCADE";
        cmd.ExecuteNonQuery();
    }
}
