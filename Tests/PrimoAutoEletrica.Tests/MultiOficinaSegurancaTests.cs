using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using PrimoAutoEletrica.Models;
using PrimoAutoEletrica.Services;
using PrimoAutoEletrica.Helpers;
using Xunit;

namespace PrimoAutoEletrica.Tests
{
    public class MultiOficinaSegurancaTests
    {
        [Fact]
        public void PasswordHasher_HashEVerificacao_DevemSerSegurosEPrevenirInvasao()
        {
            var senhaOriginal = "OficinaForte@2026";
            var hash = PasswordHasherService.HashPassword(senhaOriginal);

            Assert.NotNull(hash);
            Assert.StartsWith("PBKDF2$", hash);
            Assert.NotEqual(senhaOriginal, hash);

            // Validação de senha correta
            Assert.True(PasswordHasherService.VerifyPassword(senhaOriginal, hash));

            // Validação de rejeição de senha incorreta
            Assert.False(PasswordHasherService.VerifyPassword("SenhaErrada", hash));
            Assert.False(PasswordHasherService.VerifyPassword(string.Empty, hash));
        }

        [Fact]
        public void StationConfiguration_PadraoEstacao_DeveConterNomeDeMaquinaETipoValido()
        {
            var config = new StationConfiguration
            {
                StationName = "Bancada Diagnóstico 01",
                StationType = StationType.Oficina,
                Description = "Estação com osciloscópio e scanner automotivo"
            };

            Assert.False(string.IsNullOrWhiteSpace(config.MachineName));
            Assert.Equal("Bancada Diagnóstico 01", config.StationName);
            Assert.Equal(StationType.Oficina, config.StationType);
        }

