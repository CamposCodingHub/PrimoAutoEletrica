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
                ["FiscalOperacoes"] = "IMPORTAR_NFE_EXECUTAR",
                ["OperacoesFiscais"] = "IMPORTAR_NFE_EXECUTAR",
                ["Financeiro"] = "FINANCEIRO_VER",
                ["Relatorios"] = "RELATORIOS_VER",
                ["Fornecedores"] = "FORNECEDORES_VER",
                ["Funcionarios"] = "FUNCIONARIOS_VER",
                ["Agendamentos"] = "AGENDAMENTOS_VER",
                ["Sistema"] = "SISTEMA_CONFIGURAR"
            };

        private static readonly HashSet<string> CriticalPermissionCodes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "CLIENTES_EXCLUIR",
                "VEICULOS_EXCLUIR",
                "ESTOQUE_EXCLUIR",
                "ESTOQUE_AJUSTAR",
                "ESTOQUE_AJUSTAR_PRECO",
                "ESTOQUE_PERMITIR_NEGATIVO",
                "FUNCIONARIOS_EXCLUIR",
                "FUNCIONARIOS_EDITAR",
                "FORNECEDORES_EXCLUIR",
                "FINANCEIRO_EDITAR",
                "FINANCEIRO_EXCLUIR",
                "FINANCEIRO_PAGAR",
                "FINANCEIRO_RECEBER",
                "SISTEMA_CONFIGURAR",
                "PERMISSOES_GERENCIAR"
            };

        private readonly Funcionario _funcionarioLogado;
        private readonly LoggerService? _logger;
        private readonly DatabaseService? _databaseService;
        private readonly Func<string, bool?>? _permissionLookupOverride;

        public PermissionService(Funcionario funcionarioLogado, LoggerService? logger = null, DatabaseService? databaseService = null)
        {
            _funcionarioLogado = funcionarioLogado ?? throw new ArgumentNullException(nameof(funcionarioLogado));
            _logger = logger;
            _databaseService = databaseService ?? global::PrimoAutoEletrica.App.Database;
            _permissionLookupOverride = null;
        }

        /// <summary>
        /// Construtor de teste: o lookup pode retornar true/false/null ou lançar para simular Unavailable.
        /// </summary>
        public PermissionService(Funcionario funcionarioLogado, Func<string, bool?> permissionLookup, LoggerService? logger = null)
        {
            _funcionarioLogado = funcionarioLogado ?? throw new ArgumentNullException(nameof(funcionarioLogado));
            _permissionLookupOverride = permissionLookup ?? throw new ArgumentNullException(nameof(permissionLookup));
            _logger = logger;
            _databaseService = null;
        }

        public static bool IsCriticalPermission(string? codigoPermissao)
        {
            return !string.IsNullOrWhiteSpace(codigoPermissao)
                && CriticalPermissionCodes.Contains(codigoPermissao.Trim());
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

        /// <summary>
        /// Valida acesso a um módulo específico de forma centralizada.
        /// Este método deve ser usado por todos os menus para verificação de permissões.
        /// </summary>
        /// <param name="module">Nome do módulo a ser validado</param>
        /// <returns>Retorna true se o usuário tem permissão, false caso contrário</returns>
        public bool ValidateAccess(string module)
        {
            return TemPermissao(module);
        }

        /// <summary>
        /// Valida acesso baseado em código de permissão específico.
        /// </summary>
        /// <param name="permissionCode">Código da permissão a ser validada</param>
        /// <returns>Retorna true se o usuário tem permissão, false caso contrário</returns>
        public bool ValidateAccessByCode(string permissionCode)
        {
            return TemPermissaoCodigo(permissionCode);
        }

        /// <summary>
        /// Valifica acesso a múltiplos módulos de uma vez.
        /// </summary>
        /// <param name="modules">Lista de módulos a serem validados</param>
        /// <returns>Retorna true se o usuário tem permissão para todos os módulos, false caso contrário</returns>
        public bool ValidateAccessToAll(params string[] modules)
        {
            return modules.All(ValidateAccess);
        }

        /// <summary>
        /// Valifica acesso a pelo menos um dos módulos especificados.
        /// </summary>
        /// <param name="modules">Lista de módulos a serem validados</param>
        /// <returns>Retorna true se o usuário tem permissão para pelo menos um módulo, false caso contrário</returns>
        public bool ValidateAccessToAny(params string[] modules)
        {
            return modules.Any(ValidateAccess);
        }

        public bool TemPermissao(string modulo)
        {
            if (string.IsNullOrWhiteSpace(modulo))
            {
                return false;
            }

            if (string.Equals(modulo.Trim(), "Help", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(modulo.Trim(), "Ajuda", StringComparison.OrdinalIgnoreCase))
            {
                return _funcionarioLogado.Ativo || global::PrimoAutoEletrica.App.Session.IsAuthenticated;
            }

            if (ModulePermissionCodes.TryGetValue(modulo.Trim(), out var codigoModulo))
            {
                var consulta = ConsultarPermissaoPersistida(codigoModulo);
                if (consulta.IsUnavailable)
                {
                    RegistrarPermissaoNegada("ModuloIndisponivel", modulo, codigoModulo);
                    return false;
                }

                if (consulta.IsAllowed || (consulta.IsDenied && string.Equals(consulta.Detail, "persistido_negado", StringComparison.Ordinal)))
                {
                    if (consulta.IsDenied)
                    {
                        RegistrarPermissaoNegada("Modulo", modulo, codigoModulo);
                    }

                    return consulta.IsAllowed;
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
            var resultado = VerificarPermissaoCodigo(codigoPermissao);
            return resultado.IsAllowed;
        }

        public PermissionCheckResult VerificarPermissaoCodigo(string codigoPermissao)
        {
            if (string.IsNullOrWhiteSpace(codigoPermissao))
            {
                return PermissionCheckResult.Denied("codigo_vazio");
            }

            var codigoNormalizado = codigoPermissao.Trim().ToUpperInvariant();

            if (NormalizarPerfil(_funcionarioLogado.PerfilAcesso) == "ADMINISTRADOR")
            {
                return PermissionCheckResult.Allowed("administrador");
            }

            var consulta = ConsultarPermissaoPersistida(codigoNormalizado);
            if (consulta.IsUnavailable)
            {
                _logger?.LogWarning(
                    $"Permissao '{codigoNormalizado}' indisponivel (infra). Fail-closed. Detalhe={consulta.Detail}");
                RegistrarPermissaoNegada("AcaoIndisponivel", codigoNormalizado, codigoNormalizado);
                return consulta;
            }

            if (consulta.IsAllowed)
            {
                return consulta;
            }

            if (consulta.IsDenied && string.Equals(consulta.Detail, "persistido_negado", StringComparison.Ordinal))
            {
                RegistrarPermissaoNegada("Acao", codigoNormalizado, codigoNormalizado);
                return consulta;
            }

            // sem_linha / outros: fallback de perfil (não é Unavailable).
            var permitido = ObterCodigosPermitidosFallback().Contains(codigoNormalizado);
            if (!permitido)
            {
                RegistrarPermissaoNegada("Acao", codigoNormalizado, codigoNormalizado);
                return PermissionCheckResult.Denied("fallback_negado");
            }

            return PermissionCheckResult.Allowed("fallback_perfil");
        }

        /// <summary>
        /// Operações críticas: Unavailable e Denied bloqueiam. Allowed libera.
        /// </summary>
        public bool GarantirPermissaoCritica(string codigoPermissao)
        {
            var resultado = VerificarPermissaoCodigo(codigoPermissao);
            if (resultado.IsUnavailable)
            {
                _logger?.LogError(
                    $"Operacao critica bloqueada: permissao '{codigoPermissao}' Unavailable. Fail-closed.");
                return false;
            }

            return resultado.IsAllowed;
        }

        public HashSet<string> ObterModulosPermitidos()
        {
            try
            {
                if (_databaseService == null)
                {
                    return ObterModulosPermitidosFallback();
                }

                var modulosPersistidos = _databaseService.ObterModulosPermitidosPorPerfil(_funcionarioLogado.PerfilAcesso);
                if (modulosPersistidos.Count > 0)
                {
                    return modulosPersistidos;
                }
            }
            catch (Exception ex)
            {
                // Fail-closed para menus: sem módulos quando a infra falha (não libera fallback amplo).
                _logger?.LogError($"Falha ao carregar permissoes persistidas para perfil '{_funcionarioLogado.PerfilAcesso}'. Fail-closed (sem fallback).", ex);
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
            var resultado = ConsultarPermissaoPersistida(codigoPermissao);
            if (resultado.IsUnavailable)
            {
                return null;
            }

            if (resultado.IsAllowed)
            {
                return true;
            }

            if (resultado.IsDenied && string.Equals(resultado.Detail, "persistido_negado", StringComparison.Ordinal))
            {
                return false;
            }

            return null;
        }

        private PermissionCheckResult ConsultarPermissaoPersistida(string codigoPermissao)
        {
            try
            {
                bool? valor;
                if (_permissionLookupOverride != null)
                {
                    valor = _permissionLookupOverride(codigoPermissao);
                }
                else if (_databaseService == null)
                {
                    return PermissionCheckResult.Unavailable("database_ausente");
                }
                else
                {
                    valor = _databaseService.ObterPermissaoPorPerfilECodigo(_funcionarioLogado.PerfilAcesso, codigoPermissao);
                }

                if (!valor.HasValue)
                {
                    // Sem linha: caller pode aplicar fallback (não é Unavailable).
                    return PermissionCheckResult.Denied("sem_linha");
                }

                return valor.Value
                    ? PermissionCheckResult.Allowed("persistido")
                    : PermissionCheckResult.Denied("persistido_negado");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Falha ao consultar permissao '{codigoPermissao}' para o perfil '{_funcionarioLogado.PerfilAcesso}'.", ex);
                return PermissionCheckResult.Unavailable(ex.GetType().Name + ": " + ex.Message);
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
                    "PDV", "Estoque", "CatalogoPecas", "ImportarNFe", "FiscalOperacoes", "Financeiro", "Relatorios",
                    "Fornecedores", "Funcionarios", "Agendamentos", "Sistema"
                },
                "GERENTE" => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "Dashboard", "Clientes", "Veiculos", "AutoEletricaTecnica", "Orcamentos", "OrdensServico",
                    "PDV", "Estoque", "CatalogoPecas", "ImportarNFe", "FiscalOperacoes", "Financeiro", "Relatorios",
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
                    "Dashboard", "Estoque", "CatalogoPecas", "ImportarNFe", "FiscalOperacoes", "Relatorios", "Fornecedores"
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
