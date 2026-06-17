using PrimoAutoEletrica.Models;
using System;

namespace PrimoAutoEletrica.Services
{
    public partial class DatabaseService
    {
        public bool EmailFuncionarioExiste(string email, int? ignorarFuncionarioId = null)
        {
            return global::PrimoAutoEletrica.App.Repositories.Funcionarios.EmailExiste(email, ignorarFuncionarioId);
        }

        public int SalvarFuncionario(Funcionario funcionario)
        {
            return global::PrimoAutoEletrica.App.Repositories.Funcionarios.Salvar(funcionario);
        }

        public void AtualizarFuncionario(Funcionario funcionario)
        {
            global::PrimoAutoEletrica.App.Repositories.Funcionarios.Atualizar(funcionario);
        }

        public void ResetarSenhaFuncionario(int funcionarioId, string senhaHash)
        {
            global::PrimoAutoEletrica.App.Repositories.Funcionarios.ResetarSenha(funcionarioId, senhaHash);
        }

        public void AtualizarStatusAcessoFuncionario(int funcionarioId, bool ativo, string status)
        {
            global::PrimoAutoEletrica.App.Repositories.Funcionarios.AtualizarStatusAcesso(funcionarioId, ativo, status);
        }

        public void ExcluirFuncionario(int funcionarioId)
        {
            global::PrimoAutoEletrica.App.Repositories.Funcionarios.Excluir(funcionarioId);
        }
    }
}
