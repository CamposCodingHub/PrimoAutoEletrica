using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.ViewModels;
using System;
using System.Collections.Generic;
using Xunit;

namespace PrimoAutoEletrica.Tests.ViewModelTests
{
    public class ClientesViewModelTests
    {
        [Fact]
        public void ApplyFilters_FiltersByNameAndStatus()
        {
            var viewModel = new ClientesViewModel
            {
                AllClientes = new List<ClienteListItemViewModel>
                {
                    new ClienteListItemViewModel
                    {
                        Cliente = new Cliente { Ativo = true, TipoPessoaDescricao = "Fisica", Documento = "11111111111", UltimaVisita = DateTime.Today, ClienteVip = false, ConsentimentoLGPD = true, AutorizaContatoWhatsApp = true },
                        Nome = "Ana Maria",
                        TipoPessoaResumo = "Fisica",
                        DocumentoResumo = "11111111111",
                        ContatoPrincipal = "(11) 90000-0000",
                        VeiculoPrincipal = "Fiat Uno",
                        UltimaVisita = DateTime.Today.ToString("dd/MM/yyyy"),
                        TicketMedio = "R$ 300,00",
                        StatusResumo = "Ativo",
                        LgpdResumo = "LGPD + WhatsApp",
                        ImagemUrl = string.Empty,
                        Iniciais = "AM"
                    },
                    new ClienteListItemViewModel
                    {
                        Cliente = new Cliente { Ativo = false, TipoPessoaDescricao = "Juridica", Documento = "22222222222", UltimaVisita = DateTime.Today.AddDays(-120), ClienteVip = false, ConsentimentoLGPD = false, AutorizaContatoWhatsApp = false },
                        Nome = "Carlos Silva",
                        TipoPessoaResumo = "Juridica",
                        DocumentoResumo = "22222222222",
                        ContatoPrincipal = "(21) 90000-0001",
                        VeiculoPrincipal = "Ford Ka",
                        UltimaVisita = DateTime.Today.AddDays(-120).ToString("dd/MM/yyyy"),
                        TicketMedio = "R$ 150,00",
                        StatusResumo = "Inativo",
                        LgpdResumo = "LGPD pendente",
                        ImagemUrl = string.Empty,
                        Iniciais = "CS"
                    }
                }
            };

            viewModel.ApplyFilters("Ana", "Ativos");

            Assert.Single(viewModel.FilteredClientes);
            Assert.Equal("Ana Maria", viewModel.FilteredClientes[0].Nome);
        }

        [Fact]
        public void ApplyFilters_FiltersByVipStatus()
        {
            var viewModel = new ClientesViewModel
            {
                AllClientes = new List<ClienteListItemViewModel>
                {
                    new ClienteListItemViewModel
                    {
                        Cliente = new Cliente { Ativo = true, ClienteVip = true },
                        Nome = "VIP Cliente",
                        TipoPessoaResumo = "Fisica",
                        DocumentoResumo = "33333333333",
                        ContatoPrincipal = "(31) 90000-0002",
                        VeiculoPrincipal = "Honda Civic",
                        UltimaVisita = DateTime.Today.ToString("dd/MM/yyyy"),
                        TicketMedio = "R$ 420,00",
                        StatusResumo = "VIP",
                        LgpdResumo = "LGPD + WhatsApp",
                        ImagemUrl = string.Empty,
                        Iniciais = "VC"
                    },
                    new ClienteListItemViewModel
                    {
                        Cliente = new Cliente { Ativo = true, ClienteVip = false },
                        Nome = "Cliente Normal",
                        TipoPessoaResumo = "Fisica",
                        DocumentoResumo = "44444444444",
                        ContatoPrincipal = "(31) 90000-0003",
                        VeiculoPrincipal = "Chevrolet Onix",
                        UltimaVisita = DateTime.Today.ToString("dd/MM/yyyy"),
                        TicketMedio = "R$ 250,00",
                        StatusResumo = "Ativo",
                        LgpdResumo = "LGPD + WhatsApp",
                        ImagemUrl = string.Empty,
                        Iniciais = "CN"
                    }
                }
            };

            viewModel.ApplyFilters(string.Empty, "VIP");

            Assert.Single(viewModel.FilteredClientes);
            Assert.Equal("VIP Cliente", viewModel.FilteredClientes[0].Nome);
        }
    }
}
