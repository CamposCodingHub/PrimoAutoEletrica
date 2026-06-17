using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using Xunit;

namespace PrimoAutoEletrica.Tests;

public sealed class AccessControlPolicyTests : IDisposable
{
    private readonly string _tempRoot;

    public AccessControlPolicyTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"primo-access-control-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public void MatrizPersistida_DeveConcederAcoesPorPerfilSemVazarModulosSensiveis()
    {
        var database = new DatabaseService(_tempRoot, logger: new LoggerService());
        var permissoes = database.ObterPermissoes(incluirInativas: false)
            .ToDictionary(permissao => permissao.Codigo, StringComparer.OrdinalIgnoreCase);
        var perfis = database.ObterPerfisAcesso(incluirInativos: false)
            .ToDictionary(perfil => perfil.Nome, StringComparer.OrdinalIgnoreCase);

        AssertPerfilTemTodasAsPermissoes(database, perfis["Administrador"], permissoes.Values);

        AssertPerfilTem(database, perfis["Gerente"], permissoes, "ESTOQUE_EDITAR", "PDV_APLICAR_DESCONTO", "RELATORIOS_EXPORTAR", "CLIENTES_EXPORTAR", "VEICULOS_EXPORTAR");
        AssertPerfilNaoTem(database, perfis["Gerente"], permissoes, "SISTEMA_CONFIGURAR");

        AssertPerfilTem(database, perfis["Vendedor"], permissoes, "CLIENTES_CRIAR", "ORCAMENTOS_CONVERTER_VENDA", "AGENDAMENTOS_EXPORTAR");
        AssertPerfilNaoTem(database, perfis["Vendedor"], permissoes, "FINANCEIRO_VER", "PDV_REGISTRAR_VENDA", "CLIENTES_EXPORTAR", "VEICULOS_EXPORTAR");

        AssertPerfilTem(database, perfis["Caixa"], permissoes, "PDV_REGISTRAR_VENDA", "PDV_CANCELAR_VENDA_REGISTRADA", "FINANCEIRO_EXPORTAR");
        AssertPerfilNaoTem(database, perfis["Caixa"], permissoes, "ESTOQUE_AJUSTAR_PRECO");

        AssertPerfilTem(database, perfis["Almoxarife"], permissoes, "ESTOQUE_CRIAR", "CATALOGO_IMPORTAR", "FORNECEDORES_EDITAR");
        AssertPerfilNaoTem(database, perfis["Almoxarife"], permissoes, "FUNCIONARIOS_EDITAR");
    }

    private static void AssertPerfilTemTodasAsPermissoes(DatabaseService database, PerfilAcesso perfil, IEnumerable<Permissao> permissoes)
    {
        var idsPerfil = database.ObterPermissoesDoPerfil(perfil.Id);

        foreach (var permissao in permissoes)
        {
            Assert.Contains(permissao.Id, idsPerfil);
        }
    }

    private static void AssertPerfilTem(
        DatabaseService database,
        PerfilAcesso perfil,
        IReadOnlyDictionary<string, Permissao> permissoes,
        params string[] codigos)
    {
        var idsPerfil = database.ObterPermissoesDoPerfil(perfil.Id);

        foreach (var codigo in codigos)
        {
            Assert.True(idsPerfil.Contains(permissoes[codigo].Id), $"Perfil {perfil.Nome} deveria conter {codigo}.");
        }
    }

    private static void AssertPerfilNaoTem(
        DatabaseService database,
        PerfilAcesso perfil,
        IReadOnlyDictionary<string, Permissao> permissoes,
        params string[] codigos)
    {
        var idsPerfil = database.ObterPermissoesDoPerfil(perfil.Id);

        foreach (var codigo in codigos)
        {
            Assert.False(idsPerfil.Contains(permissoes[codigo].Id), $"Perfil {perfil.Nome} nao deveria conter {codigo}.");
        }
    }

    public void Dispose()
    {
        try
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // Cleanup failure should not hide the assertion result.
        }
    }
}
