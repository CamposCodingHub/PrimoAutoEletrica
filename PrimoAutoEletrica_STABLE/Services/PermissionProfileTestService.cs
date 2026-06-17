using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    /// <summary>
    /// Valida permissões por perfil para garantir que cada perfil
    /// acessa apenas os módulos e funcionalidades permitidos.
    /// </summary>
    public sealed class PermissionProfileTestService
    {
        private readonly LoggerService _logger;

        public PermissionProfileTestService(LoggerService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public PermissionTestResult Run()
        {
            var result = new PermissionTestResult();
            _logger.LogInfo("Iniciando teste de permissões por perfil.");

            try
            {
                // Criar usuários sintéticos para cada perfil
                var admin = EnsureFuncionario("permission-admin@primoauto.com", "Permission Admin", "Administrador");
                var gerente = EnsureFuncionario("permission-gerente@primoauto.com", "Permission Gerente", "Gerente");
                var mecanico = EnsureFuncionario("permission-mecanico@primoauto.com", "Permission Mecanico", "Mecanico");
                var vendedor = EnsureFuncionario("permission-vendedor@primoauto.com", "Permission Vendedor", "Vendedor");
                var caixa = EnsureFuncionario("permission-caixa@primoauto.com", "Permission Caixa", "Caixa");
                var almoxarife = EnsureFuncionario("permission-almoxarife@primoauto.com", "Permission Almoxarife", "Almoxarife");

                // Testar permissões do Administrador
                TestAdministratorPermissions(result, admin);

                // Testar permissões do Gerente
                TestGerentePermissions(result, gerente);

                // Testar permissões do Mecânico
                TestMecanicoPermissions(result, mecanico);

                // Testar permissões do Vendedor
                TestVendedorPermissions(result, vendedor);

                // Testar permissões do Caixa
                TestCaixaPermissions(result, caixa);

                // Testar permissões do Almoxarife
                TestAlmoxarifePermissions(result, almoxarife);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro durante teste de permissões: {ex.Message}");
                result.AddError("TestException", ex.Message);
            }

            result.ReportPath = PersistReport(result);
            return result;
        }

        private void TestAdministratorPermissions(PermissionTestResult result, Funcionario admin)
        {
            _logger.LogInfo("Testando permissões do Administrador...");

            var permissionService = new PermissionService(admin, _logger, App.Database);

            // Administrador deve acessar tudo
            var requiredPermissions = new[]
            {
                "DASHBOARD_VER",
                "CLIENTES_VER", "CLIENTES_CRIAR", "CLIENTES_EDITAR", "CLIENTES_EXCLUIR",
                "VEICULOS_VER", "VEICULOS_CRIAR", "VEICULOS_EDITAR", "VEICULOS_EXCLUIR",
                "ORCAMENTOS_VER", "ORCAMENTOS_CRIAR", "ORCAMENTOS_EDITAR", "ORCAMENTOS_EXCLUIR",
                "ORDEM_SERVICO_VER", "ORDEM_SERVICO_CRIAR", "ORDEM_SERVICO_EDITAR", "ORDEM_SERVICO_EXCLUIR",
                "PDV_VER", "PDV_REGISTRAR_VENDA",
                "ESTOQUE_VER", "ESTOQUE_CRIAR", "ESTOQUE_EDITAR", "ESTOQUE_EXCLUIR",
                "FINANCEIRO_VER", "FINANCEIRO_CRIAR", "FINANCEIRO_EDITAR", "FINANCEIRO_EXCLUIR",
                "FORNECEDORES_VER", "FORNECEDORES_CRIAR", "FORNECEDORES_EDITAR", "FORNECEDORES_EXCLUIR",
                "FUNCIONARIOS_VER", "FUNCIONARIOS_CRIAR", "FUNCIONARIOS_EDITAR", "FUNCIONARIOS_EXCLUIR",
                "AGENDAMENTOS_VER", "AGENDAMENTOS_CRIAR", "AGENDAMENTOS_EDITAR", "AGENDAMENTOS_EXCLUIR",
                "RELATORIOS_VER",
                "CONFIGURACOES_VER", "CONFIGURACOES_EDITAR",
                "PERMISSOES_VER", "PERMISSOES_EDITAR"
            };

            foreach (var permission in requiredPermissions)
            {
                if (!permissionService.TemPermissaoCodigo(permission))
                {
                    result.AddError("AdminMissingPermission", $"Administrador não tem permissão: {permission}");
                }
            }

            _logger.LogInfo("Teste de permissões do Administrador concluído");
        }

        private void TestGerentePermissions(PermissionTestResult result, Funcionario gerente)
        {
            _logger.LogInfo("Testando permissões do Gerente...");

            var permissionService = new PermissionService(gerente, _logger, App.Database);

            // Gerente deve acessar relatórios e financeiro, mas pode ter restrições de configuração
            var requiredPermissions = new[]
            {
                "DASHBOARD_VER",
                "CLIENTES_VER", "CLIENTES_CRIAR", "CLIENTES_EDITAR",
                "VEICULOS_VER", "VEICULOS_CRIAR", "VEICULOS_EDITAR",
                "ORCAMENTOS_VER", "ORCAMENTOS_CRIAR", "ORCAMENTOS_EDITAR",
                "ORDEM_SERVICO_VER", "ORDEM_SERVICO_CRIAR", "ORDEM_SERVICO_EDITAR",
                "ESTOQUE_VER",
                "FINANCEIRO_VER", "FINANCEIRO_CRIAR", "FINANCEIRO_EDITAR",
                "AGENDAMENTOS_VER", "AGENDAMENTOS_CRIAR", "AGENDAMENTOS_EDITAR",
                "RELATORIOS_VER"
            };

            foreach (var permission in requiredPermissions)
            {
                if (!permissionService.TemPermissaoCodigo(permission))
                {
                    result.AddError("GerenteMissingPermission", $"Gerente não tem permissão: {permission}");
                }
            }

            // Gerente não deve acessar configurações críticas
            var restrictedPermissions = new[]
            {
                "PERMISSOES_EDITAR"
            };

            foreach (var permission in restrictedPermissions)
            {
                if (permissionService.TemPermissaoCodigo(permission))
                {
                    result.AddWarning("GerenteHasRestrictedPermission", $"Gerente tem permissão restrita: {permission}");
                }
            }

            _logger.LogInfo("Teste de permissões do Gerente concluído");
        }

        private void TestMecanicoPermissions(PermissionTestResult result, Funcionario mecanico)
        {
            _logger.LogInfo("Testando permissões do Mecânico...");

            var permissionService = new PermissionService(mecanico, _logger, App.Database);

            // Mecânico deve acessar OS, veículos, checklist e diagnóstico
            var requiredPermissions = new[]
            {
                "DASHBOARD_VER",
                "VEICULOS_VER",
                "ORDEM_SERVICO_VER", "ORDEM_SERVICO_CRIAR", "ORDEM_SERVICO_EDITAR",
                "AGENDAMENTOS_VER", "AGENDAMENTOS_CHECKIN", "AGENDAMENTOS_CHECKOUT"
            };

            foreach (var permission in requiredPermissions)
            {
                if (!permissionService.TemPermissaoCodigo(permission))
                {
                    result.AddError("MecanicoMissingPermission", $"Mecânico não tem permissão: {permission}");
                }
            }

            // Mecânico não deve acessar financeiro completo
            var restrictedPermissions = new[]
            {
                "FINANCEIRO_VER",
                "FINANCEIRO_CRIAR",
                "FINANCEIRO_EDITAR",
                "FINANCEIRO_EXCLUIR"
            };

            foreach (var permission in restrictedPermissions)
            {
                if (permissionService.TemPermissaoCodigo(permission))
                {
                    result.AddError("MecanicoHasRestrictedPermission", $"Mecânico tem permissão restrita: {permission}");
                }
            }

            _logger.LogInfo("Teste de permissões do Mecânico concluído");
        }

        private void TestVendedorPermissions(PermissionTestResult result, Funcionario vendedor)
        {
            _logger.LogInfo("Testando permissões do Vendedor...");

            var permissionService = new PermissionService(vendedor, _logger, App.Database);

            // Vendedor deve acessar clientes, veículos, orçamentos e agenda
            var requiredPermissions = new[]
            {
                "DASHBOARD_VER",
                "CLIENTES_VER", "CLIENTES_CRIAR", "CLIENTES_EDITAR",
                "VEICULOS_VER",
                "ORCAMENTOS_VER", "ORCAMENTOS_CRIAR", "ORCAMENTOS_EDITAR",
                "AGENDAMENTOS_VER", "AGENDAMENTOS_CRIAR", "AGENDAMENTOS_EDITAR",
                "AGENDAMENTOS_COMPARTILHAR"
            };

            foreach (var permission in requiredPermissions)
            {
                if (!permissionService.TemPermissaoCodigo(permission))
                {
                    result.AddError("VendedorMissingPermission", $"Vendedor não tem permissão: {permission}");
                }
            }

            // Vendedor não deve acessar financeiro completo
            var restrictedPermissions = new[]
            {
                "FINANCEIRO_VER",
                "FINANCEIRO_CRIAR",
                "FINANCEIRO_EDITAR",
                "FINANCEIRO_EXCLUIR"
            };

            foreach (var permission in restrictedPermissions)
            {
                if (permissionService.TemPermissaoCodigo(permission))
                {
                    result.AddWarning("VendedorHasRestrictedPermission", $"Vendedor tem permissão restrita: {permission}");
                }
            }

            _logger.LogInfo("Teste de permissões do Vendedor concluído");
        }

        private void TestCaixaPermissions(PermissionTestResult result, Funcionario caixa)
        {
            _logger.LogInfo("Testando permissões do Caixa...");

            var permissionService = new PermissionService(caixa, _logger, App.Database);

            // Caixa deve acessar PDV e Caixa
            var requiredPermissions = new[]
            {
                "DASHBOARD_VER",
                "PDV_VER", "PDV_REGISTRAR_VENDA",
                "FINANCEIRO_VER"
            };

            foreach (var permission in requiredPermissions)
            {
                if (!permissionService.TemPermissaoCodigo(permission))
                {
                    result.AddError("CaixaMissingPermission", $"Caixa não tem permissão: {permission}");
                }
            }

            // Caixa não deve acessar Funcionários, Configurações críticas e permissões
            var restrictedPermissions = new[]
            {
                "FUNCIONARIOS_VER",
                "FUNCIONARIOS_CRIAR",
                "FUNCIONARIOS_EDITAR",
                "FUNCIONARIOS_EXCLUIR",
                "CONFIGURACOES_VER",
                "CONFIGURACOES_EDITAR",
                "PERMISSOES_VER",
                "PERMISSOES_EDITAR"
            };

            foreach (var permission in restrictedPermissions)
            {
                if (permissionService.TemPermissaoCodigo(permission))
                {
                    result.AddError("CaixaHasRestrictedPermission", $"Caixa tem permissão restrita: {permission}");
                }
            }

            _logger.LogInfo("Teste de permissões do Caixa concluído");
        }

        private void TestAlmoxarifePermissions(PermissionTestResult result, Funcionario almoxarife)
        {
            _logger.LogInfo("Testando permissões do Almoxarife...");

            var permissionService = new PermissionService(almoxarife, _logger, App.Database);

            // Almoxarife deve acessar estoque e fornecedores
            var requiredPermissions = new[]
            {
                "DASHBOARD_VER",
                "ESTOQUE_VER", "ESTOQUE_CRIAR", "ESTOQUE_EDITAR",
                "FORNECEDORES_VER", "FORNECEDORES_CRIAR", "FORNECEDORES_EDITAR"
            };

            foreach (var permission in requiredPermissions)
            {
                if (!permissionService.TemPermissaoCodigo(permission))
                {
                    result.AddError("AlmoxarifeMissingPermission", $"Almoxarife não tem permissão: {permission}");
                }
            }

            // Almoxarife não deve alterar caixa/financeiro sensível
            var restrictedPermissions = new[]
            {
                "FINANCEIRO_CRIAR",
                "FINANCEIRO_EDITAR",
                "FINANCEIRO_EXCLUIR",
                "PDV_REGISTRAR_VENDA"
            };

            foreach (var permission in restrictedPermissions)
            {
                if (permissionService.TemPermissaoCodigo(permission))
                {
                    result.AddWarning("AlmoxarifeHasRestrictedPermission", $"Almoxarife tem permissão restrita: {permission}");
                }
            }

            _logger.LogInfo("Teste de permissões do Almoxarife concluído");
        }

        private Funcionario EnsureFuncionario(string email, string nome, string perfil)
        {
            var funcionarios = App.Repositories.Funcionarios.ObterTodos(false);
            var existente = funcionarios.FirstOrDefault(f => string.Equals(f.Email, email, StringComparison.OrdinalIgnoreCase));

            if (existente != null)
            {
                return existente;
            }

            var novo = new Funcionario
            {
                Email = email,
                Nome = nome,
                Funcao = perfil,
                PerfilAcesso = perfil,
                Ativo = true,
                DataAdmissao = DateTime.Now,
                Senha = "HASH_TESTE",
                DataCadastro = DateTime.Now
            };

            // Usar o método correto do repositório
            App.Repositories.Funcionarios.Salvar(novo);
            return novo;
        }

        private string PersistReport(PermissionTestResult result)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var reportDir = Path.Combine(App.RuntimeAppDataPath, "TestResults", "PermissionValidation");
            Directory.CreateDirectory(reportDir);

            var reportPath = Path.Combine(reportDir, $"PermissionValidation_{timestamp}.md");
            var reportContent = GenerateReport(result);

            File.WriteAllText(reportPath, reportContent);
            _logger.LogInfo($"Relatório de validação de permissões salvo em: {reportPath}");

            return reportPath;
        }

        private string GenerateReport(PermissionTestResult result)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Relatório de Validação de Permissões por Perfil");
            sb.AppendLine();
            sb.AppendLine($"**Data/Hora:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**Total de Erros:** {result.Errors.Count}");
            sb.AppendLine($"**Total de Avisos:** {result.Warnings.Count}");
            sb.AppendLine();
            sb.AppendLine("## Erros");
            sb.AppendLine();
            foreach (var error in result.Errors)
            {
                sb.AppendLine($"- **{error.Key}:** {error.Value}");
            }
            sb.AppendLine();
            sb.AppendLine("## Avisos");
            sb.AppendLine();
            foreach (var warning in result.Warnings)
            {
                sb.AppendLine($"- **{warning.Key}:** {warning.Value}");
            }
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("Gerado automaticamente por PermissionProfileTestService");

            return sb.ToString();
        }
    }

    public class PermissionTestResult
    {
        public Dictionary<string, string> Errors { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> Warnings { get; } = new Dictionary<string, string>();
        public string ReportPath { get; set; } = string.Empty;

        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;

        public void AddError(string key, string message)
        {
            Errors[key] = message;
        }

        public void AddWarning(string key, string message)
        {
            Warnings[key] = message;
        }
    }
}
