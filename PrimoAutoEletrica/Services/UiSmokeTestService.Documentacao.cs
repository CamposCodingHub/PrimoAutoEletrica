using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PrimoAutoEletrica.Services
{
    public sealed partial class UiSmokeTestService
    {
        private void RunDocumentacaoEntregaChecks(UiSmokeTestRunResult result)
        {
            RunCheck(result, "Documentacao:ManualUsuarioManualTecnicoRoadmap", () =>
            {
                var docsRoot = ResolveProjectDocsRoot();
                var requiredFiles = new Dictionary<string, string[]>
                {
                    ["ManualUsuario/README.md"] = new[] { "Manual do Usuario", "instalar", "backup", "permissoes" },
                    ["ManualUsuario/01_INSTALACAO_E_PRIMEIRO_ACESSO.md"] = new[] { "Como instalar", "Primeiro acesso", ".NET Desktop Runtime 9" },
                    ["ManualUsuario/02_CADASTROS_CLIENTE_VEICULO_FUNCIONARIO.md"] = new[] { "Como cadastrar cliente", "Como cadastrar veiculo", "Como cadastrar funcionario" },
                    ["ManualUsuario/03_ORCAMENTOS_OS_PDV_CAIXA.md"] = new[] { "Como criar orcamento", "Como converter orcamento em OS", "Como vender no PDV", "Como fechar caixa" },
                    ["ManualUsuario/04_NFE_BACKUP_RESTAURACAO_PERMISSOES.md"] = new[] { "Como importar NF-e", "Como fazer backup", "Como restaurar backup", "Como configurar permissoes" },
                    ["ManualTecnico/README.md"] = new[] { "Manual Tecnico", "estrutura", "validacao completa" },
                    ["ManualTecnico/01_ESTRUTURA_PROJETO.md"] = new[] { "Estrutura do Projeto", "Services", "UserControls" },
                    ["ManualTecnico/02_BANCO_DADOS_SERVICOS.md"] = new[] { "Banco de Dados", "Migracoes", "Servicos principais" },
                    ["ManualTecnico/03_TESTES_PACOTE_RELEASE.md"] = new[] { "Como rodar testes", "Como gerar pacote limpo", "Como criar release" },
                    ["ROADMAP_PRODUTO_VENDAVEL.md"] = new[] { "Multiempresa", "Multiestacao", "Licenciamento", "Planos comerciais futuros" }
                };

                foreach (var required in requiredFiles)
                {
                    var path = Path.Combine(docsRoot, required.Key.Replace('/', Path.DirectorySeparatorChar));
                    if (!File.Exists(path))
                    {
                        throw new InvalidOperationException($"Documento obrigatorio nao encontrado: {required.Key}");
                    }

                    var content = File.ReadAllText(path);
                    var normalizedContent = NormalizeDocText(content);
                    foreach (var term in required.Value)
                    {
                        if (!normalizedContent.Contains(NormalizeDocText(term), StringComparison.OrdinalIgnoreCase))
                        {
                            throw new InvalidOperationException($"Documento {required.Key} nao contem o termo obrigatorio: {term}");
                        }
                    }
                }
            });
        }

        private static string ResolveProjectDocsRoot()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Docs"),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Docs")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "PrimoAutoEletrica", "Docs"))
            };

            var docsRoot = candidates.FirstOrDefault(Directory.Exists);
            if (string.IsNullOrWhiteSpace(docsRoot))
            {
                throw new DirectoryNotFoundException("Pasta PrimoAutoEletrica/Docs nao encontrada para validacao de documentacao.");
            }

            return docsRoot;
        }

        private static string NormalizeDocText(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new System.Text.StringBuilder(normalized.Length);
            foreach (var ch in normalized)
            {
                var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(ch);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
