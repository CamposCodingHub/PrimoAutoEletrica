# C1.1.4-35 — Matriz build / testes / artefato / DB

| Item | Resultado |
|------|-----------|
| Branch | cycle-c1/operational-intelligence |
| commit | ea3a1d62ac257e206a1a353a1a088a8a15c1ef06 |
| main local | 29b19b16d0e6e3413bdba20c505e20c992596c24 |
| origin/main | bf1eb784a3ed45782487197f38d9ba15d319997e |
| Unit tests Debug | 506 PASS / 0 FAIL / 0 skip |
| Novos testes C1.1.4 | 17 PASS (Defeitos+Lucro+Clientes harness) |
| Build Debug | 0 erros (warnings CA1416/CS8632 preexistentes) |
| Build Release | 0 erros / 127 avisos |
| Publish | self-contained win-x64 Release (NOVO, nao reusou DLL antiga) |
| Deploy | Scripts\Deploy-ToInstalledApp.ps1 -> %LOCALAPPDATA%\PrimoAutoEletrica\App |
| EXE SHA | 05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B |
| DLL SHA | 886AB11F815C691A5E9A02A08D673BB168E121C9BA6D21CA329932B7D95E4764 |
| C1.1.4-21 identity match publish | True |
| Shortcut TargetPath | C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\PrimoAutoEletrica.exe |
| Shortcut WorkingDirectory | C:\Users\campo\AppData\Local\PrimoAutoEletrica\App |
| Protected DB SHA | C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B |
| Protected DB status | INTACTO (== C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B) |
| Operacional integrity_check | ok |
| Operacional foreign_key_check | vazio (OK) |
| Operacional user_version | 1 |
| Framework | net10.0-windows / included Microsoft.NETCore.App 10.0.10 + WindowsDesktop 10.0.10 |