using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Performance;

[SqlTest(SqlFeatureCategory.Indexes, "Test STRAIGHT_JOIN with hints")]
public class StraightJoinTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = @"CREATE TABLE dept (
                            id INT PRIMARY KEY,
                            name VARCHAR(50),
                            INDEX idx_name (name)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE emp (
                            id INT PRIMARY KEY,
                            name VARCHAR(50),
                            dept_id INT,
                            INDEX idx_dept (dept_id)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO dept VALUES (1, 'IT'), (2, 'HR'), (3, 'Sales')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO emp VALUES (1, 'Alice', 1), (2, 'Bob', 2), (3, 'Charlie', 1), (4, 'Diana', 3)";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"SELECT d.name, e.name 
                           FROM dept d STRAIGHT_JOIN emp e 
                           ON d.id = e.dept_id 
                           WHERE d.id = 1";
        using DbDataReader reader = cmd.ExecuteReader();
        int count = 0;
        while (reader.Read())
        {
            count++;
        }
        AssertEqual(2, count, "STRAIGHT_JOIN should find 2 IT department employees");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE emp";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "DROP TABLE dept";
        cmd.ExecuteNonQuery();
    }
}
