from pathlib import Path
import re

# === BUG-005 ===
p = Path("PrimoAutoEletrica/Services/AutoEletricaTecnicaService.cs")
text = p.read_text(encoding="utf-8")
old = """            resultados.AddRange(ordens
                .Select(ordem => new { Ordem = ordem, Defeito = NormalizarDefeito(ordem) })
                .Where(item => !string.IsNullOrWhiteSpace(item.Defeito))
                .GroupBy(item => item.Defeito)
                .Where(grupo => grupo.Count() >= 1)
                .OrderByDescending(grupo => grupo.Count())
                .Take(5)
                .Select(grupo => CriarResumo(\"Mesmo defeito\", grupo.Key, grupo.Select(item => item.Ordem), veiculos)));

            resultados.AddRange(ordens
                .SelectMany(ordem => ordem.Itens
                    .Where(item => !string.IsNullOrWhiteSpace(item.Descricao))
                    .Select(item => new { Ordem = ordem, Peca = item.Descricao.Trim() }))
                .GroupBy(item => item.Peca, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(grupo => grupo.Count())
                .Take(5)
                .Select(grupo => CriarResumo(\"Peca que mais falha\", grupo.Key, grupo.Select(item => item.Ordem), veiculos)));

            resultados.AddRange(ordens
                .Where(ordem => ordem.GarantiaValidaAte.HasValue && ordem.GarantiaValidaAte.Value.Date >= DateTime.Today)
                .GroupBy(ordem => string.IsNullOrWhiteSpace(ordem.GarantiaObservacoes) ? \"Servico em garantia\" : ordem.GarantiaObservacoes.Trim())
                .OrderByDescending(grupo => grupo.Count())
                .Take(5)
                .Select(grupo => CriarResumo(\"Garantia ativa\", grupo.Key, grupo, veiculos, emGarantia: true)));

            resultados.AddRange(ordens
                .Where(ordem => ordem.VeiculoId.HasValue && veiculos.ContainsKey(ordem.VeiculoId.Value))
                .Select(ordem => new
                {
                    Ordem = ordem,
                    Veiculo = veiculos[ordem.VeiculoId!.Value],
                    Defeito = NormalizarDefeito(ordem)
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Defeito))
                .GroupBy(item => $\"{item.Veiculo.Marca} {item.Veiculo.Modelo}: {item.Defeito}\")
                .OrderByDescending(grupo => grupo.Count())
                .Take(5)
                .Select(grupo => CriarResumo(\"Defeito por modelo\", grupo.Key, grupo.Select(item => item.Ordem), veiculos)));

            resultados.AddRange(ordens
                .Where(ordem => ObterTempoResolucaoMinutos(ordem) > 0)
                .Select(ordem => new { Ordem = ordem, Defeito = NormalizarDefeito(ordem) })
                .Where(item => !string.IsNullOrWhiteSpace(item.Defeito))
                .GroupBy(item => item.Defeito)
                .OrderByDescending(grupo => grupo.Average(item => ObterTempoResolucaoMinutos(item.Ordem)))
                .Take(5)
                .Select(grupo => CriarResumo(\"Tempo medio de resolucao\", grupo.Key, grupo.Select(item => item.Ordem), veiculos)));"""

new = """            const int limitePainelRecorrencias = 25;

            // Mesmo defeito: recorrencia real (2+), volume e depois ocorrencia mais recente
            // evita perder retornos recentes quando o banco operacional tem muitos historicos legados.
            resultados.AddRange(ordens
                .Select(ordem => new { Ordem = ordem, Defeito = NormalizarDefeito(ordem) })
                .Where(item => !string.IsNullOrWhiteSpace(item.Defeito))
                .GroupBy(item => item.Defeito)
                .Where(grupo => grupo.Count() >= 2)
                .OrderByDescending(grupo => grupo.Count())
                .ThenByDescending(grupo => grupo.Max(item => item.Ordem.DataAbertura))
                .Take(limitePainelRecorrencias)
                .Select(grupo => CriarResumo(\"Mesmo defeito\", grupo.Key, grupo.Select(item => item.Ordem), veiculos)));

            resultados.AddRange(ordens
                .SelectMany(ordem => ordem.Itens
                    .Where(item => !string.IsNullOrWhiteSpace(item.Descricao))
                    .Select(item => new { Ordem = ordem, Peca = item.Descricao.Trim() }))
                .GroupBy(item => item.Peca, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(grupo => grupo.Count())
                .ThenByDescending(grupo => grupo.Max(item => item.Ordem.DataAbertura))
                .Take(limitePainelRecorrencias)
                .Select(grupo => CriarResumo(\"Peca que mais falha\", grupo.Key, grupo.Select(item => item.Ordem), veiculos)));

            resultados.AddRange(ordens
                .Where(ordem => ordem.GarantiaValidaAte.HasValue && ordem.GarantiaValidaAte.Value.Date >= DateTime.Today)
                .GroupBy(ordem => string.IsNullOrWhiteSpace(ordem.GarantiaObservacoes) ? \"Servico em garantia\" : ordem.GarantiaObservacoes.Trim())
                .OrderByDescending(grupo => grupo.Count())
                .ThenByDescending(grupo => grupo.Max(ordem => ordem.DataAbertura))
                .Take(limitePainelRecorrencias)
                .Select(grupo => CriarResumo(\"Garantia ativa\", grupo.Key, grupo, veiculos, emGarantia: true)));

            resultados.AddRange(ordens
                .Where(ordem => ordem.VeiculoId.HasValue && veiculos.ContainsKey(ordem.VeiculoId.Value))
                .Select(ordem => new
                {
                    Ordem = ordem,
                    Veiculo = veiculos[ordem.VeiculoId!.Value],
                    Defeito = NormalizarDefeito(ordem)
                })
                .Where(item => !string.IsNullOrWhiteSpace(item.Defeito))
                .GroupBy(item => $\"{item.Veiculo.Marca} {item.Veiculo.Modelo}: {item.Defeito}\")
                .OrderByDescending(grupo => grupo.Count())
                .ThenByDescending(grupo => grupo.Max(item => item.Ordem.DataAbertura))
                .Take(limitePainelRecorrencias)
                .Select(grupo => CriarResumo(\"Defeito por modelo\", grupo.Key, grupo.Select(item => item.Ordem), veiculos)));

            resultados.AddRange(ordens
                .Where(ordem => ObterTempoResolucaoMinutos(ordem) > 0)
                .Select(ordem => new { Ordem = ordem, Defeito = NormalizarDefeito(ordem) })
                .Where(item => !string.IsNullOrWhiteSpace(item.Defeito))
                .GroupBy(item => item.Defeito)
                .OrderByDescending(grupo => grupo.Average(item => ObterTempoResolucaoMinutos(item.Ordem)))
                .ThenByDescending(grupo => grupo.Max(item => item.Ordem.DataAbertura))
                .Take(limitePainelRecorrencias)
                .Select(grupo => CriarResumo(\"Tempo medio de resolucao\", grupo.Key, grupo.Select(item => item.Ordem), veiculos)));"""

