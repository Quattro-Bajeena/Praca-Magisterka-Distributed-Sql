using NSCI.Configuration;
using NSCI.Testing;
using System.Data.Common;

namespace NSCI.Tests.Subqueries;

[SqlTest(SqlFeatureCategory.Subqueries, "Test PostgreSQL array subqueries", DatabaseType.PostgreSql)]
public class PostgresArraySubqueryTest : SqlTest
{
    protected override void SetupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        
        cmd.CommandText = @"CREATE TABLE teams (
                            id SERIAL PRIMARY KEY,
                            name VARCHAR(100)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"CREATE TABLE team_members (
                            id SERIAL PRIMARY KEY,
                            team_id INT,
                            member_name VARCHAR(100),
                            role VARCHAR(50)
                        )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO teams (name) VALUES ('Alpha'), ('Beta'), ('Gamma')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO team_members (team_id, member_name, role) VALUES (1, 'Alice', 'Lead'), (1, 'Bob', 'Developer'), (1, 'Charlie', 'Designer')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO team_members (team_id, member_name, role) VALUES (2, 'David', 'Lead'), (2, 'Eve', 'Developer')";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO team_members (team_id, member_name, role) VALUES (3, 'Frank', 'Lead')";
        cmd.ExecuteNonQuery();
    }

    protected override void ExecutePg(DbConnection connection, DbConnection connectionSecond)
    {
        using DbCommand cmd = connection.CreateCommand();

        cmd.CommandText = @"
            SELECT t.name,
                   ARRAY(SELECT member_name FROM team_members WHERE team_id = t.id ORDER BY member_name) as members
            FROM teams t
            ORDER BY t.name";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should have team results");
            string teamName = reader.GetString(0);
            AssertTrue(teamName != null, "Team name should not be null");
            
            object membersArray = reader.GetValue(1);
            AssertTrue(membersArray != null, "Members array should not be null");
        }

        cmd.CommandText = @"
            SELECT t.name,
                   (SELECT COUNT(*) FROM team_members WHERE team_id = t.id) as member_count,
                   ARRAY(SELECT role FROM team_members WHERE team_id = t.id) as roles
            FROM teams t
            WHERE t.id = 1";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            AssertTrue(reader.Read(), "Should find team 1");
            long memberCount = reader.GetInt64(1);
            AssertEqual(3L, memberCount, "Team 1 should have 3 members");
        }

        cmd.CommandText = @"
            SELECT member_name
            FROM team_members
            WHERE role = ANY(ARRAY['Lead', 'Developer'])
            ORDER BY member_name";
        
        int anyCount = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                anyCount++;
            }
        }
        AssertTrue(anyCount >= 4, "Should find Leads and Developers using ANY");

        cmd.CommandText = @"
            SELECT t.name
            FROM teams t
            WHERE 'Lead' = ANY(SELECT role FROM team_members WHERE team_id = t.id)";
        
        int teamsWithLead = 0;
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                teamsWithLead++;
            }
        }
        AssertEqual(3, teamsWithLead, "All teams should have a Lead");

        cmd.CommandText = @"
            SELECT t.name,
                   array_agg(tm.member_name ORDER BY tm.member_name) as all_members
            FROM teams t
            LEFT JOIN team_members tm ON t.id = tm.team_id
            GROUP BY t.id, t.name
            ORDER BY t.name";
        
        using (DbDataReader reader = cmd.ExecuteReader())
        {
            int teamCount = 0;
            while (reader.Read())
            {
                teamCount++;
                object members = reader.GetValue(1);
                AssertTrue(members != null, "Should have aggregated members array");
            }
            AssertEqual(3, teamCount, "Should have 3 teams");
        }
    }

    protected override void CleanupPg(DbConnection connection)
    {
        using DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = "DROP TABLE IF EXISTS team_members CASCADE";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "DROP TABLE IF EXISTS teams CASCADE";
        cmd.ExecuteNonQuery();
    }
}
