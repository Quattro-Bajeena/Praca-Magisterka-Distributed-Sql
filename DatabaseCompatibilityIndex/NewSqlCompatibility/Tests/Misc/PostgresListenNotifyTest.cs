using Npgsql;
using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Misc;

// LISTEN/NOTIFY is PostgreSQL's built-in publish-subscribe mechanism.
//
// LISTEN channel       — subscribes the current connection to a named channel.
// NOTIFY channel, msg  — queues a notification for every connection listening on that
//                        channel. The notification is delivered at transaction commit,
//                        so a standalone NOTIFY (auto-commit) delivers immediately.
// UNLISTEN channel     — cancels the subscription on the current connection.
//
// Notifications are out-of-band protocol messages.  Npgsql surfaces them via the
// NpgsqlConnection.Notification event.  The event fires when Npgsql reads the server
// response to the next command issued on the listening connection — the pending
// notification messages are prepended to the response stream by the server.
[SqlTest(SqlFeatureCategory.Misc, "Test LISTEN/NOTIFY delivers payloads between connections and UNLISTEN stops delivery", DatabaseType.PostgreSql)]
public class PostgresListenNotifyTest : SqlTest
{
    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        // connectionSecond is the listener; connection is the notifier.
        NpgsqlConnection listenConn = (NpgsqlConnection)connectionSecond;

        string? receivedChannel = null;
        string? receivedPayload = null;

        listenConn.Notification += (_, e) =>
        {
            receivedChannel = e.Channel;
            receivedPayload = e.Payload;
        };

        using (DbCommand listenCmd = listenConn.CreateCommand())
        {
            listenCmd.CommandText = "LISTEN test_events";
            listenCmd.ExecuteNonQuery();
        }

        // NOTIFY from the other connection; auto-commit makes delivery immediate
        using (DbCommand notifyCmd = connection.CreateCommand())
        {
            notifyCmd.CommandText = "NOTIFY test_events, 'hello-world'";
            notifyCmd.ExecuteNonQuery();
        }

        // Issuing any command on the listening connection causes Npgsql to read the
        // server's response stream, which includes the pending NotificationResponse
        // message and fires the Notification event before returning the query result.
        using (DbCommand triggerCmd = listenConn.CreateCommand())
        {
            triggerCmd.CommandText = "SELECT 1";
            triggerCmd.ExecuteScalar();
        }

        AssertEqual("test_events", receivedChannel, "Notification should arrive on channel 'test_events'");
        AssertEqual("hello-world", receivedPayload, "Notification payload should be 'hello-world'");

        // pg_notify('channel', 'payload') is the function-call equivalent of the NOTIFY
        // command — useful when NOTIFY must be issued from inside a PL/pgSQL function
        receivedPayload = null;
        using (DbCommand pgNotifyCmd = connection.CreateCommand())
        {
            pgNotifyCmd.CommandText = "SELECT pg_notify('test_events', 'via-function')";
            pgNotifyCmd.ExecuteScalar();
        }

        using (DbCommand triggerCmd = listenConn.CreateCommand())
        {
            triggerCmd.CommandText = "SELECT 1";
            triggerCmd.ExecuteScalar();
        }

        AssertEqual("via-function", receivedPayload, "pg_notify() should deliver the same way as NOTIFY");

        // Multiple notifications queued before the next command are all delivered
        // in one round-trip when SELECT 1 is issued on the listening connection
        int notificationCount = 0;
        listenConn.Notification += (_, _) => notificationCount++;

        for (int i = 1; i <= 3; i++)
        {
            using DbCommand batchCmd = connection.CreateCommand();
            batchCmd.CommandText = $"NOTIFY test_events, 'msg-{i}'";
            batchCmd.ExecuteNonQuery();
        }

        using (DbCommand triggerCmd = listenConn.CreateCommand())
        {
            triggerCmd.CommandText = "SELECT 1";
            triggerCmd.ExecuteScalar();
        }

        AssertEqual(3, notificationCount, "All 3 pending notifications should be delivered in one flush");

        // UNLISTEN removes the subscription; notifications sent afterward are ignored
        using (DbCommand unlistenCmd = listenConn.CreateCommand())
        {
            unlistenCmd.CommandText = "UNLISTEN test_events";
            unlistenCmd.ExecuteNonQuery();
        }

        receivedPayload = null;
        using (DbCommand notifyCmd = connection.CreateCommand())
        {
            notifyCmd.CommandText = "NOTIFY test_events, 'after-unlisten'";
            notifyCmd.ExecuteNonQuery();
        }

        using (DbCommand triggerCmd = listenConn.CreateCommand())
        {
            triggerCmd.CommandText = "SELECT 1";
            triggerCmd.ExecuteScalar();
        }

        AssertEqual(null, receivedPayload, "No notification should be received after UNLISTEN");
    }
}
