using PrimoAutoEletrica.Models;
using System.Collections.Generic;

namespace PrimoAutoEletrica.Repositories
{
    public interface IFuncionarioRepository
    {
        List<Funcionario> ObterTodos(bool somenteAtivos = true);
        Funcionario? ObterPorId(int id);
        bool EmailExiste(string email, int? ignorarFuncionarioId = null);
        int Salvar(Funcionario funcionario);
        void Atualizar(Funcionario funcionario);
        void ResetarSenha(int funcionarioId, string senhaHash);
        void AtualizarStatusAcesso(int funcionarioId, bool ativo, string status);
        void Excluir(int funcionarioId);
    }
}
