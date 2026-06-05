using System;

namespace PrimoAutoEletrica.Models
{
    public class Permissao
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        public string Modulo { get; set; } = string.Empty;
        public string Acao { get; set; } = string.Empty; // Criar, Ler, Atualizar, Excluir, Aprovar, Executar
        public string Codigo { get; set; } = string.Empty; // Ex: DASHBOARD_VER, CLIENTES_CRIAR
        public bool Ativo { get; set; } = true;
        public DateTime DataCriacao { get; set; }
        public bool Essencial { get; set; } = false; // Permissões essenciais não podem ser removidas
        public int OrdemExibicao { get; set; } = 0;
    }

    public enum ModuloSistema
    {
        Dashboard = 1,
        Clientes = 2,
        Veiculos = 3,
        Orcamentos = 4,
        OrdensServico = 5,
        PDV = 6,
        Estoque = 7,
        Financeiro = 8,
        Relatorios = 9,
        Fornecedores = 10,
        Funcionarios = 11,
        Agendamentos = 12,
        Sistema = 13
    }

    public enum TipoAcao
    {
        Ver = 1,
        Criar = 2,
        Editar = 3,
        Excluir = 4,
        Aprovar = 5,
        Executar = 6,
        Gerenciar = 7,
        Configurar = 8
    }
}
