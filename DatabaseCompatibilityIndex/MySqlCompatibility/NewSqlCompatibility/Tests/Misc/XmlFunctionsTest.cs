using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Misc;

[SqlTest(SqlFeatureCategory.Misc, "Test XML Functions ")]
public class XmlFunctionsTest : SqlTest
{
    protected override void SetupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE xml_test (id INT PRIMARY KEY, xml_data TEXT)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO xml_test VALUES (1, '<root><item>value1</item></root>')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecuteMy(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT ExtractValue('<root><item>test</item></root>', '/root/item') AS result";
        object? result = cmd.ExecuteScalar();
        AssertEqual("test", result?.ToString(), "ExtractValue should extract XML node value");

        cmd.CommandText = "SELECT UpdateXML('<root><item>old</item></root>', '/root/item', '<item>new</item>') AS result";
        result = cmd.ExecuteScalar();
        AssertTrue(result?.ToString()?.Contains("new") == true, "UpdateXML should update XML content");
    }

    protected override void CleanupMy(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS xml_test";
        cmd.ExecuteNonQuery();
    }

    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "CREATE TABLE xml_test (id INT PRIMARY KEY, xml_data XML)";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO xml_test VALUES (1, '<root><item>value1</item></root>')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = "SELECT xpath('/root/item/text()', '<root><item>test</item></root>'::xml)";
        object? result = cmd.ExecuteScalar();
        AssertTrue(result != null, "xpath should extract XML node value");

        cmd.CommandText = "SELECT xmlelement(name root, xmlelement(name item, 'new'))";
        result = cmd.ExecuteScalar();
        AssertTrue(result?.ToString()?.Contains("new") == true, "xmlelement should create XML content");
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS xml_test";
        cmd.ExecuteNonQuery();
    }
}
