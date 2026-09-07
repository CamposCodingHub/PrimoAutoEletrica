using System;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Linq;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Repositories;
using PrimoAutoEletrica.ViewModels;
using PrimoAutoEletrica.Services;

namespace PrimoAutoEletrica.ViewModels
{
    public class FuncionariosViewModel : BaseViewModel
    {
        private readonly IFuncionarioRepository _funcionarioRepository;
        private readonly FuncionarioOperationalService _funcionarioOperationalService;
        private readonly PermissionService _permissionService;

        private List<Funcionario> _funcionarios = new();
        private string _textoBusca = string.Empty;
        private string _totalFuncionariosText = "0";
        private string _funcionariosAtivosText = "0";
        private string _funcionariosInativosText = "0";

        public ObservableCollection<FuncionarioListItem> FuncionariosFiltrados { get; } = new();

        public string TextoBusca
        {
            get => _textoBusca;
            set => SetField(ref _textoBusca, value);
        }

        public string TotalFuncionariosText
        {
            get => _totalFuncionariosText;
            set => SetField(ref _totalFuncionariosText, value);
        }

        public string FuncionariosAtivosText
        {
            get => _funcionariosAtivosText;
            set => SetField(ref _funcionariosAtivosText, value);
        }

        public string FuncionariosInativosText
        {
            get => _funcionariosInativosText;
            set => SetField(ref _funcionariosInativosText, value);
        }

        public FuncionariosViewModel(IFuncionarioRepository funcionarioRepository, Funcionario funcionarioLogado)
        {
            _funcionarioRepository = funcionarioRepository;
            _funcionarioOperationalService = new FuncionarioOperationalService(App.Database);
            _permissionService = new PermissionService(funcionarioLogado, App.Logger, App.Database);
        }

        public void CarregarFuncionarios()
        {
            try
            {
                _funcionarios = _funcionarioRepository.ObterTodos().ToList();
                AplicarFiltros();
                AtualizarEstatisticas();
            }
            catch (Exception ex)
            {
                App.Logger.LogError("Erro ao carregar funcionários", ex);
            }
        }

        public void FiltrarFuncionarios()
        {
            AplicarFiltros();
            AtualizarEstatisticas();
        }

        private void AplicarFiltros()
        {
            FuncionariosFiltrados.Clear();

            var filtrados = _funcionarios.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(TextoBusca))
            {
                var termos = TextoBusca.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                filtrados = filtrados.Where(f => termos.All(termo =>
    f.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
    f.Email.Contains(termo, StringComparison.OrdinalIgnoreCase) ||
    (f.CPF != null && f.CPF.Contains(termo, StringComparison.OrdinalIgnoreCase))
));
            }

            foreach (var funcionario in filtrados.Select(CriarListItem))
            {
                FuncionariosFiltrados.Add(funcionario);
            }
        }

        private void AtualizarEstatisticas()
        {
            TotalFuncionariosText = _funcionarios.Count.ToString("N0");
            FuncionariosAtivosText = _funcionarios.Count(f => f.Ativo).ToString("N0");
            FuncionariosInativosText = _funcionarios.Count(f => !f.Ativo).ToString("N0");
        }

        private FuncionarioListItem CriarListItem(Funcionario funcionario)
        {
            return new FuncionarioListItem
            {
                Id = funcionario.Id,
                Nome = funcionario.Nome,
                Email = funcionario.Email,
                Cpf = funcionario.CPF,
                Telefone = funcionario.Telefone,
                Cargo = funcionario.Cargo,
                PerfilAcesso = funcionario.PerfilAcesso,
                Ativo = funcionario.Ativo,
                DataAdmissao = funcionario.DataAdmissao,
                Salario = funcionario.Salario,
                StatusTexto = funcionario.Ativo ? "Ativo" : "Inativo",
                StatusCor = funcionario.Ativo ? "Green" : "Red"
            };
        }

        public bool TemPermissaoCriar()
        {
            return _permissionService.TemPermissaoCodigo("FUNCIONARIOS_CRIAR");
        }

        public bool TemPermissaoEditar()
        {
            return _permissionService.TemPermissaoCodigo("FUNCIONARIOS_EDITAR");
        }

        public bool TemPermissaoExcluir()
        {
            return _permissionService.TemPermissaoCodigo("FUNCIONARIOS_EXCLUIR");
        }

        // Property change handling is provided by BaseViewModel (ObservableObject)
        // The SetField method is retained for compatibility, delegating to SetProperty from ObservableObject
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            return SetProperty(ref field, value, propertyName);
        }
    }

    public class FuncionarioListItem
    {
        public int Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Cpf { get; set; }
        public string? Telefone { get; set; }
        public string? Cargo { get; set; }
        public string? PerfilAcesso { get; set; }
        public bool Ativo { get; set; }
        public DateTime? DataAdmissao { get; set; }
        public decimal? Salario { get; set; }
        public string StatusTexto { get; set; } = string.Empty;
        public string StatusCor { get; set; } = string.Empty;
    }
}