using System;
using System.Data;
using System.IO;
using Microsoft.Data.SqlClient;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: DbConfigurator test|create-schema <connectionString>");
            return 2;
        }

        var cmd = args[0].ToLowerInvariant();
        if (args.Length < 2)
        {
            Console.WriteLine("Missing connection string");
            return 2;
        }

        var cs = args[1];

        try
        {
            if (cmd == "test")
            {
                return TestConnection(cs) ? 0 : 3;
            }
            else if (cmd == "create-schema")
            {
                var solutionRoot = AppContext.BaseDirectory;
                // try to locate SqlServerSchema.sql in repository
                var candidate = Path.Combine(solutionRoot, "..", "..", "..", "..", "PrimoAutoEletrica", "Services", "DatabaseProviders", "SqlServerSchema.sql");
                if (!File.Exists(candidate)) candidate = Path.Combine(solutionRoot, "..", "..", "..", "..", "Services", "DatabaseProviders", "SqlServerSchema.sql");
                if (!File.Exists(candidate))
                {
                    Console.WriteLine("Could not locate SqlServerSchema.sql. Provide a path or run from repo root.");
                    return 4;
                }

                var sql = File.ReadAllText(candidate);
                return ExecuteSql(cs, sql) ? 0 : 5;
            }
            else
            {
                Console.WriteLine("Unknown command: " + cmd);
                return 2;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            return 10;
        }
    }

    static bool TestConnection(string connectionString)
    {
        using var conn = new SqlConnection(connectionString);
        conn.Open();
        Console.WriteLine("OK: Connected to " + conn.DataSource + " / " + conn.Database);
        return true;
    }

    static bool ExecuteSql(string connectionString, string sql)
    {
        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandType = CommandType.Text;
        cmd.CommandText = sql;
        cmd.CommandTimeout = 600;
        cmd.ExecuteNonQuery();
        Console.WriteLine("Schema applied successfully.");
        return true;
    }
}
