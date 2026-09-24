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
    public interface IChecklistTecnicoService
    {
        ChecklistTecnicoOS Salvar(ChecklistTecnicoOS checklist);
        ChecklistTecnicoOS? ObterPorOrdemServicoId(Guid ordemServicoId);
        ChecklistTecnicoOS? ObterPorId(Guid id);
        IReadOnlyList<ChecklistTecnicoOS> ListarPorVeiculoId(Guid veiculoId);
        ChecklistTecnicoOS ObterOuCriarPadrao(Guid ordemServicoId, Guid veiculoId, Guid? clienteId = null, bool isLinhaPesada = false);
        bool Concluir(Guid id, string responsavel, string? observacoes = null);
    }

    /// <summary>
    /// Servico de gerenciamento pericial de Checklists Tecnicos Multiponto de autoeletrica.
    /// Opera com associacao rigorosa por OrdemServicoId, VeiculoId e ClienteId.
    /// Suporta arquiteturas 12V e 24V, medicoes antes/depois com delta e laudo auditavel.
    /// </summary>
    public sealed class ChecklistTecnicoService : IChecklistTecnicoService
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly object _lock = new();
        private readonly string? _instanceStorageDirectory;

        /// <summary>
        /// Diretorio de testes para isolamento completo em suite automatizada.
        /// </summary>
        public static string? TestStorageDirectoryOverride { get; set; }

        public ChecklistTecnicoService(string? storageDirectoryOverride = null)
        {
            _instanceStorageDirectory = storageDirectoryOverride;
        }

        private string ObterDiretorioArmazenamento()
        {
            var baseDir = _instanceStorageDirectory
                          ?? TestStorageDirectoryOverride
                          ?? Path.Combine(App.RuntimeAppDataPath, "AutoEletrica", "checklists");
            Directory.CreateDirectory(baseDir);
            return baseDir;
        }

        private string ObterCaminhoArquivoOs(Guid ordemServicoId) =>
            Path.Combine(ObterDiretorioArmazenamento(), $"checklist-os-{ordemServicoId:N}.json");

        public ChecklistTecnicoOS Salvar(ChecklistTecnicoOS checklist)
        {
            ArgumentNullException.ThrowIfNull(checklist);

            if (checklist.Id == Guid.Empty)
            {
                checklist.Id = Guid.NewGuid();
            }

            if (checklist.OrdemServicoId == Guid.Empty)
            {
                throw new ArgumentException("O checklist exige vinculo com uma Ordem de Servico valida (OrdemServicoId obrigatorio).", nameof(checklist.OrdemServicoId));
            }

            if (checklist.VeiculoId == Guid.Empty)
            {
                throw new ArgumentException("O checklist exige vinculo com um Veiculo valido (VeiculoId obrigatorio).", nameof(checklist.VeiculoId));
            }

            lock (_lock)
            {
                var caminho = ObterCaminhoArquivoOs(checklist.OrdemServicoId);
                var json = JsonSerializer.Serialize(checklist, JsonOpts);
                File.WriteAllText(caminho, json, Encoding.UTF8);
            }

            return checklist;
        }

        public ChecklistTecnicoOS? ObterPorOrdemServicoId(Guid ordemServicoId)
        {
            if (ordemServicoId == Guid.Empty) return null;

            lock (_lock)
            {
                var caminho = ObterCaminhoArquivoOs(ordemServicoId);
                if (!File.Exists(caminho)) return null;

                try
                {
                    var json = File.ReadAllText(caminho, Encoding.UTF8);
                    return JsonSerializer.Deserialize<ChecklistTecnicoOS>(json, JsonOpts);
                }
                catch
                {
                    return null;
                }
            }
        }

        public ChecklistTecnicoOS? ObterPorId(Guid id)
        {
            if (id == Guid.Empty) return null;

            lock (_lock)
            {
                var dir = ObterDiretorioArmazenamento();
                var arquivos = Directory.GetFiles(dir, "checklist-os-*.json");
                foreach (var arquivo in arquivos)
                {
                    try
                    {
                        var json = File.ReadAllText(arquivo, Encoding.UTF8);
                        var item = JsonSerializer.Deserialize<ChecklistTecnicoOS>(json, JsonOpts);
                        if (item?.Id == id) return item;
                    }
                    catch
                    {
                        // best-effort
                    }
                }
                return null;
            }
        }

        public IReadOnlyList<ChecklistTecnicoOS> ListarPorVeiculoId(Guid veiculoId)
        {
            if (veiculoId == Guid.Empty) return Array.Empty<ChecklistTecnicoOS>();

            lock (_lock)
            {
                var lista = new List<ChecklistTecnicoOS>();
                var dir = ObterDiretorioArmazenamento();
                var arquivos = Directory.GetFiles(dir, "checklist-os-*.json");

                foreach (var arquivo in arquivos)
                {
                    try
                    {
                        var json = File.ReadAllText(arquivo, Encoding.UTF8);
                        var item = JsonSerializer.Deserialize<ChecklistTecnicoOS>(json, JsonOpts);
                        if (item != null && item.VeiculoId == veiculoId)
                        {
                            lista.Add(item);
                        }
                    }
                    catch
                    {
                        // best-effort
                    }
                }

                return lista.OrderByDescending(c => c.DataRegistro).ToList();
            }
        }

        public ChecklistTecnicoOS ObterOuCriarPadrao(Guid ordemServicoId, Guid veiculoId, Guid? clienteId = null, bool isLinhaPesada = false)
        {
            var existente = ObterPorOrdemServicoId(ordemServicoId);
            if (existente != null)
            {
                if (clienteId.HasValue && !existente.ClienteId.HasValue)
                {
                    existente.ClienteId = clienteId;
                    Salvar(existente);
                }
                return existente;
            }

            var novo = new ChecklistTecnicoOS
            {
                Id = Guid.NewGuid(),
                OrdemServicoId = ordemServicoId,
                VeiculoId = veiculoId,
                ClienteId = clienteId,
                ContextoTensao = isLinhaPesada ? "24V" : "12V",
                DataRegistro = DateTime.Now,
                Itens = GerarItensPadrao(isLinhaPesada)
            };

            return Salvar(novo);
        }

        public bool Concluir(Guid id, string responsavel, string? observacoes = null)
        {
            var checklist = ObterPorId(id);
            if (checklist == null) return false;

            checklist.Concluido = true;
            checklist.DataConclusao = DateTime.Now;
            if (!string.IsNullOrWhiteSpace(responsavel))
            {
                checklist.TecnicoResponsavel = responsavel;
            }
            if (!string.IsNullOrWhiteSpace(observacoes))
            {
                checklist.ObservacoesGerais = observacoes;
            }

            Salvar(checklist);
            return true;
        }

        public static List<ChecklistTecnicoItem> GerarItensPadrao(bool isLinhaPesada)
        {
            var itens = new List<ChecklistTecnicoItem>
            {
                // BATERIA
                new()
                {
                    ItemId = "BAT_01",
                    Secao = "BATERIA",
                    Descricao = isLinhaPesada ? "Tensão de repouso da bateria principal A (24V série)" : "Tensão de repouso da bateria em circuito aberto",
                    Unidade = "V",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                },
                new()
                {
                    ItemId = "BAT_02",
                    Secao = "BATERIA",
                    Descricao = "Capacidade de partida a frio (CCA) / Condutância",
                    Unidade = "CCA",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                },
                new()
                {
                    ItemId = "BAT_03",
                    Secao = "BATERIA",
                    Descricao = "Estado físico dos bornes, isolação e fixação no suporte",
                    Unidade = "",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                }
            };

            if (isLinhaPesada)
            {
                itens.Add(new ChecklistTecnicoItem
                {
                    ItemId = "BAT_04",
                    Secao = "BATERIA",
                    Descricao = "Tensão de repouso da bateria auxiliar B (24V série)",
                    Unidade = "V",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                });
                itens.Add(new ChecklistTecnicoItem
                {
                    ItemId = "BAT_05",
                    Secao = "BATERIA",
                    Descricao = "Desbalanceamento de tensão entre baterias A e B (máx 0.3V)",
                    Unidade = "V",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                });
            }

            // ALTERNADOR
            itens.AddRange(new[]
            {
                new ChecklistTecnicoItem
                {
                    ItemId = "ALT_01",
                    Secao = "ALTERNADOR",
                    Descricao = "Tensão de carga em marcha-lenta (sem consumidores)",
                    Unidade = "V",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                },
                new ChecklistTecnicoItem
                {
                    ItemId = "ALT_02",
                    Secao = "ALTERNADOR",
                    Descricao = "Tensão de carga sob carga máxima (faróis altos, ventilação e ar)",
                    Unidade = "V",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                },
                new ChecklistTecnicoItem
                {
                    ItemId = "ALT_03",
                    Secao = "ALTERNADOR",
                    Descricao = "Queda de tensão no cabo positivo B+ do alternador à bateria",
                    Unidade = "mV",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                },
                new ChecklistTecnicoItem
                {
                    ItemId = "ALT_04",
                    Secao = "ALTERNADOR",
                    Descricao = "Ondulação de corrente alternada (Ripple AC dos diodos retificadores)",
                    Unidade = "mV",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                },

                // PARTIDA
                new ChecklistTecnicoItem
                {
                    ItemId = "PAR_01",
                    Secao = "PARTIDA",
                    Descricao = "Corrente de partida / Consumo de amperagem do motor de arranque",
                    Unidade = "A",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                },
                new ChecklistTecnicoItem
                {
                    ItemId = "PAR_02",
                    Secao = "PARTIDA",
                    Descricao = "Queda de tensão na linha 30/50 durante o acionamento da partida",
                    Unidade = "V",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                },
                new ChecklistTecnicoItem
                {
                    ItemId = "PAR_03",
                    Secao = "PARTIDA",
                    Descricao = "Engrenamento do pinhão Bendix e ruído mecânico do automático",
                    Unidade = "",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                },

                // ATERRAMENTO
                new ChecklistTecnicoItem
                {
                    ItemId = "ATE_01",
                    Secao = "ATERRAMENTO",
                    Descricao = "Queda de tensão do polo negativo da bateria ao bloco do motor",
                    Unidade = "mV",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                },
                new ChecklistTecnicoItem
                {
                    ItemId = "ATE_02",
                    Secao = "ATERRAMENTO",
                    Descricao = "Queda de tensão do polo negativo da bateria ao chassi / cabine",
                    Unidade = "mV",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                },

                // ILUMINAÇÃO & SINALIZAÇÃO
                new ChecklistTecnicoItem
                {
                    ItemId = "ILU_01",
                    Secao = "ILUMINAÇÃO",
                    Descricao = "Farol alto, baixo, milha e estado dos conectores H4/H7",
                    Unidade = "",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                },
                new ChecklistTecnicoItem
                {
                    ItemId = "ILU_02",
                    Secao = "ILUMINAÇÃO",
                    Descricao = "Lanternas traseiras, luz de freio, iluminação de placa e ré",
                    Unidade = "",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                },
                new ChecklistTecnicoItem
                {
                    ItemId = "ILU_03",
                    Secao = "ILUMINAÇÃO",
                    Descricao = "Setas de direção, pisca-alerta e relé temporizador",
                    Unidade = "",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                },

                // FUSÍVEIS & RELÉS
                new ChecklistTecnicoItem
                {
                    ItemId = "FUS_01",
                    Secao = "FUSÍVEIS E RELÉS",
                    Descricao = "Inspeção visual da caixa de fusíveis, oxidação e aquecimento",
                    Unidade = "",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                },
                new ChecklistTecnicoItem
                {
                    ItemId = "FUS_02",
                    Secao = "FUSÍVEIS E RELÉS",
                    Descricao = "Relé principal de ignição e relé auxiliar de partida",
                    Unidade = "",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                },

                // SEGURANÇA & CONSUMO
                new ChecklistTecnicoItem
                {
                    ItemId = "SEG_01",
                    Secao = "SEGURANÇA E CONSUMO",
                    Descricao = "Corrente de fuga em repouso com chave desligada (consumo parasita)",
                    Unidade = "mA",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                },
                new ChecklistTecnicoItem
                {
                    ItemId = "SEG_02",
                    Secao = "SEGURANÇA E CONSUMO",
                    Descricao = "Chave geral / corta-corrente e travas elétricas",
                    Unidade = "",
                    Momento = "AntesReparo",
                    Status = ChecklistStatusEnum.NaoTestado
                }
            });

            return itens;
        }
    }
}
