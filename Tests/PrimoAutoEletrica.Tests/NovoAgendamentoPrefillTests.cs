using System;
using System.Collections.Generic;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.ViewModels;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public sealed class NovoAgendamentoPrefillTests
    {
        [Fact]
        public void PrefillFromCliente_SetsClienteIdAndContactFields()
        {
            var cliente = new Cliente
            {
                Id = Guid.NewGuid(),
                Nome = "Maria Prefill",
                Telefone = "11988887777",
                WhatsApp = "11999998888",
                Email = "maria@example.com",
                CPF = "12345678901",
                Veiculos = new List<Veiculo>
                {
                    new Veiculo
                    {
                        Placa = "ABC1D23",
                        Marca = "Fiat",
                        Modelo = "Uno",
                        Cor = "Prata",
                        Ano = "2018"
                    }
                }
            };

            var vm = new NovoAgendamentoPremiumViewModel();
            vm.PrefillFromCliente(cliente);

            Assert.Equal(cliente.Id, vm.ClienteId);
            Assert.Equal("Maria Prefill", vm.ClienteNome);
            Assert.Equal("11999998888", vm.ClienteTelefone);
            Assert.Equal("maria@example.com", vm.ClienteEmail);
            Assert.Equal("123.456.789-01", vm.ClienteDocumento);
            Assert.Equal("ABC1D23", vm.VeiculoPlaca);
            Assert.Equal("Fiat", vm.VeiculoMarca);
            Assert.Equal("Uno", vm.VeiculoModelo);
            Assert.Equal("Prata", vm.VeiculoCor);
            Assert.Equal("2018", vm.VeiculoAno);
        }

        [Fact]
        public void PrefillFromCliente_UsesPreferredVehicleWhenProvided()
        {
            var cliente = new Cliente
            {
                Id = Guid.NewGuid(),
                Nome = "Joao Prefill",
                Telefone = "11911112222",
                CPF = "12345678901",
                Veiculos = new List<Veiculo>
                {
                    new Veiculo { Placa = "AAA0A00", Marca = "VW", Modelo = "Gol", Ano = "2010" },
                    new Veiculo { Placa = "BBB1B11", Marca = "Ford", Modelo = "Ka", Ano = "2020", Cor = "Azul" }
                }
            };

            var preferido = cliente.Veiculos[1];
            var vm = new NovoAgendamentoPremiumViewModel();
            vm.PrefillFromCliente(cliente, preferido);

            Assert.Equal(cliente.Id, vm.ClienteId);
            Assert.Equal("BBB1B11", vm.VeiculoPlaca);
            Assert.Equal("Ford", vm.VeiculoMarca);
            Assert.Equal("Ka", vm.VeiculoModelo);
            Assert.Equal("Azul", vm.VeiculoCor);
            Assert.Equal("2020", vm.VeiculoAno);
        }

        [Fact]
        public void PrefillFromCliente_NullCliente_DoesNothing()
        {
            var vm = new NovoAgendamentoPremiumViewModel();
            vm.ClienteNome = "Antes";
            vm.ClienteId = Guid.Parse("11111111-1111-1111-1111-111111111111");

            vm.PrefillFromCliente(null!);

            Assert.Equal("Antes", vm.ClienteNome);
            Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), vm.ClienteId);
            Assert.Equal(string.Empty, vm.ClienteTelefone);
            Assert.Equal(string.Empty, vm.VeiculoPlaca);
        }
    }
}
