using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.UserControls;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// FULL ASSURANCE-13 — process residual verification, bulk closure, DB scans, performance samples.
    /// </summary>
    public sealed partial class UiSmokeTestService
    {
        private void RunAssurance13ProcessResidualChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "A13Security:ProcessResidual:SecureLauncherGates", () =>
            {
                AssertThrowsUnauthorized(() => SecureProcessLauncher.OpenUri("cmd.exe"));
                AssertThrowsUnauthorized(() => SecureProcessLauncher.OpenUri("file:///C:/Windows/notepad.exe"));
                AssertThrowsUnauthorized(() => SecureProcessLauncher.OpenUri("powershell.exe"));
                AssertThrowsUnauthorized(() => SecureProcessLauncher.OpenUri("https://evil.example/path;calc.exe"));
                AssertThrowsUnauthorized(() => SecureProcessLauncher.OpenWhatsAppLink("https://evil.example/wa.me/5511999999999"));
                AssertThrowsUnauthorized(() => SecureProcessLauncher.OpenWhatsAppLink("https://wa.me.evil.example/5511"));
                AssertThrowsUnauthorized(() => SecureProcessLauncher.OpenUri("mailto:test@test.local\" & calc.exe"));

                var outside = Path.Combine(
                    Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\",
                    "PrimoAutoEletrica-A13-Canary",
                    Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(outside);
                var canary = Path.Combine(outside, "CANARY_OUTSIDE.txt");
                File.WriteAllText(canary, "x");
                try
                {
                    // Canary fora de AppData/Temp/Documents — deve ser rejeitado.
                    AssertThrowsUnauthorized(() => SecureProcessLauncher.OpenFileOrDirectory(canary, App.RuntimeAppDataPath));
                    AssertThrowsUnauthorized(() =>
                        SecureProcessLauncher.OpenFileOrDirectory(
                            Path.Combine(App.RuntimeAppDataPath, "..", "..", "Windows", "System32", "notepad.exe"),
                            App.RuntimeAppDataPath));
                }
                finally
                {
                    try { Directory.Delete(outside, true); } catch { /* ignore */ }
                }

                // Harmless authorized path with spaces/accents under AppData
                var accentDir = Path.Combine(App.RuntimeAppDataPath, "Exports", "Pasta com espaços e ação");
                Directory.CreateDirectory(accentDir);
                var accentFile = Path.Combine(accentDir, "arquivo-teste.txt");
                File.WriteAllText(accentFile, "qa13");
                // Does not open shell; only validates jail accepts path. Skip actual Process.Start in headless CI by catching FileNotFound after delete? Keep file and call OpenFileOrDirectory — may open explorer briefly in interactive.
                // Prefer validate via PathSecurityHelper without launching:
                PathSecurityHelper.RequireUnderAnyRoot(
                    PathSecurityHelper.NormalizeFullPath(accentFile),
                    PathSecurityHelper.GetDefaultOpenFileRoots());
            });
        }

        private void RunAssurance13BulkDataChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "BulkDataQa13:OperationalModules", () =>
            {
                GarantirBancoIsoladoDoSmoke("bulk data QA13");
                _fixture ??= EnsureSmokeFixture(syntheticUser);

                // Seed base entities if missing (do not rely on prior A12 AppData).
                const int clientesTarget = 500;
                const int veiculosTarget = 500;
                const int produtosTarget = 1000;
                const int osTarget = 1000;
                const int orcTarget = 500;
                const int movTarget = 1000;
                const int finTarget = 1000;
                const int agendaTarget = 500;

                var sw = Stopwatch.StartNew();
                var clienteIds = App.Repositories.Clientes.ObterTodos()
                    .Where(c => c.Nome.StartsWith("QA13_Cliente_", StringComparison.Ordinal)
                             || c.Nome.StartsWith("QA12_Cliente_", StringComparison.Ordinal))
                    .Select(c => c.Id)
                    .ToList();

                if (clienteIds.Count < clientesTarget)
                {
                    var start = clienteIds.Count + 1;
                    for (var i = start; i <= clientesTarget; i++)
                    {
                        var token = $"{DateTime.UtcNow:HHmmss}{i:D5}";
                        var cliente = new Cliente
                        {
                            Nome = $"QA13_Cliente_{i:D4}",
                            CPF = GerarCpfValido(token.PadLeft(16, '0')),
                            Telefone = $"(11) 9{i:D3}-{i:D4}",
                            Email = $"qa13.cliente.{i}@test.local",
                            Observacoes = "QA13 bulk",
                            ConsentimentoLGPD = true,
                            DataConsentimentoLGPD = DateTime.Now,
                            OrigemConsentimentoLGPD = "A13 Bulk",
                            AutorizaContatoWhatsApp = false,
                            DataCadastro = DateTime.Now
                        };
                        App.Repositories.Clientes.Inserir(cliente);
                        clienteIds.Add(cliente.Id);
                    }
                }

                var veiculos = App.Repositories.Clientes.ObterTodosVeiculos()
                    .Where(v => v.Observacoes != null && v.Observacoes.Contains("QA13", StringComparison.Ordinal))
                    .ToList();
                if (veiculos.Count < veiculosTarget)
                {
                    for (var i = veiculos.Count; i < veiculosTarget; i++)
                    {
                        var veiculo = new Veiculo
                        {
                            ClienteId = clienteIds[i % clienteIds.Count],
                            Marca = "QA13",
                            Modelo = $"Modelo_{i:D4}",
                            Ano = "2021",
                            Cor = "Preto",
                            Placa = GerarPlacaUnicaQa12(10_000 + i),
                            Observacoes = "QA13 bulk vehicle"
                        };
                        App.Repositories.Clientes.SalvarVeiculo(veiculo);
                        veiculos.Add(veiculo);
                    }
                }

                var produtos = App.Repositories.Produtos.ObterTodos()
                    .Where(p => p.Codigo != null && (p.Codigo.StartsWith("QA13P", StringComparison.Ordinal)
                                                  || p.Codigo.StartsWith("QA12P", StringComparison.Ordinal)))
                    .ToList();
                var fornecedor = _fixture.Fornecedor ?? CreatePersistedFornecedor();
                if (produtos.Count < produtosTarget)
                {
                    for (var i = produtos.Count + 1; i <= produtosTarget; i++)
                    {
                        var produto = new Produto
                        {
                            Codigo = $"QA13P{i:D5}",
                            Nome = $"QA13_Produto_{i:D5}",
                            Descricao = "QA13 bulk product",
                            Categoria = "Eletrica",
                            Marca = "QA13",
                            Fornecedor = fornecedor.NomeFantasia,
                            CNPJFornecedor = fornecedor.CNPJ,
                            PrecoCompra = 5m + (i % 20),
                            PrecoVenda = 15m + (i % 50),
                            QuantidadeEstoque = 500,
                            QuantidadeMinima = 1,
                            QuantidadeMaxima = 5000,
                            Ativo = true
                        };
                        App.Repositories.Produtos.Inserir(produto);
                        produtos.Add(produto);
                    }
                }

                // Reload products for accurate stock baseline
                produtos = App.Repositories.Produtos.ObterTodos()
                    .Where(p => p.Codigo != null && (p.Codigo.StartsWith("QA13P", StringComparison.Ordinal)
                                                  || p.Codigo.StartsWith("QA12P", StringComparison.Ordinal)))
                    .Take(produtosTarget)
                    .ToList();
                var stockExpected = produtos.ToDictionary(p => p.Id, p => p.QuantidadeEstoque);

                var statusesOs = new[] { "Rascunho", "Aberta", "Em Andamento", "Aguardando Pecas", "Concluida", "Cancelada" };
                var osCreated = 0;
                for (var i = 1; i <= osTarget; i++)
                {
                    var clienteId = clienteIds[(i - 1) % clienteIds.Count];
                    var veiculo = veiculos[(i - 1) % veiculos.Count];
                    var produto = produtos[(i - 1) % produtos.Count];
                    var cliente = App.Repositories.Clientes.ObterPorId(clienteId) ?? new Cliente { Id = clienteId, Nome = $"QA13_Cliente" };
                    var ordem = new OrdemServico
                    {
                        Numero = App.Repositories.OrdensServico.GerarProximoNumero(),
                        ClienteId = clienteId,
                        VeiculoId = veiculo.Id,
                        ClienteNomeSnapshot = cliente.Nome,
                        TelefoneClienteSnapshot = cliente.Telefone,
                        VeiculoDescricaoSnapshot = $"{veiculo.Marca} {veiculo.Modelo}",
                        PlacaSnapshot = veiculo.Placa,
                        Status = statusesOs[i % statusesOs.Length],
                        Prioridade = i % 3 == 0 ? "Alta" : "Normal",
                        Origem = "QA13Bulk",
                        ProblemaRelatado = $"QA13_OS_{i:D4}",
                        ObservacoesInternas = "QA13 bulk OS",
                        DataAbertura = DateTime.Now.AddDays(-(i % 60)),
                        Ativo = true,
                        Itens = new List<OrdemServicoItem>
                        {
                            new()
                            {
                                ProdutoId = produto.Id,
                                Tipo = "Peca",
                                Descricao = produto.Nome,
                                Quantidade = 1,
                                ValorUnitario = produto.PrecoVenda,
                                CustoUnitario = produto.PrecoCompra,
                                Observacoes = "QA13"
                            }
                        }
                    };
                    App.Repositories.OrdensServico.Inserir(ordem);
                    osCreated++;
                }

                var orcService = new OrcamentoDatabaseService();
                var orcStatuses = new[] { "Rascunho", "Enviado", "Aprovado", "Rejeitado", "Cancelado" };
                var orcCreated = 0;
                for (var i = 1; i <= orcTarget; i++)
                {
                    var clienteId = clienteIds[(i - 1) % clienteIds.Count];
                    var veiculo = veiculos[(i - 1) % veiculos.Count];
                    var produto = produtos[(i - 1) % produtos.Count];
                    var cliente = App.Repositories.Clientes.ObterPorId(clienteId) ?? new Cliente { Id = clienteId, Nome = "QA13" };
                    var orcamentoId = Guid.NewGuid();
                    var item = new OrcamentoItem
                    {
                        Id = Guid.NewGuid(),
                        OrcamentoId = orcamentoId,
                        ProdutoId = produto.Id,
                        ProdutoNome = produto.Nome,
                        ProdutoCodigo = produto.Codigo,
                        Quantidade = 1,
                        PrecoUnitario = produto.PrecoVenda,
                        PrecoCusto = produto.PrecoCompra,
                        Subtotal = produto.PrecoVenda,
                        Observacoes = "QA13"
                    };
                    var orcamento = new Orcamento
                    {
                        Id = orcamentoId,
                        ClienteId = clienteId,
                        VeiculoId = veiculo.Id,
                        Cliente = cliente,
                        Veiculo = veiculo,
                        Numero = orcService.GerarNumeroOrcamento(),
                        Status = orcStatuses[i % orcStatuses.Length],
                        DataCriacao = DateTime.Now.AddDays(-(i % 40)),
                        DataValidade = DateTime.Today.AddDays(7),
                        Subtotal = item.Subtotal,
                        Total = item.Subtotal,
                        Observacoes = $"QA13_ORC_{i:D4}",
                        Itens = new List<OrcamentoItem> { item }
                    };
                    orcService.AdicionarOrcamento(orcamento);
                    orcCreated++;
                }

                var estoque = new EstoqueOperationalService(App.Database, _logger);
                var movCreated = 0;
                for (var i = 1; i <= movTarget; i++)
                {
                    var produto = produtos[(i - 1) % produtos.Count];
                    var isEntrada = i % 2 == 0;
                    var qty = 1 + (i % 3);
                    estoque.RegistrarMovimentacaoManual(
                        produto.Id,
                        qty,
                        isEntrada ? "Entrada" : "Saida",
                        $"QA13 mov {i}",
                        "QA13Bulk",
                        permitirDisponivelNegativo: true);
                    stockExpected[produto.Id] = stockExpected[produto.Id] + (isEntrada ? qty : -qty);
                    movCreated++;
                }

                // Independent stock verification
                var produtosAfter = App.Repositories.Produtos.ObterTodos()
                    .Where(p => stockExpected.ContainsKey(p.Id))
                    .ToList();
                foreach (var p in produtosAfter)
                {
                    if (p.QuantidadeEstoque != stockExpected[p.Id])
                    {
                        throw new InvalidOperationException(
                            $"Saldo estoque divergente produto {p.Codigo}: esperado={stockExpected[p.Id]} atual={p.QuantidadeEstoque}");
                    }
                }

                var financeiro = new FinanceiroDatabaseService();
                var finCreated = 0;
                decimal expectedReceitas = 0;
                decimal expectedDespesas = 0;
                for (var i = 1; i <= finTarget; i++)
                {
                    var valor = 10m + (i % 90);
                    if (i % 2 == 0)
                    {
                        // Status suportados: Pendente, Pago, Parcial, Cancelado
                        var status = i % 5 == 0 ? "Pago" : (i % 11 == 0 ? "Parcial" : "Pendente");
                        financeiro.AdicionarContaReceber(
                            cliente: $"QA13_Cliente_{(i % clientesTarget) + 1:D4}",
                            descricao: $"QA13_REC_{i:D4}",
                            valor: valor,
                            dataVencimento: DateTime.Today.AddDays(-(i % 30)),
                            formaPagamento: "PIX",
                            observacoes: "QA13 bulk",
                            status: status,
                            dataPagamento: status == "Pago" ? DateTime.Today : null,
                            origem: "QA13Bulk",
                            referenciaExterna: $"QA13-REC-{i}");
                        expectedReceitas += valor;
                    }
                    else
                    {
                        // ContaPagar API nao expoe status na assinatura publica; cria pendente via origem/referencia.
                        financeiro.AdicionarContaPagar(
                            fornecedor: "QA13 Fornecedor",
                            descricao: $"QA13_PAG_{i:D4}",
                            valor: valor,
                            dataVencimento: DateTime.Today.AddDays(i % 45),
                            categoria: "QA13",
                            observacoes: "QA13 bulk",
                            origem: "QA13Bulk",
                            referenciaExterna: $"QA13-PAG-{i}");
                        expectedDespesas += valor;
                    }

                    finCreated++;
                }

                var agendaStatuses = new[] { "Confirmado", "Pendente", "Cancelado", "Concluido" };
                var agendaCreated = 0;
                for (var i = 1; i <= agendaTarget; i++)
                {
                    var clienteId = clienteIds[(i - 1) % clienteIds.Count];
                    var veiculo = veiculos[(i - 1) % veiculos.Count];
                    var produto = produtos[(i - 1) % produtos.Count];
                    var cliente = App.Repositories.Clientes.ObterPorId(clienteId) ?? new Cliente { Id = clienteId, Nome = "QA13" };
                    // Dia unico por indice evita conflito de horario (fixture usa 10h-11h fixos).
                    CreatePersistedAgendamento(
                        cliente,
                        veiculo,
                        produto,
                        dataAgendamento: DateTime.Today.AddDays(i),
                        status: agendaStatuses[i % agendaStatuses.Length],
                        prioridade: i % 4 == 0 ? "Alta" : "Normal",
                        numeroPrefixo: "QA13");
                    agendaCreated++;
                }

                sw.Stop();

                RunDatabaseIntegrityAssert("apos BulkDataQa13");
                RunOrphanAndDuplicateScans(out var orphanCount, out var suspiciousDupes);
                if (orphanCount > 0)
                {
                    throw new InvalidOperationException($"Orphan scan encontrou {orphanCount} registros.");
                }

                _logger.LogInfo(
                    $"A13 bulk: os={osCreated} orc={orcCreated} mov={movCreated} fin={finCreated} agenda={agendaCreated} " +
                    $"receitasEsp={expectedReceitas:F2} despesasEsp={expectedDespesas:F2} orphans={orphanCount} " +
                    $"suspiciousDupes={suspiciousDupes} ms={sw.ElapsedMilliseconds}");

                if (osCreated < osTarget || orcCreated < orcTarget || movCreated < movTarget
                    || finCreated < finTarget || agendaCreated < agendaTarget)
                {
                    throw new InvalidOperationException("Bulk QA13 incompleto.");
                }
            });
        }

        private void RunAssurance13DatabaseScanChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "A13Database:IntegrityOrphanDuplicate", () =>
            {
                GarantirBancoIsoladoDoSmoke("A13 database scans");
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                RunDatabaseIntegrityAssert("A13Database");
                RunOrphanAndDuplicateScans(out var orphans, out var suspicious);
                if (orphans > 0)
                {
                    throw new InvalidOperationException($"Orphans={orphans}");
                }

                // Write scan evidence under RuntimeLogDirectory
                var dir = Path.Combine(App.RuntimeLogDirectory, "a13-db-scans");
                Directory.CreateDirectory(dir);
                File.WriteAllText(
                    Path.Combine(dir, "orphan-duplicate-summary.txt"),
                    $"orphans={orphans}; suspiciousDuplicates={suspicious}; generated={DateTime.Now:o}",
                    Encoding.UTF8);
                File.WriteAllText(
                    Path.Combine(dir, "orphans.csv"),
                    "relation,count\n" +
                    "total_orphans," + orphans + "\n" +
                    "suspicious_business_key_duplicates," + suspicious + "\n",
                    Encoding.UTF8);
            });
        }

        private void RunAssurance13PerformanceChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "A13Performance:ProcessCountersSample", () =>
            {
                using var proc = Process.GetCurrentProcess();
                proc.Refresh();
                var ramBefore = proc.WorkingSet64;
                var handlesBefore = proc.HandleCount;
                var cpuBefore = proc.TotalProcessorTime;

                // Light navigation pressure: open/close core controls
                for (var i = 0; i < 5; i++)
                {
                    var host = CreateHostWindow(new DashboardControl(), "Dashboard");
                    try
                    {
                        ShowWindowForInteraction(host);
                        WaitForUiIdle();
                    }
                    finally
                    {
                        if (host.IsVisible) host.Close();
                    }
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                proc.Refresh();
                var ramAfter = proc.WorkingSet64;
                var handlesAfter = proc.HandleCount;
                var cpuAfter = proc.TotalProcessorTime;

                var dir = Path.Combine(App.RuntimeLogDirectory, "a13-performance");
                Directory.CreateDirectory(dir);
                File.WriteAllText(
                    Path.Combine(dir, "sample.txt"),
                    $"ramBefore={ramBefore};ramAfter={ramAfter};handlesBefore={handlesBefore};handlesAfter={handlesAfter};" +
                    $"cpuBeforeMs={cpuBefore.TotalMilliseconds:F0};cpuAfterMs={cpuAfter.TotalMilliseconds:F0};at={DateTime.Now:o}",
                    Encoding.UTF8);

                // Classification: temporary growth after UI open/close is expected; flag only extreme handle growth.
                if (handlesAfter - handlesBefore > 5000)
                {
                    throw new InvalidOperationException(
                        $"Handle growth REVIEW/POTENTIAL LEAK: before={handlesBefore} after={handlesAfter}");
                }

                _logger.LogInfo(
                    $"A13 perf sample: ram {ramBefore}->{ramAfter} handles {handlesBefore}->{handlesAfter}");
            });
        }

        private void RunAssurance13ConcurrencyChecks(UiSmokeTestRunResult result, Funcionario syntheticUser)
        {
            RunCheck(result, "A13Concurrency:DoubleSaveIdempotency", () =>
            {
                GarantirBancoIsoladoDoSmoke("A13 concurrency");
                _fixture ??= EnsureSmokeFixture(syntheticUser);
                var financeiro = new FinanceiroDatabaseService();
                var referencia = $"QA13-CONC-{Guid.NewGuid():N}";
                Exception? secondError = null;

                financeiro.AdicionarContaReceber(
                    cliente: "QA13_Concurrency",
                    descricao: "QA13_CONC_REC",
                    valor: 33.33m,
                    dataVencimento: DateTime.Today.AddDays(3),
                    formaPagamento: "PIX",
                    observacoes: "QA13 concurrency",
                    status: "Pendente",
                    origem: "QA13Conc",
                    referenciaExterna: referencia);

                try
                {
                    // Segunda gravação com mesma origem+referencia — deve ser upsert/idempotente ou rejeitar sem corromper.
                    financeiro.AdicionarContaReceber(
                        cliente: "QA13_Concurrency",
                        descricao: "QA13_CONC_REC_DUP",
                        valor: 33.33m,
                        dataVencimento: DateTime.Today.AddDays(3),
                        formaPagamento: "PIX",
                        observacoes: "QA13 concurrency dup",
                        status: "Pendente",
                        origem: "QA13Conc",
                        referenciaExterna: referencia);
                }
                catch (Exception ex)
                {
                    secondError = ex;
                }

                RunDatabaseIntegrityAssert("A13Concurrency");
                using var connection = App.Database.GetSqliteConnection();
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = @"
SELECT COUNT(1) FROM ContasReceber
WHERE Origem = @o AND ReferenciaExterna = @r;";
                cmd.Parameters.AddWithValue("@o", "QA13Conc");
                cmd.Parameters.AddWithValue("@r", referencia);
                var count = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                if (count != 1)
                {
                    throw new InvalidOperationException(
                        $"Double-save produziu {count} registros (esperado 1). secondError={secondError?.GetType().Name}");
                }

                _logger.LogInfo($"A13 concurrency ok: referencia={referencia}; secondError={secondError?.GetType().Name ?? "none"}");
            });
        }

        private static void AssertThrowsUnauthorized(Action action)
        {
            try
            {
                action();
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
            catch (ArgumentException)
            {
                return;
            }

            throw new InvalidOperationException("Esperava rejeicao de seguranca (Unauthorized/Argument).");
        }

        private void RunDatabaseIntegrityAssert(string context)
        {
            using var connection = App.Database.GetSqliteConnection();
            connection.Open();
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA integrity_check;";
                var integrity = Convert.ToString(cmd.ExecuteScalar()) ?? string.Empty;
                if (!string.Equals(integrity, "ok", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"integrity_check ({context}): {integrity}");
                }
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA foreign_key_check;";
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    throw new InvalidOperationException($"foreign_key_check ({context}) retornou violacoes.");
                }
            }
        }

        private void RunOrphanAndDuplicateScans(out int orphanCount, out int suspiciousDuplicates)
        {
            orphanCount = 0;
            suspiciousDuplicates = 0;
            using var connection = App.Database.GetSqliteConnection();
            connection.Open();

            // Discover table existence first
            var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table';";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }
            }

            int CountSql(string sql)
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                var scalar = cmd.ExecuteScalar();
                return Convert.ToInt32(scalar ?? 0);
            }

            if (tables.Contains("Veiculos") && tables.Contains("Clientes"))
            {
                orphanCount += CountSql(@"
SELECT COUNT(1) FROM Veiculos v
LEFT JOIN Clientes c ON c.Id = v.ClienteId
WHERE c.Id IS NULL;");
            }

            if (tables.Contains("OrdensServico") && tables.Contains("Clientes"))
            {
                orphanCount += CountSql(@"
SELECT COUNT(1) FROM OrdensServico o
LEFT JOIN Clientes c ON c.Id = o.ClienteId
WHERE o.ClienteId IS NOT NULL AND c.Id IS NULL;");
            }

            if (tables.Contains("OrdensServico") && tables.Contains("Veiculos"))
            {
                orphanCount += CountSql(@"
SELECT COUNT(1) FROM OrdensServico o
LEFT JOIN Veiculos v ON v.Id = o.VeiculoId
WHERE o.VeiculoId IS NOT NULL AND v.Id IS NULL;");
            }

            if (tables.Contains("Orcamentos") && tables.Contains("Clientes"))
            {
                orphanCount += CountSql(@"
SELECT COUNT(1) FROM Orcamentos o
LEFT JOIN Clientes c ON c.Id = o.ClienteId
WHERE o.ClienteId IS NOT NULL AND c.Id IS NULL;");
            }

            if (tables.Contains("Agendamentos") && tables.Contains("Clientes"))
            {
                orphanCount += CountSql(@"
SELECT COUNT(1) FROM Agendamentos a
LEFT JOIN Clientes c ON c.Id = a.ClienteId
WHERE a.ClienteId IS NOT NULL AND c.Id IS NULL;");
            }

            // Suspicious duplicates: same CPF on different active clients (business key)
            if (tables.Contains("Clientes"))
            {
                suspiciousDuplicates += CountSql(@"
SELECT COUNT(1) FROM (
  SELECT CPF FROM Clientes
  WHERE CPF IS NOT NULL AND TRIM(CPF) <> ''
  GROUP BY CPF
  HAVING COUNT(1) > 1
);");
            }

            if (tables.Contains("Veiculos"))
            {
                suspiciousDuplicates += CountSql(@"
SELECT COUNT(1) FROM (
  SELECT Placa FROM Veiculos
  WHERE Placa IS NOT NULL AND TRIM(Placa) <> ''
  GROUP BY UPPER(Placa)
  HAVING COUNT(1) > 1
);");
            }

            if (tables.Contains("Produtos"))
            {
                suspiciousDuplicates += CountSql(@"
SELECT COUNT(1) FROM (
  SELECT Codigo FROM Produtos
  WHERE Codigo IS NOT NULL AND TRIM(Codigo) <> ''
  GROUP BY UPPER(Codigo)
  HAVING COUNT(1) > 1
);");
            }
        }
    }
}
