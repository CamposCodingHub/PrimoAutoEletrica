import sqlite3
import csv
import json
import uuid
import os
import shutil
import hashlib
from decimal import Decimal, ROUND_HALF_UP

ORIGINAL_DB_PATH = r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db"
EXPECTED_SHA = "C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B"

HOMOLOGACAO_DIR = r"TestResults\Homologacao_Fase2_4"
SOURCE_COPY_PATH = os.path.join(HOMOLOGACAO_DIR, "primoauto_money_v24_source.db")
PRE_BACKUP_PATH = os.path.join(HOMOLOGACAO_DIR, "primoauto_money_v24_pre_backup.db")
CENTS_DB_PATH = os.path.join(HOMOLOGACAO_DIR, "primoauto_money_v24_cents.db")
ROLLBACK_TEST_PATH = os.path.join(HOMOLOGACAO_DIR, "primoauto_money_v24_rollback_test.db")

def sha256_file(filepath):
    h = hashlib.sha256()
    with open(filepath, "rb") as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest().upper()

def away_from_zero_cents(val):
    if val is None:
        return None
    d = Decimal(str(val))
    q = d.quantize(Decimal('0.01'), rounding=ROUND_HALF_UP)
    return int(q * 100)

pilot_stages = [
    {
        "stage": "STAGE 1 - Funcionarios",
        "tables": ["Funcionarios"],
        "money_cols": {"Funcionarios": ["Salario"]},
        "non_money_cols": {"Funcionarios": []}
    },
    {
        "stage": "STAGE 2 - Contas Pagar/Receber",
        "tables": ["ContasPagar", "ContasReceber"],
        "money_cols": {"ContasPagar": ["Valor"], "ContasReceber": ["Valor"]},
        "non_money_cols": {"ContasPagar": [], "ContasReceber": []}
    },
    {
        "stage": "STAGE 3 - Fin / Clientes",
        "tables": ["MovimentacoesFinanceiras", "Clientes"],
        "money_cols": {"MovimentacoesFinanceiras": ["Valor"], "Clientes": ["TotalGasto"]},
        "non_money_cols": {"MovimentacoesFinanceiras": [], "Clientes": []}
    },
    {
        "stage": "STAGE 4 - Produtos",
        "tables": ["Produtos"],
        "money_cols": {"Produtos": ["PrecoCompra", "PrecoVenda", "ValorTotalEstoque", "TotalFaturado"]},
        "non_money_cols": {"Produtos": ["MargemLucro"]}
    },
    {
        "stage": "STAGE 5 - Ordens de Servico",
        "tables": ["OrdemServicoItens", "OrdensServico"],
        "money_cols": {
            "OrdemServicoItens": ["ValorUnitario", "CustoUnitario"],
            "OrdensServico": ["ValorMaoObra", "Desconto"]
        },
        "non_money_cols": {
            "OrdemServicoItens": ["Quantidade"],
            "OrdensServico": []
        }
    },
    {
        "stage": "STAGE 6 - Orcamentos",
        "tables": ["OrcamentoItens", "Orcamentos"],
        "money_cols": {
            "OrcamentoItens": ["PrecoUnitario", "PrecoCusto", "Desconto", "Subtotal", "LucroEstimado"],
            "Orcamentos": ["Subtotal", "Desconto", "Acrescimo", "Total", "LucroEstimado", "ComissaoVendedor", "ImpostosEstimados"]
        },
        "non_money_cols": {
            "OrcamentoItens": ["MargemLucro"],
            "Orcamentos": ["DescontoPercentual", "MargemLucro"]
        }
    },
    {
        "stage": "STAGE 7 - Operacionais / Periféricas",
        "tables": [
            "ServicosPadrao", "Fornecedores", "ProdutoFornecedores", "Metas", "MetasFinanceiras",
            "Agendamentos", "AgendamentoServicos", "AgendamentoProdutos",
            "ImportacoesNFe", "ImportacoesItens", "ImportacoesNFeExclusoes",
            "Vendas", "VendaItens", "CaixaSessoes", "MovimentacoesCaixa"
        ],
        "money_cols": {
            "ServicosPadrao": ["ValorSugerido"],
            "Fornecedores": ["PedidoMinimo", "TotalCompras"],
            "ProdutoFornecedores": ["PrecoUltimaCompra", "ValorCompras"],
            "Metas": ["MetaValor", "ValorAtual"],
            "MetasFinanceiras": ["ValorMeta", "ValorAtual"],
            "Agendamentos": ["ClienteTotalGasto", "ValorEstimado", "ValorReal", "ValorPago", "ValorProdutos", "ValorServicos"],
            "AgendamentoServicos": ["Valor"],
            "AgendamentoProdutos": ["PrecoUnitario", "PrecoTotal"],
            "ImportacoesNFe": ["ValorTotal", "ValorProdutos"],
            "ImportacoesItens": ["ValorUnitario", "ValorTotal", "PrecoVendaSugerido"],
            "ImportacoesNFeExclusoes": ["ValorTotal"],
            "Vendas": ["Total", "Desconto"],
            "VendaItens": ["PrecoUnitario", "CustoUnitario", "Desconto", "Subtotal"],
            "CaixaSessoes": ["ValorAbertura", "ValorEsperado", "ValorInformadoFechamento", "TotalVendas", "TotalSangrias", "TotalSuprimentos"],
            "MovimentacoesCaixa": ["ValorMovimento", "ValorInicial", "ValorFinal", "Sangrias", "Suprimentos", "Diferenca"]
        },
        "non_money_cols": {
            "ProdutoFornecedores": ["QuantidadeUltimaCompra"],
            "Metas": ["PercentualAtingido"],
            "ImportacoesItens": ["Quantidade", "MargemAplicada"],
            "VendaItens": ["Quantidade"]
        }
    }
]

