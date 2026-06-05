# Migration helpers

Scripts to help migrate data from SQLite to SQL Server.

Requirements:
- `sqlite3` CLI for exporting CSV
- `bcp` for importing CSV into SQL Server (part of SQL Server tools)

Export:

```powershell
.\export_sqlite_to_csv.ps1 -SqliteFile "path\to\database.db" -OutDir "C:\tmp\csv"
```

Import (example using connection string):

```powershell
.\import_csv_to_sqlserver.ps1 -CsvDir "C:\tmp\csv" -ConnectionString "Server=localhost;Database=PrimoAuto;Integrated Security=true;"
```

Notes:
- `bcp` must be able to reach the SQL Server and the target tables must already exist (apply `SqlServerSchema.sql` beforehand).
- For large imports, consider using `BULK INSERT` on the SQL Server side to read files from a location accessible by the server.
