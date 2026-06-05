using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.Repositories
{
    public sealed class RepositoryRegistry
    {
        public RepositoryRegistry(DatabaseService databaseService, LoggerService logger)
        {
            Produtos = new ProdutoRepository(databaseService.GetConnection, logger);
            Clientes = new ClienteRepository(databaseService.GetConnection, databaseService.ObterHistoricoServicosPorClienteId, logger);
            Fornecedores = new FornecedorRepository(databaseService.GetConnection, logger);
            Funcionarios = new FuncionarioRepository(databaseService.GetConnection, logger);
            OrdensServico = new OrdemServicoRepository(databaseService.GetConnection, logger);
        }

        public IProdutoRepository Produtos { get; }
        public IClienteRepository Clientes { get; }
        public IFornecedorRepository Fornecedores { get; }
        public IFuncionarioRepository Funcionarios { get; }
        public IOrdemServicoRepository OrdensServico { get; }
    }
}
