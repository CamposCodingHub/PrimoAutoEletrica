using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// FULL ASSURANCE-12 — authorization / path / bulk data red-team harnesses (isolated AppData only).
    /// </summary>
    public sealed partial class UiSmokeTestService
    {
        private void RunAssurance12SecurityChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "A12Security:Authorization:BackupRestoreServiceGate", () =>
            {
                GarantirBancoIsoladoDoSmoke("authorization backup/restore");
                var operador = CreateSyntheticUser("Vendedor", "OPERATOR_TEST");
                var user = CreateSyntheticUser("Tecnico", "USER_TEST");
                var adminPerms = new PermissionService(syntheticUser, _logger, App.Database);
                var operatorPerms = new PermissionService(operador, _logger, App.Database);
                var userPerms = new PermissionService(user, _logger, App.Database);

                if (!adminPerms.TemPermissaoCodigo("SISTEMA_CONFIGURAR"))
                {
                    throw new InvalidOperationException("ADMIN_TEST (Administrador) deveria ter SISTEMA_CONFIGURAR.");
                }

                if (operatorPerms.TemPermissaoCodigo("SISTEMA_CONFIGURAR") ||
                    userPerms.TemPermissaoCodigo("SISTEMA_CONFIGURAR"))
                {
                    throw new InvalidOperationException("OPERATOR/USER de teste nao deveriam ter SISTEMA_CONFIGURAR.");
                }

                var deniedOperator = false;
                try
                {
                    App.Backups.CriarBackupManualAuthorized(null, operador, operatorPerms);
                }
                catch (UnauthorizedAccessException)
                {
                    deniedOperator = true;
                }

                if (!deniedOperator)
                {
                    throw new InvalidOperationException("OPERATOR_TEST conseguiu CriarBackupManualAuthorized sem SISTEMA_CONFIGURAR.");
                }

                var deniedUser = false;
                try
                {
                    App.Backups.RestaurarBackupAuthorized(
                        Path.Combine(App.RuntimeBackupDirectory, "nao-existe.db"),
                        user,
                        userPerms,
                        criarBackupSeguranca: false);
                }
                catch (UnauthorizedAccessException)
                {
                    deniedUser = true;
                }

                if (!deniedUser)
                {
                    throw new InvalidOperationException("USER_TEST conseguiu RestaurarBackupAuthorized sem SISTEMA_CONFIGURAR.");
                }

                // Positive path: admin can create backup under authorized root.
                var backup = App.Backups.CriarBackupManualAuthorized(null, syntheticUser, adminPerms);
                if (string.IsNullOrWhiteSpace(backup) || !File.Exists(backup))
                {
                    throw new InvalidOperationException("ADMIN_TEST nao conseguiu criar backup autorizado.");
                }

                if (!PathSecurityHelper.IsUnderRoot(backup, App.RuntimeAppDataPath) &&
                    !PathSecurityHelper.IsUnderRoot(backup, App.RuntimeBackupDirectory))
                {
                    throw new InvalidOperationException("Backup autorizado foi gravado fora dos roots esperados.");
                }
            });

            RunCheck(result, "A12Security:PathTraversal:CanaryReject", () =>
            {
                GarantirBancoIsoladoDoSmoke("path traversal canary");
                var sandbox = Path.Combine(App.RuntimeAppDataPath, "PRIMOX_SECURITY_SANDBOX");
                var outside = Path.Combine(Path.GetTempPath(), $"PRIMOX_SECURITY_OUTSIDE-{Guid.NewGuid():N}");
                Directory.CreateDirectory(sandbox);
                Directory.CreateDirectory(outside);
                File.WriteAllText(Path.Combine(sandbox, "CANARY_INSIDE.txt"), "INSIDE");
                var outsideCanary = Path.Combine(outside, "CANARY_OUTSIDE.txt");
                File.WriteAllText(outsideCanary, "OUTSIDE");

                var escapes = new[]
                {
                    Path.Combine(sandbox, "..", Path.GetFileName(outside), "CANARY_OUTSIDE.txt"),
                    Path.Combine(sandbox, "..\\..\\", Path.GetFileName(outside), "CANARY_OUTSIDE.txt"),
                    outsideCanary,
                    Path.GetFullPath(Path.Combine(sandbox, "../", Path.GetFileName(outside), "CANARY_OUTSIDE.txt"))
                };

                foreach (var candidate in escapes)
                {
                    var rejected = false;
                    try
                    {
                        App.Backups.RestaurarBackup(candidate, criarBackupSeguranca: false);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        rejected = true;
                    }
                    catch (InvalidOperationException)
                    {
                        // Path may pass jail only if under RuntimeAppData — outside temp must be Unauthorized.
                        // Invalid backup after jail means path was allowed incorrectly if candidate resolves outside appdata.
                        if (!PathSecurityHelper.IsUnderRoot(candidate, App.RuntimeAppDataPath))
                        {
                            throw new InvalidOperationException($"Path fora do AppData nao rejeitado por Unauthorized: {candidate}");
                        }

                        rejected = true;
                    }

                    if (!rejected)
                    {
                        throw new InvalidOperationException($"Path traversal nao rejeitado: {candidate}");
                    }
                }

                try
                {
                    Directory.Delete(outside, recursive: true);
                }
                catch
                {
                    // ignore
                }
            });

            RunCheck(result, "A12Security:ProcessSecurity:UriSchemeGate", () =>
            {
                var rejected = false;
                try
                {
                    SecureProcessLauncher.OpenUri("file:///C:/Windows/notepad.exe");
                }
                catch (UnauthorizedAccessException)
                {
                    rejected = true;
                }

                if (!rejected)
                {
                    throw new InvalidOperationException("SecureProcessLauncher aceitou esquema file://.");
                }

                rejected = false;
                try
                {
                    SecureProcessLauncher.OpenWhatsAppLink("https://evil.example/phishing");
                }
                catch (UnauthorizedAccessException)
                {
                    rejected = true;
                }

                if (!rejected)
                {
                    throw new InvalidOperationException("SecureProcessLauncher aceitou link WhatsApp invalido.");
                }
            });
        }

        private void RunAssurance12BulkDataChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "BulkDataQa12:SeedAndSearch", () =>
            {
                GarantirBancoIsoladoDoSmoke("bulk data QA12");
                _fixture ??= EnsureSmokeFixture(syntheticUser);

                const int clientesTarget = 500;
                const int produtosTarget = 1000;
                const int veiculosTarget = 500;

                var sw = Stopwatch.StartNew();
                var clienteIds = new List<Guid>(clientesTarget);
                for (var i = 1; i <= clientesTarget; i++)
                {
                    var token = $"{DateTime.UtcNow:HHmmss}{i:D5}";
                    var cliente = new Cliente
                    {
                        Nome = $"QA12_Cliente_{i:D4}",
                        CPF = GerarCpfValido(token.PadLeft(16, '0')),
                        Telefone = $"(11) 9{i:D3}-{i:D4}",
                        Email = $"qa12.cliente.{i}@test.local",
                        Observacoes = "QA12 bulk",
                        ConsentimentoLGPD = true,
                        DataConsentimentoLGPD = DateTime.Now,
                        OrigemConsentimentoLGPD = "A12 Bulk",
                        AutorizaContatoWhatsApp = false,
                        DataCadastro = DateTime.Now
                    };
                    App.Repositories.Clientes.Inserir(cliente);
                    clienteIds.Add(cliente.Id);
                }

                for (var i = 0; i < veiculosTarget; i++)
                {
                    var clienteId = clienteIds[i % clienteIds.Count];
                    var veiculo = new Veiculo
                    {
                        ClienteId = clienteId,
                        Marca = "QA12",
                        Modelo = $"Modelo_{i:D4}",
                        Ano = "2020",
                        Cor = "Prata",
                        Placa = GerarPlacaUnicaQa12(i),
                        Observacoes = "QA12 bulk vehicle"
                    };
                    App.Repositories.Clientes.SalvarVeiculo(veiculo);
                }

                var fornecedor = _fixture.Fornecedor ?? CreatePersistedFornecedor();
                for (var i = 1; i <= produtosTarget; i++)
                {
                    var produto = new Produto
                    {
                        Codigo = $"QA12P{i:D5}",
                        Nome = $"QA12_Produto_{i:D5}",
                        Descricao = "QA12 bulk product",
                        Categoria = "Eletrica",
                        Marca = "QA12",
                        Fornecedor = fornecedor.NomeFantasia,
                        CNPJFornecedor = fornecedor.CNPJ,
                        PrecoCompra = 5m + (i % 20),
                        PrecoVenda = 15m + (i % 50),
                        QuantidadeEstoque = 100,
                        QuantidadeMinima = 1,
                        QuantidadeMaxima = 500,
                        Ativo = true
                    };
                    App.Repositories.Produtos.Inserir(produto);
                }

                sw.Stop();

                var encontrados = App.Repositories.Clientes.ObterTodos()
                    .Count(c => c.Nome.StartsWith("QA12_Cliente_", StringComparison.Ordinal));
                if (encontrados < clientesTarget)
                {
                    throw new InvalidOperationException(
                        $"Bulk clientes incompleto: esperados>={clientesTarget}, busca={encontrados}");
                }

                var produtos = App.Repositories.Produtos.ObterTodos()
                    .Count(p => p.Codigo != null && p.Codigo.StartsWith("QA12P", StringComparison.Ordinal));
                if (produtos < produtosTarget)
                {
                    throw new InvalidOperationException(
                        $"Bulk produtos incompleto: esperados>={produtosTarget}, encontrados={produtos}");
                }

                // Integrity after bulk
                using var connection = App.Database.GetSqliteConnection();
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA integrity_check;";
                    var integrity = Convert.ToString(cmd.ExecuteScalar()) ?? string.Empty;
                    if (!string.Equals(integrity, "ok", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"integrity_check apos bulk: {integrity}");
                    }
                }

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA foreign_key_check;";
                    using var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        throw new InvalidOperationException("foreign_key_check retornou violacoes apos bulk QA12.");
                    }
                }

                _logger.LogInfo(
                    $"A12 bulk concluido: clientes={clientesTarget}, veiculos={veiculosTarget}, produtos={produtosTarget}, ms={sw.ElapsedMilliseconds}");
            });
        }

        private static string GerarPlacaUnicaQa12(int index)
        {
            // Mercosul-like LLLDLDD unique for index (supports >> 500 without collision).
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var n = Math.Abs(index);
            var d2 = n % 10; n /= 10;
            var d1 = n % 10; n /= 10;
            var l3 = letters[n % 26]; n /= 26;
            var d0 = n % 10; n /= 10;
            var l2 = letters[n % 26]; n /= 26;
            var l1 = letters[n % 26]; n /= 26;
            var l0 = letters[n % 26];
            return $"{l0}{l1}{l2}{d0}{l3}{d1}{d2}";
        }
    }
}
