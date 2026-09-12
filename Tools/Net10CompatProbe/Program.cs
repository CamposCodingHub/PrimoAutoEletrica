using System.Windows;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

namespace Net10CompatProbe;

public class Program
{
    [STAThread]
    public static int Main()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        using var conn = new SqliteConnection("Data Source=:memory:");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1;";
        var v = cmd.ExecuteScalar();
        Console.WriteLine($"PROBE_OK sqlite={v} tfm=net10.0-windows wpf={typeof(Window).Assembly.GetName().Version}");
        return 0;
    }
}
