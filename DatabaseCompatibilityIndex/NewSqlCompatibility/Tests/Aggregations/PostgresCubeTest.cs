using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Aggregations;

// GROUP BY CUBE(a, b) generates subtotals for every possible subset (power set) of
// the supplied columns — in contrast to ROLLUP, which only covers prefixes.
//
//   CUBE(region, product)  ≡  GROUPING SETS (
//       (region, product),   -- detail rows
//       (region),            -- region subtotals   (like ROLLUP)
//       (product),           -- product subtotals  (extra vs ROLLUP)
//       ()                   -- grand total
//   )
//
// For n dimensions, CUBE produces at most 2ⁿ grouping sets.
// With 2 dimensions and 4 data rows the result contains:
//   4 detail rows  + 2 region subtotals  + 2 product subtotals  + 1 grand total  = 9 rows
//
// MySQL does NOT support CUBE — this test is PostgreSQL-only.

// https://www.postgresql.org/docs/current/queries-table-expressions.html
[SqlTest(SqlFeatureCategory.Aggregations, "Test GROUP BY CUBE generates all-dimension subtotals including cross-column aggregates", DatabaseType.PostgreSql)]
public class PostgresCubeTest : SqlTest
{
    // Data layout — same 2×2 grid used in RollupTest for easy cross-reference:
    //
    //           Widget  Gadget   row total
    //  North      100     200       300
    //  South      150     250       400
    //  col total  250     450       700  ← grand total
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"CREATE TABLE sales_cube (
            id      SERIAL         PRIMARY KEY,
            region  TEXT           NOT NULL,
            product TEXT           NOT NULL,
            amount  DECIMAL(10, 2) NOT NULL
        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO sales_cube (region, product, amount) VALUES
            ('North', 'Widget', 100),
            ('North', 'Gadget', 200),
            ('South', 'Widget', 150),
            ('South', 'Gadget', 250)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        // Total rows: 4 detail + 2 region subtotals + 2 product subtotals + 1 grand total = 9
        cmd.CommandText = @"SELECT COUNT(*) FROM (
                                SELECT region, product, SUM(amount)
                                FROM sales_cube
                                GROUP BY CUBE(region, product)
                            ) sub";
        long rowCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(9L, rowCount, "CUBE(region, product) should produce 9 rows");

        // CUBE produces 2 more rows than ROLLUP over the same columns because it adds
        // the (NULL, product) product-only subtotals that ROLLUP omits.
        cmd.CommandText = @"SELECT COUNT(*) FROM (
                                SELECT region, product, SUM(amount)
                                FROM sales_cube
                                GROUP BY ROLLUP(region, product)
                            ) sub";
        long rollupCount = Convert.ToInt64(cmd.ExecuteScalar()!);
        AssertEqual(rowCount - 2, rollupCount,
            "CUBE should produce exactly 2 more rows than ROLLUP for the same 2-column input");

        // Grand total row: both dimensions rolled up
        cmd.CommandText = @"SELECT SUM(amount) FROM sales_cube
                            GROUP BY CUBE(region, product)
                            HAVING GROUPING(region) = 1 AND GROUPING(product) = 1";
        decimal grandTotal = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(700m, grandTotal, "Grand total should be 700");

        // Region subtotals (region is real, product was rolled up)
        cmd.CommandText = @"SELECT SUM(amount) FROM sales_cube
                            GROUP BY CUBE(region, product)
                            HAVING region = 'North' AND GROUPING(product) = 1";
        decimal northSubtotal = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(300m, northSubtotal, "North region subtotal should be 300");

        cmd.CommandText = @"SELECT SUM(amount) FROM sales_cube
                            GROUP BY CUBE(region, product)
                            HAVING region = 'South' AND GROUPING(product) = 1";
        decimal southSubtotal = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(400m, southSubtotal, "South region subtotal should be 400");

        // Product subtotals — unique to CUBE, absent from ROLLUP (region rolled up, product real)
        cmd.CommandText = @"SELECT SUM(amount) FROM sales_cube
                            GROUP BY CUBE(region, product)
                            HAVING GROUPING(region) = 1 AND product = 'Widget'";
        decimal widgetSubtotal = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(250m, widgetSubtotal, "Widget product subtotal should be 250");

        cmd.CommandText = @"SELECT SUM(amount) FROM sales_cube
                            GROUP BY CUBE(region, product)
                            HAVING GROUPING(region) = 1 AND product = 'Gadget'";
        decimal gadgetSubtotal = Convert.ToDecimal(cmd.ExecuteScalar()!);
        AssertEqual(450m, gadgetSubtotal, "Gadget product subtotal should be 450");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS sales_cube CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
