using Microsoft.Data.Sqlite;

if (args.Length < 1)
{
    Console.Error.WriteLine("usage: <dbPath> [seed|verify]");
    return 2;
}

var db = args[0];
var mode = args.Length > 1 ? args[1] : "seed";
using var c = new SqliteConnection("Data Source=" + db);
c.Open();

if (mode == "seed")
{
    using (var cmd = c.CreateCommand())
    {
        cmd.CommandText =
            "CREATE TABLE IF NOT EXISTS C08_InstallerMarker (" +
            "Id INTEGER PRIMARY KEY, ClientName TEXT NOT NULL, VehiclePlate TEXT NOT NULL, " +
            "WorkOrderNote TEXT NOT NULL, CreatedUtc TEXT NOT NULL);" +
            "DELETE FROM C08_InstallerMarker;" +
            "INSERT INTO C08_InstallerMarker(Id, ClientName, VehiclePlate, WorkOrderNote, CreatedUtc) " +
            "VALUES (1, 'PRIMOX INSTALLER TEST CLIENT', 'PRIMOX-TEST-001', 'PRIMOX INSTALLER E2E TEST', datetime('now'));";
        cmd.ExecuteNonQuery();
    }
    Console.WriteLine("seed=ok");
}
else
{
    using var cmd = c.CreateCommand();
    cmd.CommandText = "SELECT ClientName || '|' || VehiclePlate || '|' || WorkOrderNote FROM C08_InstallerMarker WHERE Id=1;";
    var v = cmd.ExecuteScalar()?.ToString() ?? "";
    Console.WriteLine("found=" + v);
}

using (var cmd = c.CreateCommand())
{
    cmd.CommandText = "PRAGMA integrity_check;";
    Console.WriteLine("integrity=" + cmd.ExecuteScalar());
}

return 0;
