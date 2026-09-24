using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public interface IDiagnosticoTecnicoService
    {
        DiagnosticoTecnico SalvarDiagnostico(DiagnosticoTecnico diagnostico);
        DiagnosticoTecnico? ObterPorId(Guid id);
        IReadOnlyList<DiagnosticoTecnico> ObterPorOrdemServicoId(Guid ordemServicoId);
        IReadOnlyList<DiagnosticoTecnico> ObterHistoricoPorVeiculoId(Guid veiculoId);
        DiagnosticoTecnico? RegistrarMedicaoPosReparo(Guid diagnosticoId, Guid medicaoId, decimal valorPosReparo, MedicaoResultadoEnum? novoResultado = null, string? observacao = null);
        DiagnosticoTecnico? ConcluirDiagnostico(Guid diagnosticoId, string? laudoFinal = null, string? correcao = null, CausaStatusEnum? statusCausa = null);
        int ImportarLegado(string? caminhoJson = null);
        IReadOnlyList<DiagnosticoTecnico> ObterTodos();
    }

    /// <summary>
    /// Servico de gerenciamento pericial de diagnosticos tecnicos de autoeletricidade.
    /// Garante associacao estrita por OrdemServicoId e VeiculoId, suporte a medicoes objetivas
    /// com limites 12V/24V e ciclo completo antes vs depois (teste pos-reparo).
    /// </summary>
    public sealed class DiagnosticoTecnicoService : IDiagnosticoTecnicoService
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly object _lock = new();
        private readonly string? _instanceStorageDirectory;

        /// <summary>
        /// Diretorio customizado exclusivo para testes automatizados isolados.
        /// Nao deve ser usado em runtime de producao.
        /// </summary>
        public static string? TestStorageDirectoryOverride { get; set; }

        public DiagnosticoTecnicoService(string? storageDirectoryOverride = null)
        {
            _instanceStorageDirectory = storageDirectoryOverride;
        }

        private string ObterDiretorioArmazenamento()
        {
            var baseDir = _instanceStorageDirectory 
                          ?? TestStorageDirectoryOverride 
                          ?? Path.Combine(App.RuntimeAppDataPath, "AutoEletrica", "diagnosticos");
            Directory.CreateDirectory(baseDir);
            return baseDir;
        }

        private string ObterCaminhoArquivo(Guid diagnosticoId) =>
            Path.Combine(ObterDiretorioArmazenamento(), $"{diagnosticoId:N}.json");

        public DiagnosticoTecnico SalvarDiagnostico(DiagnosticoTecnico diagnostico)
        {
            ArgumentNullException.ThrowIfNull(diagnostico);

            if (diagnostico.Id == Guid.Empty)
            {
                diagnostico.Id = Guid.NewGuid();
            }

            if (!diagnostico.OrigemLegado)
            {
                if (diagnostico.OrdemServicoId == Guid.Empty)
                {
                    throw new ArgumentException("O diagnostico tecnico precisa estar vinculado a uma Ordem de Servico valida (OrdemServicoId obrigatorio).", nameof(diagnostico.OrdemServicoId));
                }

                if (diagnostico.VeiculoId == Guid.Empty)
                {
                    throw new ArgumentException("O diagnostico tecnico precisa estar vinculado a um Veiculo valido (VeiculoId obrigatorio).", nameof(diagnostico.VeiculoId));
                }
            }

            // Normalizar e assegurar IDs nas medicoes filhas
            foreach (var medicao in diagnostico.Medicoes ?? Enumerable.Empty<DiagnosticoMedicao>())
            {
                if (medicao.Id == Guid.Empty)
                {
                    medicao.Id = Guid.NewGuid();
                }
                medicao.DiagnosticoId = diagnostico.Id;
            }

            lock (_lock)
            {
                var caminho = ObterCaminhoArquivo(diagnostico.Id);
                var json = JsonSerializer.Serialize(diagnostico, JsonOpts);
                File.WriteAllText(caminho, json, Encoding.UTF8);
            }

            return diagnostico;
        }

        public DiagnosticoTecnico? ObterPorId(Guid id)
        {
            if (id == Guid.Empty) return null;

            lock (_lock)
            {
                var caminho = ObterCaminhoArquivo(id);
                if (!File.Exists(caminho)) return null;

                try
                {
                    var json = File.ReadAllText(caminho, Encoding.UTF8);
                    return JsonSerializer.Deserialize<DiagnosticoTecnico>(json, JsonOpts);
                }
                catch
                {
                    return null;
                }
            }
        }

        public IReadOnlyList<DiagnosticoTecnico> ObterPorOrdemServicoId(Guid ordemServicoId)
        {
            if (ordemServicoId == Guid.Empty) return Array.Empty<DiagnosticoTecnico>();

            return ObterTodos()
                .Where(d => d.OrdemServicoId == ordemServicoId)
                .OrderByDescending(d => d.DataHora)
                .ToList();
        }

        public IReadOnlyList<DiagnosticoTecnico> ObterHistoricoPorVeiculoId(Guid veiculoId)
        {
            if (veiculoId == Guid.Empty) return Array.Empty<DiagnosticoTecnico>();

            return ObterTodos()
                .Where(d => d.VeiculoId == veiculoId)
                .OrderByDescending(d => d.DataHora)
                .ToList();
        }

        public DiagnosticoTecnico? RegistrarMedicaoPosReparo(
            Guid diagnosticoId,
            Guid medicaoId,
            decimal valorPosReparo,
            MedicaoResultadoEnum? novoResultado = null,
            string? observacao = null)
        {
            var diag = ObterPorId(diagnosticoId)
                ?? throw new InvalidOperationException($"Diagnostico tecnico '{diagnosticoId}' nao encontrado.");

            var medicao = diag.Medicoes.FirstOrDefault(m => m.Id == medicaoId)
                ?? throw new InvalidOperationException($"Medicao '{medicaoId}' nao encontrada no diagnostico '{diagnosticoId}'.");

            medicao.ValorPosReparo = valorPosReparo;
            if (novoResultado.HasValue)
            {
                medicao.Resultado = novoResultado.Value;
            }
            else
            {
                // Se o valor pos-reparo esta dentro dos limites de referencia, marcar como NORMAL automaticamente
                if (medicao.ValorReferenciaMin.HasValue && valorPosReparo < medicao.ValorReferenciaMin.Value)
                {
                    medicao.Resultado = MedicaoResultadoEnum.FORA_DO_ESPERADO;
                }
                else if (medicao.ValorReferenciaMax.HasValue && valorPosReparo > medicao.ValorReferenciaMax.Value)
                {
                    medicao.Resultado = MedicaoResultadoEnum.FORA_DO_ESPERADO;
                }
                else
                {
                    medicao.Resultado = MedicaoResultadoEnum.NORMAL;
                }
            }

            if (!string.IsNullOrWhiteSpace(observacao))
            {
                medicao.Observacao = string.IsNullOrWhiteSpace(medicao.Observacao)
                    ? observacao
                    : $"{medicao.Observacao} | Pós-reparo: {observacao}";
            }

            SalvarDiagnostico(diag);
            return diag;
        }

        public DiagnosticoTecnico? ConcluirDiagnostico(
            Guid diagnosticoId,
            string? laudoFinal = null,
            string? correcao = null,
            CausaStatusEnum? statusCausa = null)
        {
            var diag = ObterPorId(diagnosticoId)
                ?? throw new InvalidOperationException($"Diagnostico tecnico '{diagnosticoId}' nao encontrado.");

            diag.Status = DiagnosticoStatusEnum.Concluido;
            diag.DataConclusao = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(laudoFinal))
            {
                diag.DiagnosticoLaudo = laudoFinal.Trim();
            }

            if (!string.IsNullOrWhiteSpace(correcao))
            {
                diag.CorrecaoExecutada = correcao.Trim();
            }

            if (statusCausa.HasValue)
            {
                diag.CausaStatus = statusCausa.Value;
            }

            SalvarDiagnostico(diag);
            return diag;
        }

        public IReadOnlyList<DiagnosticoTecnico> ObterTodos()
        {
            var dir = ObterDiretorioArmazenamento();
            if (!Directory.Exists(dir)) return Array.Empty<DiagnosticoTecnico>();

            var lista = new List<DiagnosticoTecnico>();
            lock (_lock)
            {
                foreach (var arquivo in Directory.EnumerateFiles(dir, "*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(arquivo, Encoding.UTF8);
                        var item = JsonSerializer.Deserialize<DiagnosticoTecnico>(json, JsonOpts);
                        if (item != null)
                        {
                            lista.Add(item);
                        }
                    }
                    catch
                    {
                        // ignora arquivo corrompido isolado
                    }
                }
            }

            return lista.OrderByDescending(d => d.DataHora).ToList();
        }

        public int ImportarLegado(string? caminhoJson = null)
        {
            var path = caminhoJson;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = Path.Combine(App.RuntimeAppDataPath, "AutoEletrica", "roteiros-resultados.json");
            }

            if (!File.Exists(path)) return 0;

            try
            {
                var conteudo = File.ReadAllText(path, Encoding.UTF8);
                var listaLegada = JsonSerializer.Deserialize<List<AutoEletricaRoteiroPersistencia>>(conteudo);
                if (listaLegada == null || listaLegada.Count == 0) return 0;

                var existentes = ObterTodos().ToDictionary(x => x.RoteiroCodigo, StringComparer.OrdinalIgnoreCase);
                var importados = 0;

                foreach (var legado in listaLegada)
                {
                    if (string.IsNullOrWhiteSpace(legado.Codigo)) continue;

                    // Se ja foi importado, nao duplica
                    if (existentes.ContainsKey(legado.Codigo)) continue;

                    var novo = new DiagnosticoTecnico
                    {
                        Id = Guid.NewGuid(),
                        OrdemServicoId = Guid.Empty,
                        VeiculoId = Guid.Empty,
                        DataHora = legado.AtualizadoEm == default ? DateTime.Now : legado.AtualizadoEm,
                        Status = DiagnosticoStatusEnum.Concluido,
                        RoteiroCodigo = legado.Codigo,
                        SintomaRelatado = $"Importado do legado roteiros-resultados ({legado.Codigo})",
                        DiagnosticoLaudo = legado.Conclusao ?? string.Empty,
                        CausaDescricao = legado.Resultado ?? string.Empty,
                        OrigemLegado = true
                    };

                    SalvarDiagnostico(novo);
                    importados++;
                }

                return importados;
            }
            catch
            {
                return 0;
            }
        }
    }
}