if old not in text:
    raise SystemExit("BUG-005 block not found")
p.write_text(text.replace(old, new, 1), encoding="utf-8")
print("BUG-005 OK")

# === BUG-006 limits ===
p2 = Path("PrimoAutoEletrica/ViewModels/FinanceiroViewModel.cs")
t2 = p2.read_text(encoding="utf-8")
a = "ObterLucroPorOrdemServico(inicio, fim, 8)"
b = "ObterLucroPorProduto(inicio, fim, 8)"
c = "ObterLucroPorServico(inicio, fim, 8)"
if a not in t2 or b not in t2 or c not in t2:
    raise SystemExit("limits not found")
t2 = t2.replace(a, "ObterLucroPorOrdemServico(inicio, fim, 50)", 1)
t2 = t2.replace(b, "ObterLucroPorProduto(inicio, fim, 50)", 1)
t2 = t2.replace(c, "ObterLucroPorServico(inicio, fim, 50)", 1)
p2.write_text(t2, encoding="utf-8")
print("BUG-006 limits OK")

# === BUG-006 SQL filters + referencia ===
p3 = Path("PrimoAutoEletrica/Services/FinanceiroDatabaseService.cs")
t3 = p3.read_text(encoding="utf-8")

# Replace filtroTipo ternary blocks with LIKE 'servi%' after normalizing c-cedilla via char()
pat = re.compile(
    r"var filtroTipo = tipoNormalizado == \"servico\"\r?\n"
    r"\s*\? \"LOWER\(COALESCE\((vi|i)\.Tipo, '[^']*'\)\) IN \('servico', '[^']*'\)\"\r?\n"
    r"\s*: \"LOWER\(COALESCE\(\1\.Tipo, '[^']*'\)\) NOT IN \('servico', '[^']*'\)\";",
    re.M,
)

def repl(m):
    var = m.group(1)
    default = "Produto" if var == "vi" else "Servico"
    return (
        f'var filtroTipo = tipoNormalizado == "servico"\n'
        f'                    ? "REPLACE(REPLACE(LOWER(COALESCE({var}.Tipo, \'{default}\')), char(231), \'c\'), char(199), \'c\') LIKE \'servi%\'"\n'
        f'                    : "REPLACE(REPLACE(LOWER(COALESCE({var}.Tipo, \'{default}\')), char(231), \'c\'), char(199), \'c\') NOT LIKE \'servi%\'";'
    )

t3n, n = pat.subn(repl, t3)
print("filtroTipo replacements", n)
if n != 2:
    # dump candidates
    for i, line in enumerate(t3.splitlines(), 1):
        if "filtroTipo" in line:
            print(i, repr(line))
            for j in range(i, min(i+3, len(t3.splitlines())+1)):
                print(j, repr(t3.splitlines()[j-1]))
    raise SystemExit("expected 2 filtroTipo replacements")

old_ref = "COALESCE(NULLIF(vi.ProdutoNome, ''), NULLIF(vi.DescricaoItem, ''), '{tipoItem} sem nome') AS Referencia,"
new_ref = (
    "CASE WHEN '{tipoItem}' = 'Servico' THEN COALESCE(NULLIF(vi.DescricaoItem, ''), NULLIF(vi.ProdutoNome, ''), '{tipoItem} sem nome') "
    "ELSE COALESCE(NULLIF(vi.ProdutoNome, ''), NULLIF(vi.DescricaoItem, ''), '{tipoItem} sem nome') END AS Referencia,"
)
if old_ref not in t3n:
    raise SystemExit("Referencia line not found")
t3n = t3n.replace(old_ref, new_ref, 1)
p3.write_text(t3n, encoding="utf-8")
print("BUG-006 SQL OK")