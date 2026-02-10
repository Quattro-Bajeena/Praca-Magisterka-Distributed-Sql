using NSCI.Configuration;
using System.Data.Common;

namespace NSCI.Testing;

public abstract class SqlTest
{

    private DatabaseConfiguration _config = null!;

    public void Initialize(DatabaseConfiguration config)
    {
        _config = config;
    }

    public void Setup(DbConnection connection)
    {
        switch (_config.Type)
        {
            case DatabaseType.MySql:
                SetupMy(connection);
                break;
            case DatabaseType.PostgreSql:
                SetupPg(connection);
                break;
        }
    }

    protected virtual void SetupMy(DbConnection connection)
    {
        if (SetupCommandMy != null)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = SetupCommandMy;
            command.ExecuteNonQuery();
        }
    }

    protected virtual void SetupPg(DbConnection connection)
    {
        if (SetupCommandPg != null)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = SetupCommandPg;
            command.ExecuteNonQuery();
        }
    }

    public void Execute(DbConnection connection, DbConnection connectionSecond)
    {
        switch (_config.Type)
        {
            case DatabaseType.MySql:
                ExecuteMy(connection, connectionSecond);
                break;
            case DatabaseType.PostgreSql:
                ExecutePg(connection, connectionSecond);
                break;
        }
    }

    protected virtual void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        if (CommandMy != null)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = CommandMy;
            command.ExecuteNonQuery();
        }
        else
        {
            throw new Exception("No command defined for MySQL test execution");
        }
    }

    protected virtual void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        if (CommandMy != null)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = CommandMy;
            command.ExecuteNonQuery();
        }
        else
        {
            throw new Exception("No command defined for PostgreSQL test execution");
        }
    }

    public void Cleanup(DbConnection connection)
    {
        switch (_config.Type)
        {
            case DatabaseType.MySql:
                CleanupMy(connection);
                break;
            case DatabaseType.PostgreSql:
                CleanupPg(connection);
                break;
        }
    }

    protected virtual void CleanupMy(DbConnection connection)
    {
        if (CleanupCommandMy != null)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = CleanupCommandMy;
            command.ExecuteNonQuery();
        }
    }

    protected virtual void CleanupPg(DbConnection connection)
    {
        if (CleanupCommandMy != null)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = CleanupCommandPg;
            command.ExecuteNonQuery();
        }
    }


    protected virtual string? CommandMy => null;


    protected virtual string? CommandPg => null;

    protected virtual string? SetupCommandMy => null;

    protected virtual string? SetupCommandPg => null;

    protected virtual string? CleanupCommandMy => null;

    protected virtual string? CleanupCommandPg => null;

    protected void AssertEqual<T>(T? expected, T? actual, string message = "")
    {
        if (!Equals(expected, actual))
        {
            string msg = string.IsNullOrEmpty(message)
                ? $"Assertion failed: expected '{expected}' but got '{actual}'"
                : $"{message} (expected '{expected}' but got '{actual}')";
            throw new AssertionException(msg);
        }
    }

    protected void AssertTrue(bool condition, string message = "")
    {
        if (!condition)
        {
            string msg = string.IsNullOrEmpty(message)
                ? "Assertion failed: condition is false"
                : $"Assertion failed: {message}";
            throw new AssertionException(msg);
        }
    }

    protected void AssertRowCount(DbDataReader reader, int expected)
    {
        int count = 0;
        while (reader.Read())
            count++;

        AssertEqual(expected, count, "Row count mismatch");
    }

    protected void AssertHasRows(DbDataReader reader, string message = "")
    {
        if (!reader.HasRows)
        {
            throw new AssertionException(
                string.IsNullOrEmpty(message)
                    ? "Result set is empty"
                    : $"Result set is empty: {message}");
        }
    }

    protected T? ExecuteScalar<T>(DbConnection connection, string sql)
    {
        using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        object? result = command.ExecuteScalar();
        return result == null || result == DBNull.Value ? default : (T?)Convert.ChangeType(result, typeof(T));
    }

    protected void AssertThrows<TException>(Action action, string message) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new AssertionException($"Expected {typeof(TException).Name} but got {ex.GetType().Name}: {message}");
        }

        throw new AssertionException(message);
    }

    protected void ExecuteIgnoringException(Action action)
    {
        try
        {
            action();
        }
        catch
        {
        }
    }
}

public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}
