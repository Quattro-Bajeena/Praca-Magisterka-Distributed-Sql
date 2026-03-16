using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.BasicQueries;

[SqlTest(SqlFeatureCategory.BasicQueries, "Test PostgreSQL WITH clause (Common Table Expressions)", DatabaseType.PostgreSql)]
public class PostgresWithClauseTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = @"CREATE TABLE employees_cte (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100),
                            department VARCHAR(50),
                            salary DECIMAL(10,2)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO employees_cte (name, department, salary) VALUES ('Alice', 'Engineering', 90000)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_cte (name, department, salary) VALUES ('Bob', 'Engineering', 85000)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_cte (name, department, salary) VALUES ('Charlie', 'Sales', 75000)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_cte (name, department, salary) VALUES ('David', 'Sales', 70000)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "INSERT INTO employees_cte (name, department, salary) VALUES ('Eve', 'HR', 65000)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"WITH dept_avg AS (
                               SELECT department, AVG(salary) as avg_salary
                               FROM employees_cte
                               GROUP BY department
                           )
                           SELECT COUNT(*) FROM dept_avg";
        object? count = cmd.ExecuteScalar();
        AssertEqual(3L, Convert.ToInt64(count!), "Should have 3 departments");

        cmd.CommandText = @"WITH dept_stats AS (
                               SELECT department, AVG(salary) as avg_salary, COUNT(*) as emp_count
                               FROM employees_cte
                               GROUP BY department
                           )
                           SELECT department, avg_salary, emp_count
                           FROM dept_stats
                           WHERE emp_count >= 2
                           ORDER BY avg_salary DESC";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have Engineering department");
            string dept = reader.GetString(0);
            AssertEqual("Engineering", dept, "Engineering should have highest average");
            long empCount = reader.GetInt64(2);
            AssertEqual(2L, empCount, "Engineering should have 2 employees");

            AssertTrue(reader.Read(), "Should have Sales department");
            dept = reader.GetString(0);
            AssertEqual("Sales", dept, "Sales should be second");
        }

        cmd.CommandText = @"WITH high_earners AS (
                               SELECT * FROM employees_cte WHERE salary > 70000
                           ),
                           engineering_high AS (
                               SELECT * FROM high_earners WHERE department = 'Engineering'
                           )
                           SELECT COUNT(*) FROM engineering_high";
        count = cmd.ExecuteScalar();
        AssertEqual(2L, Convert.ToInt64(count!), "Should have 2 high-earning engineers");

        cmd.CommandText = @"WITH RECURSIVE numbers AS (
                               SELECT 1 as n
                               UNION ALL
                               SELECT n + 1 FROM numbers WHERE n < 5
                           )
                           SELECT SUM(n) FROM numbers";
        object? sum = cmd.ExecuteScalar();
        AssertEqual(15L, Convert.ToInt64(sum!), "Sum of 1+2+3+4+5 should be 15");

        cmd.CommandText = @"WITH dept_totals AS (
                               SELECT department, SUM(salary) as total_salary
                               FROM employees_cte
                               GROUP BY department
                           )
                           SELECT 
                               e.name, 
                               e.salary,
                               dt.total_salary,
                               ROUND((e.salary / dt.total_salary * 100)::numeric, 2) as percentage
                           FROM employees_cte e
                           JOIN dept_totals dt ON e.department = dt.department
                           WHERE e.department = 'Engineering'
                           ORDER BY e.salary DESC
                           LIMIT 1";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have top Engineering employee");
            string name = reader.GetString(0);
            AssertEqual("Alice", name, "Alice should be the top earner");
            decimal percentage = reader.GetDecimal(3);
            AssertTrue(percentage > 50 && percentage < 55, "Alice's percentage should be ~51.4%");
        }
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS employees_cte CASCADE";
        ExecuteIgnoringException(() => cmd.ExecuteNonQuery());
    }
}