        [Fact]
        public async Task FilialService_AmbienteLocal_DeveRetornarUnidadeEstavelESemDuplicidade()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "primox-test-filial-" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            try
            {
                var logger = new LoggerService(tempDir);
                var db = new DatabaseService(tempDir, logger: logger);
                var filialService = new FilialService(db, logger);

                var filiais = await filialService.CarregarFiliaisAsync();

                Assert.Single(filiais);
                Assert.Equal("LOCAL", filiais[0].Codigo);
                Assert.NotNull(filialService.FilialAtual);
                Assert.False(filialService.DeveExibirSelecaoFilial());
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [Fact]
        public void RegistroBloqueio_SimulacaoDoisOperadoresSimultaneos_DeveBloquearSegundoAcesso()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "primox-test-lock-" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            try
            {
                var logger = new LoggerService(tempDir);
                var db = new DatabaseService(tempDir, logger: logger);

                var session1 = new AppSessionService();
                session1.StartSession(new Funcionario { Id = 1, Nome = "Eletricista Carlos", Email = "carlos@oficina.com" });

                var session2 = new AppSessionService();
                session2.StartSession(new Funcionario { Id = 2, Nome = "Mecânico Roberto", Email = "roberto@oficina.com" });

                var lockService1 = new RegistroBloqueioService(db, session1);
                var lockService2 = new RegistroBloqueioService(db, session2);

                // 1. Carlos bloqueia a Ordem de Serviço OS-2026-0001
                var res1 = lockService1.TentarBloquear("OrdemServico", "OS-2026-0001", "Edição técnica");
                Assert.True(res1.Bloqueado);

                // 2. Roberto tenta bloquear a MESMA Ordem de Serviço simultaneamente
                var res2 = lockService2.TentarBloquear("OrdemServico", "OS-2026-0001", "Tentativa concorrente");
                Assert.False(res2.Bloqueado);
                Assert.Contains("Carlos", res2.Mensagem);

                // 3. Carlos libera o bloqueio
                lockService1.LiberarBloqueio("OrdemServico", "OS-2026-0001");

                // 4. Agora Roberto tenta novamente e deve obter o bloqueio com sucesso
                var res3 = lockService2.TentarBloquear("OrdemServico", "OS-2026-0001", "Agora posso editar");
                Assert.True(res3.Bloqueado);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [Fact]
        public async Task FerramentaService_VerificarPendenciasOS_DeveBloquearFinalizacaoSeHouverFerramentaEmUso()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "primox-test-tools-os-" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            try
            {
                var logger = new LoggerService(tempDir);
                var db = new DatabaseService(tempDir, logger: logger);
                var ferramentaService = new FerramentaService(db, logger);

                var osId = Guid.NewGuid();
                var numOs = "OS-2026-100";

                // Cadastrar ferramenta
                var f = new Ferramenta
                {
                    CodigoPatrimonio = "FER-TEST-01",
                    Nome = "Torquímetro de Estalo 1/2 Pol.",
                    LocalizacaoArmario = "Armário 01 / Gaveta C",
                    Status = StatusFerramenta.Disponivel
                };
                await ferramentaService.SalvarFerramentaAsync(f);

                // Verificar que não há pendências na OS
                var pendenciasAntes = await ferramentaService.ObterFerramentasPendentesOSAsync(osId);
                Assert.Empty(pendenciasAntes);

                // Emprestar ferramenta vinculada à OS
                var funcionarioId = Guid.NewGuid();
                await ferramentaService.RegistrarRetiradaAsync(
                    ferramentaId: f.Id,
                    funcionarioId: funcionarioId,
                    funcionarioNome: "Técnico Silva",
                    ordemServicoId: osId,
                    numeroOS: numOs,
                    previsaoDevolucao: DateTime.Now.AddHours(2)
                );

                // Agora a OS DEVE acusar pendência e impedir finalização
                var pendenciasDepois = await ferramentaService.ObterFerramentasPendentesOSAsync(osId);
                Assert.Single(pendenciasDepois);
                Assert.Equal(f.Id, pendenciasDepois[0].Id);

                // Devolver ferramenta
                await ferramentaService.RegistrarDevolucaoAsync(
                    ferramentaId: f.Id,
                    estadoConservacaoDevolucao: "OK - Perfeitas condições",
                    observacoesDevolucao: "Devolvido em ordem"
                );

                // Pendências limpas após retorno
                var pendenciasFinal = await ferramentaService.ObterFerramentasPendentesOSAsync(osId);
                Assert.Empty(pendenciasFinal);
            }
            finally
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [Fact]
        public void WhatsAppIntegracao_FormatacaoMensagensETelefone_DeveGerarUrlsValidas()
        {
            // 1. Telefone com máscara e DDD deve ser normalizado para 5511988887777
            var telefoneRaw = "(11) 98888-7777";
            var valido = CadastroValidationHelper.TryObterTelefoneWhatsApp(telefoneRaw, out var telefoneNormalizado);
            Assert.True(valido);
            Assert.Equal("5511988887777", telefoneNormalizado);

            // 2. Criação da URL para Veículo Pronto
            var mensagemPronto = "Olá Cliente! Seu veículo Fiat Palio (ABC-1234) está PRONTO PARA RETIRADA na oficina!";
            var urlPronto = OficinaProfissionalService.CriarWhatsAppUrl(telefoneNormalizado, mensagemPronto);
            Assert.StartsWith("https://wa.me/5511988887777?text=", urlPronto);
            Assert.Contains(Uri.EscapeDataString("PRONTO PARA RETIRADA"), urlPronto);

            // 3. Orçamento e Lembrete de Agendamento
            var mensagemOrcamento = "Olá! Segue seu orçamento OS-100 no valor de R$ 350,00.";
            var urlOrcamento = OficinaProfissionalService.CriarWhatsAppUrl(telefoneNormalizado, mensagemOrcamento);
            Assert.StartsWith("https://wa.me/5511988887777?text=", urlOrcamento);
            Assert.Contains(Uri.EscapeDataString("OS-100"), urlOrcamento);
        }
    }
}
