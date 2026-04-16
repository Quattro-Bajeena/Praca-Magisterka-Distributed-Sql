using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Aggregations;

// GROUP BY ROLLUP(a, b) is a compact way to request a hierarchy of subtotals in one query.
// It expands to a GROUPING SETS that covers every prefix of the column list:
//   ROLLUP(region, product)  ≡  GROUPING SETS ((region, product), (region), ())
//
// For each grouping set a summary row is emitted with NULL substituted for the
// "rolled-up" dimensions.  The resulting NULL must not be confused with genuine NULLs
// in the data — the GROUPING(col) function returns 1 when the NULL was introduced by
// the rollup and 0 when the value came from an actual data row.
//
// Both MySQL (8.0+) and PostgreSQL support this syntax:
//   GROUP BY ROLLUP(col1, col2)
[SqlTest(SqlFeatureCategory.Aggregations, "Test GROUP BY ROLLUP generates per-group subtotals and a grand total")]
public class RollupTest : SqlTest
{
    // Data layout — 4 rows across a 2×2 grid (2 regions × 2 products):
    //
    //           Widget  Gadget   row total
    //  North      100     200       300
    //  South      150     250       400
    //  col total  250     450       700  ← grand total
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE sales_rollup (
            id      INT AUTO_INCREMENT PRIMARY KEY,
            region  VARCHAR(20)    NOT NULL,
            product VARCHAR(20)    NOT NULL,
            amount  DECIMAL(10, 2) NOT NULL
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO sales_rollup (region, product, amount) VALUES
            ('North', 'Widget', 100),
            ('North', 'Gadget', 200),
            ('South', 'Widget', 150),
            ('South', 'Gadget', 250)";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE sales_rollup (
            id      SERIAL         PRIMARY KEY,
            region  TEXT           NOT NULL,
            product TEXT           NOT NULL,
            amount  DECIMAL(10, 2) NOT NULL
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO sales_rollup (region, product, amount) VALUES
            ('North', 'Widget', 100),
            ('North', 'Gadget', 200),
            ('South', 'Widget', 150),
            ('South', 'Gadget', 250)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        // ROLLUP over 2 columns with 4 data rows produces:
        //   4 detail rows  (region, product)
        //   2 subtotal rows  (region, NULL)   — one per region
        //   1 grand-total row  (NULL, NULL)
        //   ─────────────────────────────────
        //   7 rows total
        cmd.CommandText = @"SELECT COUNT(*) FROM (
                                SELECT region, product, SUM(amount)
                                FROM sales_rollup
                                GROUP BY ROLLUP(region, product)
                            ) sub";
        long rowCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(7L, rowCount, "ROLLUP(region, product) should produce 7 rows (4 detail + 2 subtotal + 1 grand total)");

        // Grand total: GROUPING(region)=1 flags the row where region was rolled up
        cmd.CommandText = @"SELECT SUM(amount) FROM sales_rollup
                            GROUP BY ROLLUP(region, product)
                            HAVING GROUPING(region) = 1 AND GROUPING(product) = 1";
        decimal grandTotal = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(700m, grandTotal, "Grand total should be 700");

        // North subtotal: region is a real value, product was rolled up
        cmd.CommandText = @"SELECT SUM(amount) FROM sales_rollup
                            GROUP BY ROLLUP(region, product)
                            HAVING region = 'North' AND GROUPING(product) = 1";
        decimal northSubtotal = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(300m, northSubtotal, "North subtotal should be 300");

        // South subtotal
        cmd.CommandText = @"SELECT SUM(amount) FROM sales_rollup
                            GROUP BY ROLLUP(region, product)
                            HAVING region = 'South' AND GROUPING(product) = 1";
        decimal southSubtotal = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(400m, southSubtotal, "South subtotal should be 400");

        // Individual data cell — no rollup, normal GROUP BY behaviour
        cmd.CommandText = @"SELECT SUM(amount) FROM sales_rollup
                            WHERE region = 'North' AND product = 'Widget'
                            GROUP BY region, product";
        decimal cell = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(100m, cell, "North/Widget cell should be 100");
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"SELECT COUNT(*) FROM (
                                SELECT region, product, SUM(amount)
                                FROM sales_rollup
                                GROUP BY ROLLUP(region, product)
                            ) sub";
        long rowCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(7L, rowCount, "ROLLUP(region, product) should produce 7 rows (4 detail + 2 subtotal + 1 grand total)");

        cmd.CommandText = @"SELECT SUM(amount) FROM sales_rollup
                            GROUP BY ROLLUP(region, product)
                            HAVING GROUPING(region) = 1 AND GROUPING(product) = 1";
        decimal grandTotal = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(700m, grandTotal, "Grand total should be 700");

        cmd.CommandText = @"SELECT SUM(amount) FROM sales_rollup
                            GROUP BY ROLLUP(region, product)
                            HAVING region = 'North' AND GROUPING(product) = 1";
        decimal northSubtotal = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(300m, northSubtotal, "North subtotal should be 300");

        cmd.CommandText = @"SELECT SUM(amount) FROM sales_rollup
                            GROUP BY ROLLUP(region, product)
                            HAVING region = 'South' AND GROUPING(product) = 1";
        decimal southSubtotal = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(400m, southSubtotal, "South subtotal should be 400");

        cmd.CommandText = @"SELECT SUM(amount) FROM sales_rollup
                            WHERE region = 'North' AND product = 'Widget'
                            GROUP BY region, product";
        decimal cell = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(100m, cell, "North/Widget cell should be 100");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS sales_rollup";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS sales_rollup CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
