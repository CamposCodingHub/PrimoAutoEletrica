using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PrimoAutoEletrica.ViewModels
{
    public class ClientesViewModel : INotifyPropertyChanged
    {
        private List<ClienteListItemViewModel> _allClientes = new();
        private List<ClienteListItemViewModel> _filteredClientes = new();
        private string _totalClientesText = "0";
        private string _clientesAtivosText = "0";
        private string _clientesFidelizadosText = "0";
        private string _clientesInativosText = "0";
        private List<object> _recentesPanelItems = new();

        public List<ClienteListItemViewModel> AllClientes
        {
            get => _allClientes;
            set => SetField(ref _allClientes, value);
        }

        public List<ClienteListItemViewModel> FilteredClientes
        {
            get => _filteredClientes;
            set => SetField(ref _filteredClientes, value);
        }

        public string TotalClientesText
        {
            get => _totalClientesText;
            set => SetField(ref _totalClientesText, value);
        }

        public string ClientesAtivosText
        {
            get => _clientesAtivosText;
            set => SetField(ref _clientesAtivosText, value);
        }

        public string ClientesFidelizadosText
        {
            get => _clientesFidelizadosText;
            set => SetField(ref _clientesFidelizadosText, value);
        }

        public string ClientesInativosText
        {
            get => _clientesInativosText;
            set => SetField(ref _clientesInativosText, value);
        }

        public List<object> RecentesPanelItems
        {
            get => _recentesPanelItems;
            set => SetField(ref _recentesPanelItems, value);
        }

        public void LoadClients()
        {
            var clientes = App.Repositories.Clientes.ObterTodos()
                .OrderByDescending(c => c.DataCadastro)
                .ToList();

            AllClientes = clientes.Select(MapearCliente).ToList();
            UpdateIndicatorsFromList(clientes);
            UpdatePanelFromList(clientes);
            ApplyFilters(string.Empty, "Todos");
        }

        public void ApplyFilters(string busca, string filtro)
        {
            var filtrados = AllClientes
                .Where(c => CorrespondeFiltro(c, busca ?? string.Empty, filtro ?? "Todos"))
                .OrderByDescending(c => c.Cliente.DataCadastro)
                .ToList();

            FilteredClientes = filtrados;
        }

        private void UpdateIndicatorsFromList(List<Cliente> clientes)
        {
            var ativos = clientes.Count(c => c.Ativo);
            var vip = clientes.Count(c => c.ClienteVip);
            var semRetorno = clientes.Count(c => !c.UltimaVisita.HasValue || (DateTime.Today - c.UltimaVisita.Value.Date).Days > 90);

            TotalClientesText = clientes.Count.ToString();
            ClientesAtivosText = ativos.ToString();
            ClientesFidelizadosText = vip.ToString();
            ClientesInativosText = semRetorno.ToString();
        }

        private void UpdatePanelFromList(List<Cliente> clientes)
        {
            RecentesPanelItems = clientes
                .OrderByDescending(c => c.DataCadastro)
                .Take(4)
                .Select(c => new { c.Nome, Resumo = CriarResumoRecente(c) })
                .ToList<object>();
        }

        private static ClienteListItemViewModel MapearCliente(Cliente cliente)
        {
            var contato = string.IsNullOrWhiteSpace(cliente.Telefone) ? cliente.WhatsApp : cliente.Telefone;
            var veiculoPrincipal = cliente.Veiculos.Count > 0
                ? $"{cliente.Veiculos[0].Marca} {cliente.Veiculos[0].Modelo}".Trim()
                : "Sem frota";
            var diasSemVisita = cliente.UltimaVisita.HasValue
                ? (DateTime.Today - cliente.UltimaVisita.Value.Date).Days
                : int.MaxValue;
            var ticketMedio = cliente.TotalServicos > 0 ? cliente.TotalGasto / cliente.TotalServicos : 0m;

            return new ClienteListItemViewModel
            {
                Cliente = cliente,
                Nome = cliente.Nome,
                TipoPessoaResumo = cliente.TipoPessoaDescricao,
                DocumentoResumo = string.IsNullOrWhiteSpace(cliente.Documento) ? "Documento nao informado" : cliente.Documento,
                ContatoPrincipal = string.IsNullOrWhiteSpace(contato) ? "Sem contato" : contato,
                VeiculoPrincipal = veiculoPrincipal,
                UltimaVisita = cliente.UltimaVisita?.ToString("dd/MM/yyyy") ?? "Sem visita",
                TicketMedio = ticketMedio > 0 ? ticketMedio.ToString("C") : "-",
                StatusResumo = !cliente.Ativo ? "Inativo" : cliente.ClienteVip ? "VIP" : diasSemVisita > 90 ? "Sem retorno" : "Ativo",
                LgpdResumo = cliente.ConsentimentoLGPD ? (cliente.AutorizaContatoWhatsApp ? "LGPD + WhatsApp" : "LGPD sem WhatsApp") : "LGPD pendente",
                ImagemUrl = ClienteMediaService.ResolveExistingPath(cliente.ImagemUrl),
                Iniciais = ObterIniciais(cliente.Nome)
            };
        }

        private static bool CorrespondeFiltro(ClienteListItemViewModel cliente, string busca, string filtro)
        {
            var passaBusca = string.IsNullOrWhiteSpace(busca) ||
                             cliente.Nome.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                             cliente.TipoPessoaResumo.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                             cliente.DocumentoResumo.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                             cliente.ContatoPrincipal.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                             cliente.VeiculoPrincipal.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                             cliente.LgpdResumo.Contains(busca, StringComparison.OrdinalIgnoreCase);

            if (!passaBusca) return false;

            return filtro switch
            {
                "Ativos" => cliente.Cliente.Ativo,
                "VIP" => cliente.Cliente.ClienteVip,
                "Sem retorno" => !cliente.Cliente.UltimaVisita.HasValue || (DateTime.Today - cliente.Cliente.UltimaVisita.Value.Date).Days > 90,
                _ => true
            };
        }

        private static string CriarResumoRecente(Cliente cliente)
        {
            var veiculo = cliente.Veiculos.FirstOrDefault();
            var descricaoVeiculo = veiculo == null ? "Sem veiculo vinculado" : $"{veiculo.Marca} {veiculo.Modelo} • {veiculo.Placa}".Trim();
            return $"{descricaoVeiculo} • cadastro em {cliente.DataCadastro:dd/MM/yyyy}";
        }

        private static string ObterIniciais(string? nome)
        {
            if (string.IsNullOrWhiteSpace(nome)) return "CL";
            var partes = nome.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 1) return partes[0][0].ToString().ToUpperInvariant();
            return string.Concat(partes[0][0], partes[^1][0]).ToUpperInvariant();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}