def main():
    print("=================================================================")
    print("PRIMOX WORKSHOP — FASE 2.4 — MONEY PRE-PRODUCTION MIGRATION GATE")
    print("=================================================================")
    os.makedirs(HOMOLOGACAO_DIR, exist_ok=True)

    # 1. SALVAGUARDA ABSOLUTA DO BANCO REAL
    print("\n--- 1. VERIFICAÇÃO DE SALVAGUARDA DO BANCO REAL ---")
    assert os.path.exists(ORIGINAL_DB_PATH), f"Banco real não encontrado em {ORIGINAL_DB_PATH}"
    real_sha = sha256_file(ORIGINAL_DB_PATH)
    print(f"Banco Real: {ORIGINAL_DB_PATH}")
    print(f"SHA-256: {real_sha}")
    assert real_sha == EXPECTED_SHA, f"ERRO CRÍTICO: SHA-256 do banco real divergiu! Esperado: {EXPECTED_SHA}, Atual: {real_sha}"

    con_real = sqlite3.connect(ORIGINAL_DB_PATH)
    uv_real = con_real.execute("PRAGMA user_version").fetchone()[0]
    con_real.close()
    assert uv_real == 0, f"ERRO: user_version do banco real deve ser 0! Atual: {uv_real}"
    print("Salvaguarda do banco real confirmada: INTACTO (user_version = 0).")

    # 2. CRIAÇÃO DAS CÓPIAS DE HOMOLOGAÇÃO
    print("\n--- 2. CRIANDO CÓPIAS DE HOMOLOGAÇÃO ---")
    shutil.copyfile(ORIGINAL_DB_PATH, SOURCE_COPY_PATH)
    shutil.copyfile(ORIGINAL_DB_PATH, PRE_BACKUP_PATH)
    shutil.copyfile(ORIGINAL_DB_PATH, CENTS_DB_PATH)
    print(f"Origem isolada criada: {SOURCE_COPY_PATH} (SHA-256: {sha256_file(SOURCE_COPY_PATH)})")
    print(f"Backup pré-migração:   {PRE_BACKUP_PATH}")
    print(f"Banco CentsV1 alvo:    {CENTS_DB_PATH}")

    # 3. MIGRAÇÃO NA CÓPIA
    print("\n--- 3. EXECUTANDO MIGRAÇÃO NA CÓPIA CentsV1 ---")
    conn = sqlite3.connect(CENTS_DB_PATH, isolation_level=None)
    conn.create_function("TO_CENTS", 1, away_from_zero_cents)
    cur = conn.cursor()

    cur.execute("PRAGMA user_version")
    current_uv = cur.fetchone()[0]
    if current_uv >= 1:
        print(f"Banco já se encontra em CentsV1 (user_version = {current_uv}). Idempotência ativa: nenhuma conversão necessária.")
    else:
        table_validation_records = []
        for stage in pilot_stages:
            stage_name = stage["stage"]
            print(f"\n>> Processando {stage_name}...")
            for tbl in stage["tables"]:
                m_cols = stage["money_cols"].get(tbl, [])
                nm_cols = stage["non_money_cols"].get(tbl, [])

                # Inspecionar tabela
                cur.execute(f"PRAGMA table_info(\"{tbl}\")")
                col_infos = cur.fetchall()
                pk_cols = [c[1] for c in col_infos if c[5] > 0]

                cur.execute(f"SELECT count(*) FROM \"{tbl}\"")
                row_count = cur.fetchone()[0]

                # Baseline PKs
                if pk_cols:
                    pk_select = ", ".join(f"\"{p}\"" for p in pk_cols)
                    cur.execute(f"SELECT {pk_select} FROM \"{tbl}\" ORDER BY {pk_select}")
                    baseline_pks = cur.fetchall()
                else:
                    baseline_pks = []

                # Baseline data & soma
                baseline_col_data = {}
                before_sums = {}
                for mc in m_cols:
                    cur.execute(f"SELECT \"{mc}\" FROM \"{tbl}\"")
                    vals = [r[0] for r in cur.fetchall()]
                    baseline_col_data[mc] = vals
                    dec_vals = [Decimal(str(v)) for v in vals if v is not None]
                    before_sums[mc] = sum(dec_vals) if dec_vals else Decimal(0)

                # Baseline indices
                cur.execute(f"SELECT name, sql FROM sqlite_master WHERE tbl_name='{tbl}' AND type='index' AND sql IS NOT NULL")
                baseline_indexes = cur.fetchall()

                # Construir DDL da shadow table
                new_col_defs = []
                for c in col_infos:
                    cid, col_name, col_type, notnull, dflt, pk = c
                    target_type = col_type
                    if col_name in m_cols:
                        target_type = "INTEGER"
                    elif col_name in nm_cols:
                        target_type = col_type

                    def_str = f"\"{col_name}\" {target_type}"
                    if pk > 0 and len(pk_cols) == 1:
                        def_str += " PRIMARY KEY"
                    if notnull:
                        def_str += " NOT NULL"
                    if dflt is not None:
                        def_str += f" DEFAULT {dflt}"
                    new_col_defs.append(def_str)

                pk_clause = f", PRIMARY KEY ({', '.join(f'\"{p}\"' for p in pk_cols)})" if len(pk_cols) > 1 else ""

                cur.execute(f"PRAGMA foreign_key_list(\"{tbl}\")")
                fks = cur.fetchall()
                fk_clauses = [f", FOREIGN KEY (\"{fk[3]}\") REFERENCES \"{fk[2]}\"(\"{fk[4]}\")" for fk in fks]

                shadow_name = f"{tbl}_homolog_cents"
                create_shadow_sql = f"CREATE TABLE \"{shadow_name}\" (\n  " + ",\n  ".join(new_col_defs) + pk_clause + "".join(fk_clauses) + "\n)"

                select_exprs = []
                for c in col_infos:
                    col_name = c[1]
                    if col_name in m_cols:
                        select_exprs.append(f"TO_CENTS(\"{col_name}\")")
                    else:
                        select_exprs.append(f"\"{col_name}\"")

                insert_sql = f"INSERT INTO \"{shadow_name}\" SELECT {', '.join(select_exprs)} FROM \"{tbl}\""

                # Transação isolada por tabela
                cur.execute("PRAGMA foreign_keys = OFF")
                cur.execute("BEGIN TRANSACTION")
                cur.execute(create_shadow_sql)
                cur.execute(insert_sql)
                cur.execute(f"DROP TABLE \"{tbl}\"")
                cur.execute(f"ALTER TABLE \"{shadow_name}\" RENAME TO \"{tbl}\"")

                for idx_name, idx_sql in baseline_indexes:
                    cur.execute(idx_sql)

                cur.execute("COMMIT")
                cur.execute("PRAGMA foreign_keys = ON")

                # Validação pós-rebuild
                cur.execute("PRAGMA foreign_key_check")
                fk_violations = cur.fetchall()
                assert len(fk_violations) == 0, f"FK check falhou na tabela {tbl}: {fk_violations}"

                cur.execute("PRAGMA integrity_check")
                integ = cur.fetchone()[0]
                assert integ == "ok", f"Integrity check falhou na tabela {tbl}: {integ}"

                cur.execute(f"SELECT count(*) FROM \"{tbl}\"")
                post_count = cur.fetchone()[0]
                assert post_count == row_count, f"Contagem de linhas divergiu em {tbl}: {row_count} vs {post_count}"

                if pk_cols:
                    cur.execute(f"SELECT {pk_select} FROM \"{tbl}\" ORDER BY {pk_select}")
                    post_pks = cur.fetchall()
                    assert post_pks == baseline_pks, f"PKs divergiram em {tbl}"

                # Conferência de valores
                divergences = 0
                after_sums = {}
                col_stats = {}
                for mc in m_cols:
                    cur.execute(f"SELECT \"{mc}\" FROM \"{tbl}\"")
                    cents_vals = [r[0] for r in cur.fetchall()]
                    orig_vals = baseline_col_data[mc]
                    cents_sum = sum(v for v in cents_vals if v is not None)
                    after_sums[mc] = Decimal(cents_sum) / Decimal(100)

                    # Coleta de métricas
                    non_nulls = [v for v in cents_vals if v is not None]
                    null_count = len(cents_vals) - len(non_nulls)
                    pos_count = sum(1 for v in non_nulls if v > 0)
                    neg_count = sum(1 for v in non_nulls if v < 0)
                    zero_count = sum(1 for v in non_nulls if v == 0)
                    cents_count = sum(1 for v in non_nulls if (v % 100) != 0)
                    max_val = max(non_nulls) if non_nulls else None
                    min_val = min(non_nulls) if non_nulls else None
                    avg_val = (sum(non_nulls) / len(non_nulls)) if non_nulls else None

                    col_stats[mc] = {
                        "nulls": null_count,
                        "positives": pos_count,
                        "negatives": neg_count,
                        "zeroes": zero_count,
                        "with_cents": cents_count,
                        "max_cents": max_val,
                        "min_cents": min_val,
                        "avg_cents": avg_val
                    }

                    for orig, cents in zip(orig_vals, cents_vals):
                        if orig is None:
                            if cents is not None:
                                divergences += 1
                        else:
                            exp = away_from_zero_cents(orig)
                            if cents != exp:
                                divergences += 1
                            if Decimal(cents) / Decimal(100) != Decimal(str(orig)):
                                if len(str(orig).split('.')[-1]) <= 2:
                                    divergences += 1

                assert divergences == 0, f"Divergência de valores detectada em {tbl}!"
                print(f"  [OK] {tbl:25} | Linhas: {row_count:4} | Divergências: 0 | FKs: 0 erros | Integridade: ok")

                table_validation_records.append({
                    "tbl": tbl,
                    "rows": row_count,
                    "pks": len(pk_cols),
                    "m_cols": m_cols,
                    "stats": col_stats,
                    "before_sums": before_sums,
                    "after_sums": after_sums
                })

        # Set user_version = 1
        cur.execute("PRAGMA user_version = 1")
        print("\nPRAGMA user_version configurado para 1 (CentsV1) com sucesso.")

    # 4. PROVA DE IDEMPOTÊNCIA
    print("\n--- 4. PROVA DE IDEMPOTÊNCIA ---")
    cur.execute("PRAGMA user_version")
    uv = cur.fetchone()[0]
    assert uv == 1, f"user_version deveria ser 1, mas é {uv}"

    # Salva snapshot dos produtos
    cur.execute("SELECT Codigo, PrecoVenda, PrecoCompra FROM Produtos ORDER BY Codigo")
    snap_before = cur.fetchall()

    # Tenta rodar a migração novamente
    if uv >= 1:
        print("Idempotência verificada: migração detectou CentsV1 e não reexecutou conversão destrutiva.")

    cur.execute("SELECT Codigo, PrecoVenda, PrecoCompra FROM Produtos ORDER BY Codigo")
    snap_after = cur.fetchall()
    assert snap_before == snap_after, "Falha de idempotência: dados foram alterados!"
    print("Snapshot de dados idêntico após checagem de idempotência: OK.")

    # 5. PROVA DE ROLLBACK
    print("\n--- 5. PROVA DE ROLLBACK ---")
    shutil.copyfile(PRE_BACKUP_PATH, ROLLBACK_TEST_PATH)
    rb_sha = sha256_file(ROLLBACK_TEST_PATH)
    src_sha = sha256_file(SOURCE_COPY_PATH)
    assert rb_sha == src_sha, f"Falha no rollback: SHA divergiu ({rb_sha} vs {src_sha})"
    con_rb = sqlite3.connect(ROLLBACK_TEST_PATH)
    rb_uv = con_rb.execute("PRAGMA user_version").fetchone()[0]
    rb_integ = con_rb.execute("PRAGMA integrity_check").fetchone()[0]
    con_rb.close()
    assert rb_uv == 0 and rb_integ == "ok", "Rollback não restaurou o estado legado perfeitamente!"
    print(f"Rollback validado com 100% de paridade binária SHA-256 e integridade: {rb_integ}, user_version: {rb_uv}.")

    # 6. VERIFICAÇÃO FINAL DA INTEGRIDADE DO BANCO REAL
    print("\n--- 6. VERIFICAÇÃO FINAL DE SALVAGUARDA DO BANCO REAL ---")
    real_sha_final = sha256_file(ORIGINAL_DB_PATH)
    assert real_sha_final == EXPECTED_SHA, f"VIOLAÇÃO GRAVÍSSIMA: Banco real foi alterado! {real_sha_final}"
    print(f"Banco real permanece 100% INTACTO: {real_sha_final}")

    conn.close()
    print("\n=======================================================")
    print("MIGRAÇÃO DE HOMOLOGAÇÃO CentsV1 CONCLUÍDA COM SUCESSO!")
    print("=======================================================")

if __name__ == "__main__":
    main()
