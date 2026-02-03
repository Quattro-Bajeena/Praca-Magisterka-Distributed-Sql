using NSCI.Configuration;
using NSCI.Testing;
namespace NSCI.Tests.Advanced;

[SqlTest(SqlFeatureCategory.Triggers, "Test TRIGGER (likely unsupported in distributed databases)", DatabaseType.MySql)]
public class TriggerTest : SqlTest
{
    public override string? SetupCommand => "CREATE TABLE trigger_test_table (id INT PRIMARY KEY, value INT)";


    public override string? Command => @"
            CREATE TRIGGER tr_test 
            BEFORE INSERT ON trigger_test_table 
            FOR EACH ROW 
            BEGIN 
                SET NEW.value = NEW.value * 2; 
            END";

    public override string? CleanupCommand => "DROP TABLE IF EXISTS trigger_test_table";
}
