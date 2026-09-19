using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PrimoAutoEletrica.Models;

namespace PrimoAutoEletrica.Services
{
    public sealed class OrcamentoAprovacaoRegistro
    {
        public Guid OrcamentoId { get; set; }
        public string Numero { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime CriadoEm { get; set; }
        public DateTime ExpiraEm { get; set; }
        public DateTime? AprovadoEm { get; set; }
        public string? AprovadoPor { get; set; }
        public string Status { get; set; } = "Pendente";
    }

    public sealed class OrcamentoAprovacaoService
    {
        private static string Arquivo()
        {
            var dir = Path.Combine(App.RuntimeAppDataPath, "Comercial");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "aprovacoes-orcamento.json");
        }

        public OrcamentoAprovacaoRegistro GerarToken(Orcamento orcamento, int validadeHoras = 72)
        {
            ArgumentNullException.ThrowIfNull(orcamento);
            var lista = Carregar();
            lista.RemoveAll(x => x.OrcamentoId == orcamento.Id && x.Status == "Pendente");
            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(8));
            var reg = new OrcamentoAprovacaoRegistro
            {
                OrcamentoId = orcamento.Id,
                Numero = orcamento.Numero,
                Token = token,
                CriadoEm = DateTime.Now,
                ExpiraEm = DateTime.Now.AddHours(Math.Max(1, validadeHoras)),
                Status = "Pendente"
            };
            lista.Add(reg);
            Salvar(lista);
            return reg;
        }

        public OrcamentoAprovacaoRegistro? ObterPendente(Guid orcamentoId) =>
            Carregar().Where(x => x.OrcamentoId == orcamentoId && x.Status == "Pendente")
                .OrderByDescending(x => x.CriadoEm)
                .FirstOrDefault();

        public IReadOnlyList<OrcamentoAprovacaoRegistro> Listar(Guid? orcamentoId = null)
        {
            var lista = Carregar();
            if (orcamentoId.HasValue)
            {
                return lista.Where(x => x.OrcamentoId == orcamentoId.Value)
                    .OrderByDescending(x => x.CriadoEm)
                    .ToList();
            }

            return lista.OrderByDescending(x => x.CriadoEm).Take(200).ToList();
        }

        public bool RegistrarRecusa(string token, string? motivo, out string mensagem)
        {
            mensagem = string.Empty;
            if (string.IsNullOrWhiteSpace(token))
            {
                mensagem = "Informe o token.";
                return false;
            }

            var lista = Carregar();
            var reg = lista.FirstOrDefault(x =>
                string.Equals(x.Token, token.Trim(), StringComparison.OrdinalIgnoreCase));
            if (reg == null)
            {
                mensagem = "Token nao encontrado.";
                return false;
            }

            if (reg.Status is "Aprovado" or "Recusado")
            {
                mensagem = $"Token ja finalizado ({reg.Status}).";
                return false;
            }

            reg.Status = "Recusado";
            reg.AprovadoEm = DateTime.Now;
            reg.AprovadoPor = string.IsNullOrWhiteSpace(motivo) ? "Cliente (recusa local)" : motivo.Trim();
            Salvar(lista);

            try
            {
                var db = new OrcamentoDatabaseService();
                var orc = db.ObterOrcamentoPorId(reg.OrcamentoId);
                if (orc != null)
                {
                    orc.Status = "Recusado";
                    db.AtualizarOrcamento(orc);
                }
            }
            catch (Exception ex)
            {
                mensagem = "Recusa registrada, mas falhou ao atualizar orcamento: " + ex.Message;
                return true;
            }

            mensagem = $"Orcamento {reg.Numero} recusado via token.";
            return true;
        }

        public bool RegistrarAprovacao(string token, string? aprovadoPor, out string mensagem)
        {
            mensagem = string.Empty;
            if (string.IsNullOrWhiteSpace(token))
            {
                mensagem = "Informe o token.";
                return false;
            }

            var lista = Carregar();
            var reg = lista.FirstOrDefault(x =>
                string.Equals(x.Token, token.Trim(), StringComparison.OrdinalIgnoreCase));
            if (reg == null)
            {
                mensagem = "Token nao encontrado.";
                return false;
            }

            if (reg.Status == "Aprovado")
            {
                mensagem = "Token ja utilizado.";
                return false;
            }

            if (reg.ExpiraEm < DateTime.Now)
            {
                reg.Status = "Expirado";
                Salvar(lista);
                mensagem = "Token expirado.";
                return false;
            }

            reg.Status = "Aprovado";
            reg.AprovadoEm = DateTime.Now;
            reg.AprovadoPor = string.IsNullOrWhiteSpace(aprovadoPor) ? "Cliente (token local)" : aprovadoPor.Trim();
            Salvar(lista);

            try
            {
                var db = new OrcamentoDatabaseService();
                var orc = db.ObterOrcamentoPorId(reg.OrcamentoId);
                if (orc != null)
                {
                    orc.Status = "Aprovado";
                    orc.DataAprovacao = DateTime.Now;
                    db.AtualizarOrcamento(orc);
                }
            }
            catch (Exception ex)
            {
                mensagem = "Token ok, mas falhou ao atualizar orcamento: " + ex.Message;
                return true;
            }

            mensagem = $"Orcamento {reg.Numero} aprovado via token.";
            return true;
        }

        public string MontarMensagemWhatsApp(Orcamento orcamento, OrcamentoAprovacaoRegistro reg)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Orcamento {orcamento.Numero} - total {orcamento.Total:C}");
            sb.AppendLine($"Validade do token: {reg.ExpiraEm:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"Token de aprovacao: {reg.Token}");
            sb.AppendLine("Responda com o token para confirmarmos a aprovacao (fluxo local Primox).");
            return sb.ToString();
        }

        private static List<OrcamentoAprovacaoRegistro> Carregar()
        {
            var path = Arquivo();
            if (!File.Exists(path))
            {
                return new List<OrcamentoAprovacaoRegistro>();
            }

            try
            {
                var json = File.ReadAllText(path, Encoding.UTF8);
                return JsonSerializer.Deserialize<List<OrcamentoAprovacaoRegistro>>(json)
                       ?? new List<OrcamentoAprovacaoRegistro>();
            }
            catch
            {
                return new List<OrcamentoAprovacaoRegistro>();
            }
        }

        private static void Salvar(List<OrcamentoAprovacaoRegistro> lista)
        {
            var json = JsonSerializer.Serialize(lista, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Arquivo(), json, Encoding.UTF8);
        }
    }
}
