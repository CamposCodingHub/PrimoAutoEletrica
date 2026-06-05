using System;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;

namespace PrimoAutoEletrica.Helpers
{
    public static class CadastroValidationHelper
    {
        private static readonly Regex PlacaAntigaRegex = new("^[A-Z]{3}[0-9]{4}$", RegexOptions.Compiled);
        private static readonly Regex PlacaMercosulRegex = new("^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$", RegexOptions.Compiled);

        public static string NormalizarDocumento(string? valor)
        {
            return SomenteDigitos(valor);
        }

        public static string NormalizarTelefone(string? valor)
        {
            var digitos = SomenteDigitos(valor);

            if (digitos.StartsWith("55", StringComparison.Ordinal) &&
                (digitos.Length == 12 || digitos.Length == 13))
            {
                digitos = digitos[2..];
            }

            return digitos;
        }

        public static string NormalizarEmail(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? string.Empty
                : valor.Trim().ToLowerInvariant();
        }

        public static string NormalizarPlaca(string? valor)
        {
            return new string((valor ?? string.Empty)
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());
        }

        public static string NormalizarTextoComparacao(string? valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return string.Empty;
            }

            var decomposed = valor.Trim().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);

            foreach (var caractere in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(caractere) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(char.ToLowerInvariant(caractere));
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        public static string? ValidarCpf(string? cpf, string nomeCampo = "CPF", bool obrigatorio = false)
        {
            var documento = NormalizarDocumento(cpf);
            if (string.IsNullOrWhiteSpace(documento))
            {
                return obrigatorio ? $"Informe o {nomeCampo}." : null;
            }

            return EhCpfValido(documento)
                ? null
                : $"Informe um {nomeCampo} valido.";
        }

        public static string? ValidarCnpj(string? cnpj, string nomeCampo = "CNPJ", bool obrigatorio = false)
        {
            var documento = NormalizarDocumento(cnpj);
            if (string.IsNullOrWhiteSpace(documento))
            {
                return obrigatorio ? $"Informe o {nomeCampo}." : null;
            }

            return EhCnpjValido(documento)
                ? null
                : $"Informe um {nomeCampo} valido.";
        }

        public static string? ValidarCpfOuCnpj(string? documento, string nomeCampo = "CPF ou CNPJ", bool obrigatorio = false)
        {
            var documentoNormalizado = NormalizarDocumento(documento);
            if (string.IsNullOrWhiteSpace(documentoNormalizado))
            {
                return obrigatorio ? $"Informe o {nomeCampo}." : null;
            }

            return documentoNormalizado.Length switch
            {
                11 when EhCpfValido(documentoNormalizado) => null,
                14 when EhCnpjValido(documentoNormalizado) => null,
                _ => $"Informe um {nomeCampo} valido."
            };
        }

        public static string? ValidarEmail(string? email, string nomeCampo = "e-mail", bool obrigatorio = false)
        {
            var emailNormalizado = NormalizarEmail(email);
            if (string.IsNullOrWhiteSpace(emailNormalizado))
            {
                return obrigatorio ? $"Informe o {nomeCampo}." : null;
            }

            try
            {
                _ = new MailAddress(emailNormalizado);
                return null;
            }
            catch (FormatException)
            {
                return $"Informe um {nomeCampo} valido.";
            }
        }

        public static string? ValidarTelefone(string? telefone, string nomeCampo = "telefone", bool obrigatorio = false)
        {
            var telefoneNormalizado = NormalizarTelefone(telefone);
            if (string.IsNullOrWhiteSpace(telefoneNormalizado))
            {
                return obrigatorio ? $"Informe o {nomeCampo}." : null;
            }

            return telefoneNormalizado.Length is >= 10 and <= 11
                ? null
                : $"Informe um {nomeCampo} valido com DDD.";
        }

        public static string? ValidarPlaca(string? placa, string nomeCampo = "placa", bool obrigatorio = false)
        {
            var placaNormalizada = NormalizarPlaca(placa);
            if (string.IsNullOrWhiteSpace(placaNormalizada))
            {
                return obrigatorio ? $"Informe a {nomeCampo}." : null;
            }

            return EhPlacaValida(placaNormalizada)
                ? null
                : $"Informe uma {nomeCampo} valida no padrao brasileiro.";
        }

        public static string? ValidarDataNaoFutura(DateTime? data, string descricao, bool obrigatorio = false)
        {
            if (!data.HasValue)
            {
                return obrigatorio ? $"Informe {descricao}." : null;
            }

            return data.Value.Date <= DateTime.Today
                ? null
                : $"{descricao} nao pode estar no futuro.";
        }

        public static string? ValidarIntervaloDatas(
            DateTime? dataInicial,
            DateTime? dataFinal,
            string descricaoInicial,
            string descricaoFinal,
            bool obrigatorioInicial = false,
            bool obrigatorioFinal = false)
        {
            if (!dataInicial.HasValue)
            {
                return obrigatorioInicial ? $"Informe {descricaoInicial}." : null;
            }

            if (!dataFinal.HasValue)
            {
                return obrigatorioFinal ? $"Informe {descricaoFinal}." : null;
            }

            return dataFinal.Value.Date >= dataInicial.Value.Date
                ? null
                : $"{descricaoFinal} nao pode ser anterior a {descricaoInicial}.";
        }

        public static string? ValidarDecimal(decimal valor, string descricao, bool permitirZero = true, bool permitirNegativo = false)
        {
            if (!permitirNegativo && valor < 0)
            {
                return $"Informe {descricao} maior ou igual a zero.";
            }

            if (!permitirZero && valor <= 0)
            {
                return $"Informe {descricao} maior que zero.";
            }

            return null;
        }

        public static string? ValidarInteiro(int valor, string descricao, bool permitirZero = true, bool permitirNegativo = false)
        {
            if (!permitirNegativo && valor < 0)
            {
                return $"Informe {descricao} maior ou igual a zero.";
            }

            if (!permitirZero && valor <= 0)
            {
                return $"Informe {descricao} maior que zero.";
            }

            return null;
        }

        public static bool TryObterTelefoneWhatsApp(string? telefone, out string telefoneWhatsApp)
        {
            telefoneWhatsApp = string.Empty;

            var erro = ValidarTelefone(telefone);
            if (!string.IsNullOrWhiteSpace(erro))
            {
                return false;
            }

            var telefoneNormalizado = NormalizarTelefone(telefone);
            telefoneWhatsApp = telefoneNormalizado.StartsWith("55", StringComparison.Ordinal)
                ? telefoneNormalizado
                : $"55{telefoneNormalizado}";

            return true;
        }

        public static bool EhCpfValido(string? cpf)
        {
            var documento = NormalizarDocumento(cpf);
            if (documento.Length != 11 || TodosCaracteresIguais(documento))
            {
                return false;
            }

            var digito1 = CalcularDigitoCpf(documento, 9);
            var digito2 = CalcularDigitoCpf(documento, 10);
            return documento[9] == digito1 && documento[10] == digito2;
        }

        public static bool EhCnpjValido(string? cnpj)
        {
            var documento = NormalizarDocumento(cnpj);
            if (documento.Length != 14 || TodosCaracteresIguais(documento))
            {
                return false;
            }

            var digito1 = CalcularDigitoCnpj(documento, 12);
            var digito2 = CalcularDigitoCnpj(documento, 13);
            return documento[12] == digito1 && documento[13] == digito2;
        }

        public static bool EhPlacaValida(string? placa)
        {
            var placaNormalizada = NormalizarPlaca(placa);
            return PlacaAntigaRegex.IsMatch(placaNormalizada) || PlacaMercosulRegex.IsMatch(placaNormalizada);
        }

        public static string FormatarDocumento(string? valor)
        {
            var digitos = NormalizarDocumento(valor);
            if (string.IsNullOrWhiteSpace(digitos))
            {
                return string.Empty;
            }

            return digitos.Length > 11
                ? FormatarSequencia(digitos[..Math.Min(digitos.Length, 14)], 2, 5, 8, 12, ".", ".", "/", "-")
                : FormatarSequencia(digitos[..Math.Min(digitos.Length, 11)], 3, 6, 9, ".", ".", "-");
        }

        public static string FormatarTelefone(string? valor)
        {
            var digitos = NormalizarTelefone(valor);
            if (string.IsNullOrWhiteSpace(digitos))
            {
                return string.Empty;
            }

            digitos = digitos[..Math.Min(digitos.Length, 11)];
            if (digitos.Length <= 2)
            {
                return $"({digitos}";
            }

            var ddd = digitos[..2];
            var restante = digitos[2..];

            if (restante.Length <= 4)
            {
                return $"({ddd}) {restante}";
            }

            var tamanhoPrefixo = restante.Length > 8 ? 5 : 4;
            tamanhoPrefixo = Math.Min(tamanhoPrefixo, restante.Length);
            var prefixo = restante[..tamanhoPrefixo];
            var sufixo = restante.Length > tamanhoPrefixo ? restante[tamanhoPrefixo..] : string.Empty;
            return string.IsNullOrWhiteSpace(sufixo)
                ? $"({ddd}) {prefixo}"
                : $"({ddd}) {prefixo}-{sufixo}";
        }

        private static string SomenteDigitos(string? valor)
        {
            return new string((valor ?? string.Empty).Where(char.IsDigit).ToArray());
        }

        private static bool TodosCaracteresIguais(string valor)
        {
            return valor.All(caractere => caractere == valor[0]);
        }

        private static char CalcularDigitoCpf(string cpf, int tamanhoBase)
        {
            var soma = 0;
            var peso = tamanhoBase + 1;

            for (var indice = 0; indice < tamanhoBase; indice++)
            {
                soma += (cpf[indice] - '0') * (peso - indice);
            }

            var resto = soma % 11;
            return resto < 2 ? '0' : (char)('0' + (11 - resto));
        }

        private static char CalcularDigitoCnpj(string cnpj, int tamanhoBase)
        {
            var pesos = tamanhoBase == 12
                ? new[] { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 }
                : new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

            var soma = 0;
            for (var indice = 0; indice < tamanhoBase; indice++)
            {
                soma += (cnpj[indice] - '0') * pesos[indice];
            }

            var resto = soma % 11;
            return resto < 2 ? '0' : (char)('0' + (11 - resto));
        }

        private static string FormatarSequencia(string digitos, int separador1, int separador2, int separador3, string token1, string token2, string token3)
        {
            return FormatarSequencia(digitos, separador1, separador2, separador3, -1, token1, token2, token3, string.Empty);
        }

        private static string FormatarSequencia(
            string digitos,
            int separador1,
            int separador2,
            int separador3,
            int separador4,
            string token1,
            string token2,
            string token3,
            string token4)
        {
            var builder = new StringBuilder(digitos.Length + 4);

            for (var indice = 0; indice < digitos.Length; indice++)
            {
                if (indice == separador1)
                {
                    builder.Append(token1);
                }
                else if (indice == separador2)
                {
                    builder.Append(token2);
                }
                else if (indice == separador3)
                {
                    builder.Append(token3);
                }
                else if (indice == separador4)
                {
                    builder.Append(token4);
                }

                builder.Append(digitos[indice]);
            }

            return builder.ToString();
        }
    }
}
