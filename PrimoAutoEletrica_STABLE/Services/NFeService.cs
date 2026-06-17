using PrimoAutoEletrica.Data.Repositories;
using PrimoAutoEletrica.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimoAutoEletrica.Services
{
    public class NFeService
    {
        private readonly XmlProdutoParser _xmlParser;
        private readonly ProdutoImportacaoService _produtoImportacaoService;
        private readonly ImportacaoRepository _importacaoRepository;

        public NFeService(DatabaseService databaseService)
        {
            _xmlParser = new XmlProdutoParser();
            _produtoImportacaoService = new ProdutoImportacaoService(databaseService);
            _importacaoRepository = new ImportacaoRepository(databaseService);
        }

        public NotaFiscalImportada ImportarXml(string caminhoArquivo, Guid usuarioId, string usuarioNome)
        {
            try
            {
                if (!_xmlParser.IsValidNFeXml(caminhoArquivo))
                {
                    throw new InvalidOperationException("Arquivo XML nao e uma NF-e valida.");
                }

                var nota = PrepararImportacao(caminhoArquivo);
                return ImportarNotaPreparada(nota, usuarioId, usuarioNome);
            }
            catch (Exception ex)
            {
                var nota = new NotaFiscalImportada
                {
                    CaminhoArquivo = caminhoArquivo,
                    Status = StatusImportacaoNota.Erro,
                    Erro = ex.Message,
                    UsuarioId = usuarioId,
                    UsuarioNome = usuarioNome
                };

                RegistrarHistoricoComSeguranca(nota);
                global::PrimoAutoEletrica.App.Audit.RegistrarErro("Fiscal", "FalhaImportacaoNFe", ex, "ImportacaoNFe", caminhoArquivo);
                return nota;
            }
        }

        public List<string> ValidarNota(NotaFiscalImportada nota)
        {
            var erros = new List<string>();

            if (string.IsNullOrWhiteSpace(nota.ChaveAcesso))
            {
                erros.Add("Chave de acesso nao encontrada.");
            }

            if (nota.Produtos.Count == 0)
            {
                erros.Add("Nenhum produto encontrado na nota.");
            }

            if (nota.ValorTotal <= 0)
            {
                erros.Add("Valor total da nota e invalido.");
            }

            if (string.IsNullOrWhiteSpace(nota.Fornecedor.Nome))
            {
                erros.Add("Fornecedor nao identificado.");
            }

            foreach (var produto in nota.Produtos)
            {
                if (string.IsNullOrWhiteSpace(produto.Nome))
                {
                    erros.Add($"Produto sem nome: {produto.Codigo}");
                }

                if (produto.Quantidade <= 0)
                {
                    erros.Add($"Produto com quantidade invalida: {produto.Nome}");
                }

                if (produto.ValorUnitario <= 0)
                {
                    erros.Add($"Produto com valor unitario invalido: {produto.Nome}");
                }
            }

            return erros;
        }

        public NotaFiscalImportada SimularImportacao(string caminhoArquivo)
        {
            try
            {
                return PrepararImportacao(caminhoArquivo);
            }
            catch (Exception ex)
            {
                return new NotaFiscalImportada
                {
                    CaminhoArquivo = caminhoArquivo,
                    Status = StatusImportacaoNota.Erro,
                    Erro = ex.Message
                };
            }
        }

        public void CancelarImportacao(NotaFiscalImportada nota)
        {
            nota.Status = StatusImportacaoNota.Erro;
            nota.Erro = "Importacao cancelada pelo usuario.";
            RegistrarHistoricoComSeguranca(nota);
            global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                "Fiscal",
                "NFeCancelada",
                "ImportacaoNFe",
                nota.ChaveAcesso,
                $"Numero={nota.Numero}; Usuario={nota.UsuarioNome}");
        }

        private static void AtualizarStatusDaImportacao(NotaFiscalImportada nota)
        {
            var produtosIgnorados = nota.Produtos.Count(p => p.Status == StatusImportacao.Ignorado);
            var produtosComErro = nota.Produtos.Count(p => p.Status == StatusImportacao.Erro);

            if (produtosIgnorados == nota.Produtos.Count)
            {
                nota.Status = StatusImportacaoNota.Erro;
                nota.Erro = "Todos os produtos foram ignorados.";
            }
            else if (produtosComErro > 0)
            {
                nota.Status = StatusImportacaoNota.Parcial;
                nota.Erro = $"{produtosComErro} produtos com erro.";
            }
            else
            {
                nota.Status = StatusImportacaoNota.Concluida;
            }
        }

        public NotaFiscalImportada PrepararImportacao(string caminhoArquivo)
        {
            var nota = _xmlParser.ParseXmlNFe(caminhoArquivo);
            nota.CaminhoArquivo = caminhoArquivo;
            nota.Status = StatusImportacaoNota.Pendente;
            _produtoImportacaoService.PrepararConferencia(nota);

            var erros = ValidarNota(nota);
            if (erros.Count > 0)
            {
                nota.Erro = string.Join(" ", erros);
            }

            return nota;
        }

        public NotaFiscalImportada ImportarNotaPreparada(NotaFiscalImportada nota, Guid usuarioId, string usuarioNome)
        {
            try
            {
                nota.UsuarioId = usuarioId;
                nota.UsuarioNome = usuarioNome;
                nota.Status = StatusImportacaoNota.EmProcessamento;
                nota.DataImportacao = DateTime.Now;

                if (_importacaoRepository.VerificarNotaDuplicada(nota.ChaveAcesso))
                {
                    nota.Status = StatusImportacaoNota.Duplicada;
                    nota.Erro = "NF-e ja importada anteriormente.";
                    global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                        "Fiscal",
                        "NFeDuplicada",
                        "ImportacaoNFe",
                        nota.ChaveAcesso,
                        $"Arquivo={nota.CaminhoArquivo}; Usuario={usuarioNome}");
                    return nota;
                }

                _produtoImportacaoService.ProcessarProdutos(nota);
                AtualizarStatusDaImportacao(nota);
                _importacaoRepository.SalvarImportacao(nota);
                RegistrarContaPagarDaImportacao(nota);
                global::PrimoAutoEletrica.App.Audit.RegistrarAcaoCritica(
                    "Fiscal",
                    "NFeImportada",
                    "ImportacaoNFe",
                    nota.ChaveAcesso,
                    $"Numero={nota.Numero}; Fornecedor={nota.Fornecedor.Nome}; Status={nota.Status}; Produtos={nota.Produtos.Count}; Valor={nota.ValorTotal:C}");

                return nota;
            }
            catch (Exception ex)
            {
                nota.Status = StatusImportacaoNota.Erro;
                nota.Erro = ex.Message;
                RegistrarHistoricoComSeguranca(nota);
                global::PrimoAutoEletrica.App.Audit.RegistrarErro("Fiscal", "FalhaImportacaoNFe", ex, "ImportacaoNFe", nota.CaminhoArquivo);
                return nota;
            }
        }

        private void RegistrarContaPagarDaImportacao(NotaFiscalImportada nota)
        {
            if (nota.Status is not (StatusImportacaoNota.Concluida or StatusImportacaoNota.Parcial))
            {
                return;
            }

            try
            {
                new FinanceiroDatabaseService().RegistrarContaPagarNFe(nota);
            }
            catch (Exception ex)
            {
                var mensagem = $"Conta a pagar da NF-e nao foi gerada automaticamente: {ex.Message}";
                nota.PossuiPendenciaProdutos = true;
                nota.Erro = string.IsNullOrWhiteSpace(nota.Erro)
                    ? mensagem
                    : $"{nota.Erro} {mensagem}";
                _importacaoRepository.SalvarImportacao(nota);
                global::PrimoAutoEletrica.App.Audit.RegistrarErro(
                    "Financeiro",
                    "FalhaContaPagarNFe",
                    ex,
                    "ImportacaoNFe",
                    nota.ChaveAcesso);
            }
        }

        private void RegistrarHistoricoComSeguranca(NotaFiscalImportada nota)
        {
            try
            {
                _importacaoRepository.SalvarImportacao(nota);
            }
            catch (Exception ex)
            {
                global::PrimoAutoEletrica.App.Logger.LogError("Falha ao registrar historico da importacao NF-e.", ex);
            }
        }
    }
}
