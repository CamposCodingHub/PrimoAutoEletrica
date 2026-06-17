using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Servico centralizado de gerenciamento de permissoes por perfil de acesso.
    /// </summary>
    public class PermissionService
    {
        private static readonly IReadOnlyDictionary<string, string> ModulePermissionCodes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Dashboard"] = "DASHBOARD_VER",
                ["Clientes"] = "CLIENTES_VER",
                ["Veiculos"] = "VEICULOS_VER",
                ["AutoEletricaTecnica"] = "VEICULOS_VER",
                ["Orcamentos"] = "ORCAMENTOS_VER",
                ["OrdensServico"] = "ORDENS_SERVICO_VER",
                ["OficinaKanban"] = "ORDENS_SERVICO_VER",
                ["PDV"] = "PDV_VER",
                ["Estoque"] = "ESTOQUE_VER",
                ["CatalogoPecas"] = "CATALOGO_VISUALIZAR",
                ["ImportarNFe"] = "IMPORTAR_NFE_EXECUTAR",
                ["Financeiro"] = "FINANCEIRO_VER",
                ["Relatorios"] = "RELATORIOS_VER",
                ["Fornecedores"] = "FORNECEDORES_VER",
                ["Funcionarios"] = "FUNCIONARIOS_VER",
                ["Agendamentos"] = "AGENDAMENTOS_VER",
                ["Sistema"] = "SISTEMA_CONFIGURAR"
            };

        private readonly Funcionario _funcionarioLogado;
        private readonly LoggerService? _logger;
        private readonly DatabaseService _databaseService;

        public PermissionService(Funcionario funcionarioLogado, LoggerService? logger = null, DatabaseService? databaseService = null)
        {
            _funcionarioLogado = funcionarioLogado ?? throw new ArgumentNullException(nameof(funcionarioLogado));
            _logger = logger;
            _databaseService = databaseService ?? global::PrimoAutoEletrica.App.Database;
        }

        public static PermissionService CriarParaSessaoAtual(LoggerService? logger = null, DatabaseService? databaseService = null)
        {
            var usuarioAtual = global::PrimoAutoEletrica.App.Session.CurrentUser ?? new Funcionario
            {
                Nome = global::PrimoAutoEletrica.App.Session.UserName,
                Email = global::PrimoAutoEletrica.App.Session.UserEmail,
                PerfilAcesso = global::PrimoAutoEletrica.App.Session.AccessProfile,
                Ativo = global::PrimoAutoEletrica.App.Session.IsAuthenticated
            };

            return new PermissionService(
                usuarioAtual,
                logger ?? global::PrimoAutoEletrica.App.Logger,
                databaseService ?? global::PrimoAutoEletrica.App.Database);
        }

        public bool TemPermissao(string modulo)
        {
            if (string.IsNullOrWhiteSpace(modulo))
            {
                return false;
            }

            if (ModulePermissionCodes.TryGetValue(modulo.Trim(), out var codigoModulo))
            {
                var permissaoPersistida = ObterPermissaoPersistida(codigoModulo);
                if (permissaoPersistida.HasValue)
                {
                    if (!permissaoPersistida.Value)
                    {
                        RegistrarPermissaoNegada("Modulo", modulo, codigoModulo);
                    }

                    return permissaoPersistida.Value;
                }
            }

            var modulosPermitidos = ObterModulosPermitidos();
            var permitido = modulosPermitidos.Contains(modulo, StringComparer.OrdinalIgnoreCase);

            if (!permitido)
            {
                RegistrarPermissaoNegada("Modulo", modulo, codigoModulo: ModulePermissionCodes.TryGetValue(modulo.Trim(), out var codigo) ? codigo : null);
            }

            return permitido;
        }

        public bool TemPermissaoCodigo(string codigoPermissao)
        {
            if (string.IsNullOrWhiteSpace(codigoPermissao))
            {
                return false;
            }

            var codigoNormalizado = codigoPermissao.Trim().ToUpperInvariant();

            if (NormalizarPerfil(_funcionarioLogado.PerfilAcesso) == "ADMINISTRADOR")
            {
                return true;
            }

            var permissaoPersistida = ObterPermissaoPersistida(codigoNormalizado);
            if (permissaoPersistida.HasValue)
            {
                if (!permissaoPersistida.Value)
                {
                    RegistrarPermissaoNegada("Acao", codigoNormalizado, codigoNormalizado);
                }

                return permissaoPersistida.Value;
            }

            var permitido = ObterCodigosPermitidosFallback().Contains(codigoNormalizado);
            if (!permitido)
            {
                RegistrarPermissaoNegada("Acao", codigoNormalizado, codigoNormalizado);
            }

            return permitido;
        }

        public HashSet<string> ObterModulosPermitidos()
        {
            try
            {
                var modulosPersistidos = _databaseService.ObterModulosPermitidosPorPerfil(_funcionarioLogado.PerfilAcesso);
                if (modulosPersistidos.Count > 0)
                {
                    return modulosPersistidos;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Falha ao carregar permissoes persistidas para perfil '{_funcionarioLogado.PerfilAcesso}'.", ex);
            }

            var fallback = ObterModulosPermitidosFallback();
            if (fallback.Count == 0)
            {
                _logger?.LogWarning($"Perfil '{_funcionarioLogado.PerfilAcesso}' sem modulos configurados.");
            }

            return fallback;
        }

        public string ObterPerfil()
        {
            return _funcionarioLogado.PerfilAcesso;
        }

        public string ObtePerfil()
        {
            return ObterPerfil();
        }

        public string ObterNomeUsuario()
        {
            return _funcionarioLogado.Nome;
        }

        private bool? ObterPermissaoPersistida(string codigoPermissao)
        {
            try
            {
                return _databaseService.ObterPermissaoPorPerfilECodigo(_funcionarioLogado.PerfilAcesso, codigoPermissao);
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Falha ao consultar permissao '{codigoPermissao}' para o perfil '{_funcionarioLogado.PerfilAcesso}'.", ex);
                return null;
            }
        }

        private HashSet<string> ObterCodigosPermitidosFallback()
        {
            var codigos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var modulo in ObterModulosPermitidosFallback())
            {
                if (ModulePermissionCodes.TryGetValue(modulo, out var codigoModulo))
                {
                    codigos.Add(codigoModulo);
                }
            }

            foreach (var codigoAcao in ObterCodigosAcaoFallback())
            {
                codigos.Add(codigoAcao);
            }

            return codigos;
        }

        private IEnumerable<string> ObterCodigosAcaoFallback()
        {
            return ObterCodigosAcaoPadraoPorPerfil(_funcionarioLogado.PerfilAcesso);
        }

        public static IReadOnlyCollection<string> ObterCodigosAcaoPadraoPorPerfil(string? perfil)
        {
            return NormalizarPerfil(perfil) switch
            {
                "GERENTE" => new[]
                {
                    "CLIENTES_CRIAR",
                    "CLIENTES_EDITAR",
                    "CLIENTES_EXCLUIR",
                    "CLIENTES_EXPORTAR",
                    "VEICULOS_CRIAR",
                    "VEICULOS_EDITAR",
                    "VEICULOS_EXCLUIR",
                    "VEICULOS_EXPORTAR",
                    "FORNECEDORES_CRIAR",
                    "FORNECEDORES_EDITAR",
                    "FUNCIONARIOS_CRIAR",
                    "FUNCIONARIOS_EDITAR",
                    "ESTOQUE_CRIAR",
                    "ESTOQUE_EDITAR",
                    "ESTOQUE_EXCLUIR",
                    "ESTOQUE_AJUSTAR",
                    "ESTOQUE_AJUSTAR_PRECO",
                    "ESTOQUE_INVENTARIAR",
                    "ESTOQUE_PERMITIR_NEGATIVO",
                    "CATALOGO_VISUALIZAR",
                    "CATALOGO_IMPORTAR",
                    "CATALOGO_REVISAR",
                    "CATALOGO_CRIAR_PRODUTO",
                    "CATALOGO_EXPORTAR",
                    "PDV_REGISTRAR_VENDA",
                    "PDV_CANCELAR_VENDA",
                    "PDV_APLICAR_DESCONTO",
                    "PDV_PAGAMENTO_MISTO",
                    "PDV_SUSPENDER_VENDA",
                    "PDV_RETOMAR_VENDA",
                    "CAIXA_ABRIR",
                    "CAIXA_FECHAR",
                    "CAIXA_SANGRIA",
                    "CAIXA_SUPRIMENTO",
                    "PDV_REIMPRIMIR",
                    "PDV_CANCELAR_VENDA_REGISTRADA",
                    "ORDENS_SERVICO_EDITAR",
                    "ORDENS_SERVICO_APROVAR",
                    "ORDENS_SERVICO_AVANCAR_STATUS",
                    "FINANCEIRO_EXPORTAR",
                    "FINANCEIRO_IMPRIMIR",
                    "ORCAMENTOS_CRIAR",
                    "ORCAMENTOS_EDITAR",
                    "ORCAMENTOS_DUPLICAR",
                    "ORCAMENTOS_COMPARTILHAR",
                    "ORCAMENTOS_EXPORTAR",
                    "ORCAMENTOS_IMPRIMIR",
                    "ORCAMENTOS_CONVERTER_VENDA",
                    "AGENDAMENTOS_CRIAR",
                    "AGENDAMENTOS_EDITAR",
                    "AGENDAMENTOS_CANCELAR",
                    "AGENDAMENTOS_REAGENDAR",
                    "AGENDAMENTOS_DUPLICAR",
                    "AGENDAMENTOS_GERAR_OS",
                    "AGENDAMENTOS_CHECKIN",
                    "AGENDAMENTOS_CHECKOUT",
                    "AGENDAMENTOS_COMPARTILHAR",
                    "AGENDAMENTOS_EXPORTAR",
                    "AGENDAMENTOS_IMPRIMIR",
                    "RELATORIOS_EXPORTAR",
                    "RELATORIOS_IMPRIMIR"
                },
                "VENDEDOR" => new[]
                {
                    "CLIENTES_CRIAR",
                    "CLIENTES_EDITAR",
                    "VEICULOS_CRIAR",
                    "VEICULOS_EDITAR",
                    "ORDENS_SERVICO_EDITAR",
                    "ORDENS_SERVICO_APROVAR",
                    "ORDENS_SERVICO_AVANCAR_STATUS",
                    "CATALOGO_VISUALIZAR",
                    "CATALOGO_REVISAR",
                    "ORCAMENTOS_CRIAR",
                    "ORCAMENTOS_EDITAR",
                    "ORCAMENTOS_DUPLICAR",
                    "ORCAMENTOS_COMPARTILHAR",
                    "ORCAMENTOS_EXPORTAR",
                    "ORCAMENTOS_IMPRIMIR",
                    "ORCAMENTOS_CONVERTER_VENDA",
                    "AGENDAMENTOS_CRIAR",
                    "AGENDAMENTOS_EDITAR",
                    "AGENDAMENTOS_REAGENDAR",
                    "AGENDAMENTOS_DUPLICAR",
                    "AGENDAMENTOS_GERAR_OS",
                    "AGENDAMENTOS_COMPARTILHAR",
                    "AGENDAMENTOS_EXPORTAR",
                    "AGENDAMENTOS_IMPRIMIR"
                },
                "CAIXA" => new[]
                {
                    "PDV_REGISTRAR_VENDA",
                    "PDV_CANCELAR_VENDA",
                    "PDV_PAGAMENTO_MISTO",
                    "PDV_SUSPENDER_VENDA",
                    "PDV_RETOMAR_VENDA",
                    "CAIXA_ABRIR",
                    "CAIXA_FECHAR",
                    "CAIXA_SANGRIA",
                    "CAIXA_SUPRIMENTO",
                    "PDV_REIMPRIMIR",
                    "PDV_CANCELAR_VENDA_REGISTRADA",
                    "FINANCEIRO_EXPORTAR",
                    "FINANCEIRO_IMPRIMIR",
                    "RELATORIOS_IMPRIMIR"
                },
                "ALMOXARIFE" or "ESTOQUISTA" or "ESTOQUE" => new[]
                {
                    "FORNECEDORES_CRIAR",
                    "FORNECEDORES_EDITAR",
                    "ESTOQUE_CRIAR",
                    "ESTOQUE_EDITAR",
                    "ESTOQUE_AJUSTAR",
                    "ESTOQUE_INVENTARIAR",
                    "CATALOGO_VISUALIZAR",
                    "CATALOGO_IMPORTAR",
                    "CATALOGO_REVISAR",
                    "CATALOGO_CRIAR_PRODUTO",
                    "CATALOGO_EXPORTAR"
                },
                "MECANICO" or "TECNICO" => new[]
                {
                    "ORDENS_SERVICO_EDITAR",
                    "ORDENS_SERVICO_AVANCAR_STATUS",
                    "AGENDAMENTOS_CANCELAR",
                    "AGENDAMENTOS_GERAR_OS",
                    "AGENDAMENTOS_CHECKIN",
                    "AGENDAMENTOS_CHECKOUT"
                },
                "FINANCEIRO" => new[]
                {
                    "FINANCEIRO_EXPORTAR",
                    "FINANCEIRO_IMPRIMIR",
                    "RELATORIOS_EXPORTAR",
                    "RELATORIOS_IMPRIMIR"
                },
                _ => Array.Empty<string>()
            };
        }

        private HashSet<string> ObterModulosPermitidosFallback()
        {
            return NormalizarPerfil(_funcionarioLogado.PerfilAcesso) switch
            {
                "ADMINISTRADOR" => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Dashboard", "Clientes", "Veiculos", "AutoEletricaTecnica", "Orcamentos", "OrdensServico",
                    "PDV", "Estoque", "CatalogoPecas", "ImportarNFe", "Financeiro", "Relatorios",
                    "Fornecedores", "Funcionarios", "Agendamentos", "Sistema"
                },
                "GERENTE" => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Dashboard", "Clientes", "Veiculos", "AutoEletricaTecnica", "Orcamentos", "OrdensServico",
                    "PDV", "Estoque", "CatalogoPecas", "ImportarNFe", "Financeiro", "Relatorios",
                    "Fornecedores", "Funcionarios", "Agendamentos"
                },
                "MECANICO" or "TECNICO" => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Dashboard", "Veiculos", "AutoEletricaTecnica", "OrdensServico", "Agendamentos"
                },
                "VENDEDOR" => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Dashboard", "Clientes", "Veiculos", "AutoEletricaTecnica", "Orcamentos", "OrdensServico", "CatalogoPecas", "Agendamentos"
                },
                "CAIXA" => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Dashboard", "PDV", "Financeiro", "Relatorios"
                },
                "ALMOXARIFE" or "ESTOQUISTA" or "ESTOQUE" => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Dashboard", "Estoque", "CatalogoPecas", "ImportarNFe", "Relatorios", "Fornecedores"
                },
                "FINANCEIRO" => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Dashboard", "Financeiro", "Relatorios"
                },
                _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            };
        }

        private void RegistrarPermissaoNegada(string tipo, string alvo, string? codigoModulo = null)
        {
            var detalhes = $"Usuario={ObterNomeUsuario()}; Perfil={ObterPerfil()}; Tipo={tipo}; Alvo={alvo}; Codigo={codigoModulo ?? "N/A"}";
            _logger?.LogWarning($"Permissao negada. {detalhes}");

            try
            {
                global::PrimoAutoEletrica.App.Audit.Registrar(
                    categoria: "Seguranca",
                    acao: "PermissaoNegada",
                    entidade: tipo,
                    entidadeId: codigoModulo ?? alvo,
                    detalhes: detalhes,
                    severidade: "Warning",
                    sucesso: false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning($"Falha ao auditar permissao negada: {ex.Message}");
            }
        }

        private static string NormalizarPerfil(string? perfil)
        {
            if (string.IsNullOrWhiteSpace(perfil))
            {
                return string.Empty;
            }

            var texto = perfil.Trim()
                .Replace("\u00C3\u00A1", "a")
                .Replace("\u00C3\u00A2", "a")
                .Replace("\u00C3\u00A3", "a")
                .Replace("\u00C3\u00A9", "e")
                .Replace("\u00C3\u00AA", "e")
                .Replace("\u00C3\u00AD", "i")
                .Replace("\u00C3\u00B3", "o")
                .Replace("\u00C3\u00B5", "o")
                .Replace("\u00C3\u00BA", "u")
                .Replace("\u00C3\u00A7", "c")
                .Replace("\u00C3\u0192\u00C2\u00A2", "a");

            var normalized = texto.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark && !char.IsWhiteSpace(character))
                {
                    builder.Append(char.ToUpperInvariant(character));
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
