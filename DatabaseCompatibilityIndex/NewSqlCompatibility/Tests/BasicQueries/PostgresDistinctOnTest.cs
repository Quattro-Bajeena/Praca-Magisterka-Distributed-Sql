using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "Test DISTINCT ON returns one row per distinct key with controlled ordering", DatabaseType.PostgreSql)]
public class PostgresDistinctOnTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE products_distinct (
            id       SERIAL PRIMARY KEY,
            category TEXT           NOT NULL,
            name     TEXT           NOT NULL,
            price    DECIMAL(10, 2) NOT NULL
        )";
        cmd.ExecuteNonQuery();

        // 3 categories, multiple products each
        //  Electronics: Laptop 999.99, Phone 599.99, Headphones 149.99
        //  Clothing:    Jacket  89.99, Shirt    29.99
        //  Books:       Textbook 49.99, Novel   19.99
        string[] inserts =
        [
            "('Electronics', 'Laptop',     999.99)",
            "('Electronics', 'Phone',      599.99)",
            "('Electronics', 'Headphones', 149.99)",
            "('Clothing',    'Jacket',      89.99)",
            "('Clothing',    'Shirt',       29.99)",
            "('Books',       'Textbook',    49.99)",
            "('Books',       'Novel',       19.99)",
        ];

        foreach (string values in inserts)
        {
            cmd.CommandText = $"INSERT INTO products_distinct (category, name, price) VALUES {values}";
            cmd.ExecuteNonQuery();
        }
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        // DISTINCT ON (category) with ASC price → cheapest product per category
        cmd.CommandText = @"SELECT COUNT(*) FROM (
                                SELECT DISTINCT ON (category) category, name, price
                                FROM products_distinct
                                ORDER BY category, price ASC
                            ) sub";
        long countAsc = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(3L, countAsc, "DISTINCT ON should return exactly one row per category (3 categories)");

        // Cheapest in Electronics should be Headphones (149.99)
        cmd.CommandText = @"SELECT name FROM (
                                SELECT DISTINCT ON (category) category, name, price
                                FROM products_distinct
                                ORDER BY category, price ASC
                            ) sub
                            WHERE category = 'Electronics'";
        object? cheapestElectronics = cmd.ExecuteScalar();
        AssertEqual("Headphones", cheapestElectronics?.ToString(),
            "Cheapest Electronics product should be Headphones");

        // Cheapest in Books should be Novel (19.99)
        cmd.CommandText = @"SELECT name FROM (
                                SELECT DISTINCT ON (category) category, name, price
                                FROM products_distinct
                                ORDER BY category, price ASC
                            ) sub
                            WHERE category = 'Books'";
        object? cheapestBooks = cmd.ExecuteScalar();
        AssertEqual("Novel", cheapestBooks?.ToString(),
            "Cheapest Books product should be Novel");

        // DISTINCT ON (category) with DESC price → most expensive product per category
        cmd.CommandText = @"SELECT name FROM (
                                SELECT DISTINCT ON (category) category, name, price
                                FROM products_distinct
                                ORDER BY category, price DESC
                            ) sub
                            WHERE category = 'Electronics'";
        object? mostExpensiveElectronics = cmd.ExecuteScalar();
        AssertEqual("Laptop", mostExpensiveElectronics?.ToString(),
            "Most expensive Electronics product should be Laptop");

        cmd.CommandText = @"SELECT name FROM (
                                SELECT DISTINCT ON (category) category, name, price
                                FROM products_distinct
                                ORDER BY category, price DESC
                            ) sub
                            WHERE category = 'Clothing'";
        object? mostExpensiveClothing = cmd.ExecuteScalar();
        AssertEqual("Jacket", mostExpensiveClothing?.ToString(),
            "Most expensive Clothing product should be Jacket");

        // DISTINCT ON (category) result contains exactly one row per category, not per product
        cmd.CommandText = @"SELECT COUNT(*) FROM products_distinct";
        long totalProducts = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(7L, totalProducts, "Base table should have 7 products");

        cmd.CommandText = @"SELECT COUNT(*) FROM (
                                SELECT DISTINCT ON (category) category
                                FROM products_distinct
                                ORDER BY category
                            ) sub";
        long distinctCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(3L, distinctCount,
            "DISTINCT ON should collapse 7 products into 3 category representatives");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS products_distinct CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
