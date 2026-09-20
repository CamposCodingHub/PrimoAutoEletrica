using System;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests.Security
{
    /// <summary>
    /// Testes reais contra PermissionService (nÃ£o helpers inventados sÃ³ no teste).
    /// Cobre fail-closed em Unavailable para operaÃ§Ãµes crÃ­ticas.
    /// </summary>
    public class PermissionServiceFailClosedTests
    {
        private static Funcionario Funcionario(string perfil) => new()
        {
            Id = 1,
            Nome = "Tester",
            Email = "tester@local",
            PerfilAcesso = perfil,
            Ativo = true
        };

        [Fact]
        public void VerificarPermissaoCodigo_QuandoLookupLanca_RetornaUnavailable()
        {
            var svc = new PermissionService(
                Funcionario("Vendedor"),
                _ => throw new InvalidOperationException("db down"));

            var result = svc.VerificarPermissaoCodigo("CLIENTES_EXCLUIR");

            Assert.True(result.IsUnavailable);
            Assert.False(result.IsAllowed);
        }

        [Fact]
        public void GarantirPermissaoCritica_QuandoUnavailable_RetornaFalse_FailClosed()
        {
            var svc = new PermissionService(
                Funcionario("Vendedor"),
                _ => throw new InvalidOperationException("db down"));

            Assert.False(svc.GarantirPermissaoCritica("CLIENTES_EXCLUIR"));
            Assert.False(svc.TemPermissaoCodigo("CLIENTES_EXCLUIR"));
        }

        [Fact]
        public void VerificarPermissaoCodigo_QuandoPersistidoNegado_RetornaDenied_SemFallback()
        {
            var svc = new PermissionService(
                Funcionario("Gerente"),
                _ => false);

            var result = svc.VerificarPermissaoCodigo("CLIENTES_EXCLUIR");

            Assert.True(result.IsDenied);
            Assert.Equal("persistido_negado", result.Detail);
            Assert.False(svc.GarantirPermissaoCritica("CLIENTES_EXCLUIR"));
        }

        [Fact]
        public void VerificarPermissaoCodigo_QuandoPersistidoPermitido_RetornaAllowed()
        {
            var svc = new PermissionService(
                Funcionario("Vendedor"),
                code => code == "CLIENTES_EXCLUIR" ? true : null);

            var result = svc.VerificarPermissaoCodigo("CLIENTES_EXCLUIR");

            Assert.True(result.IsAllowed);
            Assert.True(svc.GarantirPermissaoCritica("CLIENTES_EXCLUIR"));
        }

        [Fact]
        public void Administrador_SempreAllowed()
        {
            var svc = new PermissionService(
                Funcionario("Administrador"),
                _ => throw new Exception("nao deve consultar"));

            Assert.True(svc.VerificarPermissaoCodigo("CLIENTES_EXCLUIR").IsAllowed);
            Assert.True(svc.GarantirPermissaoCritica("CLIENTES_EXCLUIR"));
        }

        [Fact]
        public void IsCriticalPermission_CobreExclusaoAjusteFinanceiro()
        {
            Assert.True(PermissionService.IsCriticalPermission("CLIENTES_EXCLUIR"));
            Assert.True(PermissionService.IsCriticalPermission("ESTOQUE_AJUSTAR"));
            Assert.True(PermissionService.IsCriticalPermission("FINANCEIRO_EDITAR"));
            Assert.False(PermissionService.IsCriticalPermission("CLIENTES_VER"));
        }

        [Fact]
        public void CriticoSemLinhaPersistida_NaoUsaFallbackPerfil()
        {
            var svc = new PermissionService(
                Funcionario("Gerente"),
                _ => null); // sem linha

            var result = svc.VerificarPermissaoCodigo("PDV_APLICAR_DESCONTO");
            Assert.True(result.IsDenied);
            Assert.Equal("critico_sem_linha_persistida", result.Detail);
            Assert.False(svc.TemPermissaoCodigo("PDV_APLICAR_DESCONTO"));
            Assert.False(svc.GarantirPermissaoCritica("CAIXA_ABRIR"));
        }

        [Fact]
        public void IsCriticalPermission_IncluiPdvCaixaOsOrcamento()
        {
            Assert.True(PermissionService.IsCriticalPermission("PDV_APLICAR_DESCONTO"));
            Assert.True(PermissionService.IsCriticalPermission("CAIXA_SANGRIA"));
            Assert.True(PermissionService.IsCriticalPermission("ORDENS_SERVICO_EXCLUIR"));
            Assert.True(PermissionService.IsCriticalPermission("ORCAMENTOS_EXCLUIR"));
        }
    }
}
