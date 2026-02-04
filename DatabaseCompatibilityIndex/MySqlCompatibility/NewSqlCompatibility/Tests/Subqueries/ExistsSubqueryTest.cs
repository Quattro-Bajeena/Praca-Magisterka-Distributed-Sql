using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Subqueries;

[SqlTest(SqlFeatureCategory.Subqueries, "Test EXISTS subquery")]
public class ExistsSubqueryTest : SqlTest
{
    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "CREATE TABLE customers_sub (id INT PRIMARY KEY, name VARCHAR(50))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "CREATE TABLE orders_sub (id INT PRIMARY KEY, customer_id INT, total DECIMAL(10,2))";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO customers_sub VALUES (1, 'Alice'), (2, 'Bob'), (3, 'Charlie')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO orders_sub VALUES (1, 1, 100.0), (2, 1, 200.0), (3, 3, 150.0)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "SELECT COUNT(*) FROM customers_sub c WHERE EXISTS (SELECT 1 FROM orders_sub o WHERE o.customer_id = c.id)";
        object? count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "EXISTS subquery should find 2 customers with orders");

        cmd.CommandText = "DROP TABLE orders_sub";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE customers_sub";
        cmd.ExecuteNonQuery();
    }
}
