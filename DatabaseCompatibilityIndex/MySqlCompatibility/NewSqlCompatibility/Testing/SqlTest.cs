using System.Data.Common;

namespace NSCI.Testing;

public abstract class SqlTest
{
    /// <summary>
    /// Optional: Override to provide a simple SQL command string.
    /// If not overridden or returns null, Execute() method will be called instead.
    /// </summary>
    public virtual string? Command => null;

    /// <summary>
    /// Optional: Override to implement setup logic before Execute() is called.
    /// Default implementation does nothing.
    /// </summary>
    public virtual void Setup(DbConnection connection)
    {
        if (SetupCommand != null)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = SetupCommand;
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Override to implement custom test logic.
    /// Default implementation executes the Command property if available.
    /// </summary>
    public virtual void Execute(DbConnection connection)
    {
        if (Command != null)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = Command;
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Optional: Override to implement cleanup logic after Execute() is called.
    /// Default implementation does nothing.
    /// </summary>
    public virtual void Cleanup(DbConnection connection)
    {
        if (CleanupCommand != null)
        {
            using DbCommand command = connection.CreateCommand();
            command.CommandText = CleanupCommand;
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// Optional: Override to provide a simple SQL setup command.
    /// If not overridden or returns null, Setup() method will be used instead.
    /// </summary>
    public virtual string? SetupCommand => null;

    /// <summary>
    /// Optional: Override to provide a simple SQL cleanup command.
    /// If not overridden or returns null, Cleanup() method will be used instead.
    /// </summary>
    public virtual string? CleanupCommand => null;

    /// <summary>
    /// Helper method for value assertions.
    /// </summary>
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

    /// <summary>
    /// Helper method for boolean assertions.
    /// </summary>
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

    /// <summary>
    /// Helper method to assert row count from reader.
    /// </summary>
    protected void AssertRowCount(DbDataReader reader, int expected)
    {
        int count = 0;
        while (reader.Read())
            count++;

        AssertEqual(expected, count, "Row count mismatch");
    }

    /// <summary>
    /// Helper method to assert non-empty result set.
    /// </summary>
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

    /// <summary>
    /// Helper method to parse first column value from query result.
    /// </summary>
    protected T? ExecuteScalar<T>(DbConnection connection, string sql)
    {
        using DbCommand command = connection.CreateCommand();
        command.CommandText = sql;
        object? result = command.ExecuteScalar();
        return result == null || result == DBNull.Value ? default : (T?)Convert.ChangeType(result, typeof(T));
    }

    /// <summary>
    /// Helper method to assert that an action throws a specific exception type.
    /// </summary>
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

    /// <summary>
    /// Helper method to execute an action and ignore any exception.
    /// </summary>
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

/// <summary>
/// Custom exception for assertion failures.
/// </summary>
public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}
