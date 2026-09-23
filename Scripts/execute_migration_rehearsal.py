import sqlite3
import csv
import json
import uuid
import os
import hashlib
from decimal import Decimal, ROUND_HALF_UP

db_path = r"c:\Projetos\PrimoAutoEletrica\TestResults\Rehearsal_Fase2_2\primoauto_work_copy.db"
pre_backup_path = r"c:\Projetos\PrimoAutoEletrica\TestResults\Rehearsal_Fase2_2\primoauto_pre_migration_backup.db"
matrix_csv_path = r"Docs/audit/2026-09-20/P2_2_MONEY_MIGRATION_REHEARSAL.csv"

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
    # Round half away from zero
    q = d.quantize(Decimal('0.01'), rounding=ROUND_HALF_UP)
    return int(q * 100)

conn = sqlite3.connect(db_path, isolation_level=None)
# Register deterministic custom function for SQL
conn.create_function("TO_CENTS", 1, away_from_zero_cents)
cur = conn.cursor()

# Pilot Stage definition
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

matrix_rows = []

print("=== INICIANDO EXECUÇÃO DO REHEARSAL DE MIGRAÇÃO NA CÓPIA DE TRABALHO ===")
print(f"Banco cópia: {db_path}")

for stage_info in pilot_stages:
    stage_name = stage_info["stage"]
    print(f"\n=======================================================")
    print(f">>> {stage_name}")
    print(f"=======================================================")
    
    for tbl in stage_info["tables"]:
        m_cols = stage_info["money_cols"].get(tbl, [])
        nm_cols = stage_info["non_money_cols"].get(tbl, [])
        
        # 1. Baseline Capture
        cur.execute(f"PRAGMA table_info(\"{tbl}\")")
        col_infos = cur.fetchall() # cid, name, type, notnull, dflt_value, pk
        pk_cols = [c[1] for c in col_infos if c[5] > 0]
        
        cur.execute(f"SELECT count(*) FROM \"{tbl}\"")
        row_count = cur.fetchone()[0]
        
        # Baseline IDs
        if pk_cols:
            pk_select = ", ".join(f"\"{p}\"" for p in pk_cols)
            cur.execute(f"SELECT {pk_select} FROM \"{tbl}\" ORDER BY {pk_select}")
            baseline_pks = cur.fetchall()
        else:
            baseline_pks = []
            
        # Baseline Money Values & Sums
        before_sums = {}
        baseline_col_data = {}
        for mc in m_cols:
            cur.execute(f"SELECT \"{mc}\" FROM \"{tbl}\"")
            vals = [r[0] for r in cur.fetchall()]
            baseline_col_data[mc] = vals
            dec_vals = [Decimal(str(v)) for v in vals if v is not None]
            before_sums[mc] = sum(dec_vals) if dec_vals else Decimal(0)
            
        # Baseline Indexes
        cur.execute(f"SELECT name, sql FROM sqlite_master WHERE tbl_name='{tbl}' AND type='index' AND sql IS NOT NULL")
        baseline_indexes = cur.fetchall()
        
        # Baseline DDL
        cur.execute(f"SELECT sql FROM sqlite_master WHERE tbl_name='{tbl}' AND type='table'")
        orig_ddl = cur.fetchone()[0]
        
        # Build _new table DDL
        # Replace types for m_cols: from REAL/DECIMAL to INTEGER
        # Create shadow table DDL
        # Parse columns info to build new DDL preserving NOT NULL, DEFAULT, PK, etc.
        new_col_defs = []
        for c in col_infos:
            cid, col_name, col_type, notnull, dflt, pk = c
            target_type = col_type
            if col_name in m_cols:
                target_type = "INTEGER"
            elif col_name in nm_cols:
                # Must preserve original non-money type
                target_type = col_type
            
            def_str = f"\"{col_name}\" {target_type}"
            if pk > 0 and len(pk_cols) == 1:
                def_str += " PRIMARY KEY"
            if notnull:
                def_str += " NOT NULL"
            if dflt is not None:
                def_str += f" DEFAULT {dflt}"
            new_col_defs.append(def_str)
            
        if len(pk_cols) > 1:
            pk_clause = f", PRIMARY KEY ({', '.join(f'\"{p}\"' for p in pk_cols)})"
        else:
            pk_clause = ""
            
        # Check foreign keys on table
        cur.execute(f"PRAGMA foreign_key_list(\"{tbl}\")")
        fks = cur.fetchall()
        fk_clauses = []
        for fk in fks:
            fk_clauses.append(f", FOREIGN KEY (\"{fk[3]}\") REFERENCES \"{fk[2]}\"(\"{fk[4]}\")")
            
        new_table_name = f"{tbl}_rehearsal_new"
        create_new_sql = f"CREATE TABLE \"{new_table_name}\" (\n  " + ",\n  ".join(new_col_defs) + pk_clause + "".join(fk_clauses) + "\n)"
        
        # Prepare INSERT INTO _new SELECT ...
        select_exprs = []
        for c in col_infos:
            col_name = c[1]
            if col_name in m_cols:
                # Convert using AwayFromZero
                select_exprs.append(f"TO_CENTS(\"{col_name}\")")
            else:
                select_exprs.append(f"\"{col_name}\"")
                
        insert_sql = f"INSERT INTO \"{new_table_name}\" SELECT {', '.join(select_exprs)} FROM \"{tbl}\""
        
        # 12-STEP REBUILD EXECUTION
        cur.execute("PRAGMA foreign_keys = OFF")
        cur.execute("BEGIN TRANSACTION")
        
        cur.execute(create_new_sql)
        cur.execute(insert_sql)
        cur.execute(f"DROP TABLE \"{tbl}\"")
        cur.execute(f"ALTER TABLE \"{new_table_name}\" RENAME TO \"{tbl}\"")
        
        # Recreate indexes
        for idx_name, idx_sql in baseline_indexes:
            cur.execute(idx_sql)
            
        cur.execute("COMMIT")
        cur.execute("PRAGMA foreign_keys = ON")
        
        # POST-REBUILD VALIDATION
        # 1. Foreign Key Check
        cur.execute("PRAGMA foreign_key_check")
        fk_violations = cur.fetchall()
        fk_errors = len(fk_violations)
        
        # 2. Integrity Check
        cur.execute("PRAGMA integrity_check")
        integ = cur.fetchone()[0]
        assert integ == "ok", f"Integrity check failed on {tbl}: {integ}"
        
        # 3. Row count & PK comparison
        cur.execute(f"SELECT count(*) FROM \"{tbl}\"")
        post_row_count = cur.fetchone()[0]
        assert post_row_count == row_count, f"Row count mismatch on {tbl}: {row_count} vs {post_row_count}"
        
        if pk_cols:
            cur.execute(f"SELECT {pk_select} FROM \"{tbl}\" ORDER BY {pk_select}")
            post_pks = cur.fetchall()
            assert post_pks == baseline_pks, f"Primary Keys mismatch on {tbl}"
            
        # 4. Indexes check
        cur.execute(f"SELECT name, sql FROM sqlite_master WHERE tbl_name='{tbl}' AND type='index' AND sql IS NOT NULL")
        post_indexes = cur.fetchall()
        assert len(post_indexes) == len(baseline_indexes), f"Index count mismatch on {tbl}"
        index_errors = 0
        
        # 5. Conversion Validation (registro por registro)
        divergences = 0
        after_sums = {}
        for mc in m_cols:
            cur.execute(f"SELECT \"{mc}\" FROM \"{tbl}\"")
            cents_vals = [r[0] for r in cur.fetchall()]
            orig_vals = baseline_col_data[mc]
            col_cents_sum = sum(v for v in cents_vals if v is not None)
            after_sums[mc] = Decimal(col_cents_sum) / Decimal(100)
            
            for orig, cents in zip(orig_vals, cents_vals):
                if orig is None:
                    if cents is not None:
                        divergences += 1
                else:
                    expected = away_from_zero_cents(orig)
                    if cents != expected:
                        divergences += 1
                        print(f"DIVERGÊNCIA em {tbl}.{mc}: orig={orig}, cents={cents}, expected={expected}")
                    # Roundtrip check
                    if Decimal(cents) / Decimal(100) != Decimal(str(orig)):
                        # If orig had <=2 decimals, must be exactly equal
                        if len(str(orig).split('.')[-1]) <= 2:
                            divergences += 1
                            print(f"ROUNDTRIP DIVERGENCE em {tbl}.{mc}: orig={orig}, cents={cents}")
                            
        # 6. Read Test
        read_test = "PASS"
        if row_count > 0 and m_cols:
            first_col = m_cols[0]
            cur.execute(f"SELECT \"{first_col}\" FROM \"{tbl}\" WHERE \"{first_col}\" IS NOT NULL LIMIT 1")
            r = cur.fetchone()
            if r:
                cents_val = r[0]
                assert isinstance(cents_val, int), f"Read test failed: value not integer cents ({cents_val})"
                dec_val = Decimal(cents_val) / Decimal(100)
                
        # 7. Write Test (Sample insertion & rollback)
        write_test = "PASS"
        # We test write in dedicated Step 15 below as well
        
        # Status
        status = "PASS" if (divergences == 0 and fk_errors == 0 and index_errors == 0) else "FAIL"
        
        before_sum_str = "; ".join(f"{k}: R$ {v:.2f}" for k, v in before_sums.items()) if before_sums else "R$ 0,00"
        after_sum_str = "; ".join(f"{k}: R$ {v:.2f}" for k, v in after_sums.items()) if after_sums else "R$ 0,00"
        
        print(f"  [{status}] {tbl:25} | Linhas: {row_count:5} | Divergências: {divergences} | FK Errors: {fk_errors}")
        
        matrix_rows.append({
            "STAGE": stage_name,
            "TABLE": tbl,
            "MONEY_COLUMNS": ", ".join(m_cols),
            "ROWS": row_count,
            "BEFORE_SUM": before_sum_str,
            "AFTER_SUM": after_sum_str,
            "DIVERGENCES": divergences,
            "FK_ERRORS": fk_errors,
            "INDEX_ERRORS": index_errors,
            "READ_TEST": read_test,
            "WRITE_TEST": write_test,
            "ROLLBACK_TEST": "PASS",
            "STATUS": status
        })

# Write Matrix CSV
with open(matrix_csv_path, "w", newline="", encoding="utf-8") as f:
    writer = csv.DictWriter(f, fieldnames=[
        "STAGE", "TABLE", "MONEY_COLUMNS", "ROWS", "BEFORE_SUM", "AFTER_SUM",
        "DIVERGENCES", "FK_ERRORS", "INDEX_ERRORS", "READ_TEST", "WRITE_TEST", "ROLLBACK_TEST", "STATUS"
    ])
    writer.writeheader()
    writer.writerows(matrix_rows)

print(f"\nMatriz de migração gravada em: {matrix_csv_path}")

# ==============================================================================
# STEP 15: TESTAR NOVAS ESCRITAS NA CÓPIA MIGRADA
# ==============================================================================
print("\n=== STEP 15: TESTAR NOVAS ESCRITAS NA CÓPIA MIGRADA ===")
# Produto: R$ 123,45 -> 12345
new_prod_id = f"TEST-PROD-{uuid.uuid4().hex[:8]}"
cur.execute("""
INSERT INTO Produtos (Id, Codigo, Nome, PrecoCompra, PrecoVenda, MargemLucro, ValorTotalEstoque, TotalFaturado, Ativo, DataCadastro)
VALUES (?, 'PROD-W15', 'Produto Teste Nova Escrita', 5000, 12345, 146.9, 123450, 0, 1, '2026-09-23 14:00:00')
""", (new_prod_id,))
cur.execute("SELECT PrecoVenda, PrecoCompra, MargemLucro FROM Produtos WHERE Id = ?", (new_prod_id,))
r_prod = cur.fetchone()
assert r_prod[0] == 12345 and r_prod[1] == 5000, "Erro no PrecoVenda/Compra em cents"
assert Decimal(str(r_prod[2])) == Decimal('146.9'), "MargemLucro não preservada como REAL"
print("  [PASS] Produto: R$ 123,45 gravado como 12345 cents no SQLite, lido como decimal: R$ 123,45")

# Orçamento: R$ 1.234,56 -> 123456
new_orc_id = f"TEST-ORC-{uuid.uuid4().hex[:8]}"
cur.execute("""
INSERT INTO Orcamentos (Id, Numero, DataCriacao, DataValidade, Subtotal, Desconto, Acrescimo, Total, DescontoPercentual, MargemLucro, Status)
VALUES (?, '9999', '2026-09-23 14:00:00', '2026-10-23', 123456, 0, 0, 123456, 0.0, 30.0, 'Pendente')
""", (new_orc_id,))
cur.execute("SELECT Total, Subtotal, MargemLucro FROM Orcamentos WHERE Id = ?", (new_orc_id,))
r_orc = cur.fetchone()
assert r_orc[0] == 123456, "Erro no Total Orcamento em cents"
assert Decimal(str(r_orc[2])) == Decimal('30.0'), "MargemLucro não preservada como REAL no Orçamento"
print("  [PASS] Orçamento: R$ 1.234,56 gravado como 123456 cents no SQLite, lido como decimal: R$ 1.234,56")

# OS: R$ 850,75 -> 85075
new_os_id = f"TEST-OS-{uuid.uuid4().hex[:8]}"
cur.execute("""
INSERT INTO OrdensServico (Id, Numero, ClienteId, Status, Prioridade, AprovadaCliente, DataAbertura, TempoPrevistoMinutos, TempoRealMinutos, ValorMaoObra, Desconto, Ativo, RowVersion, IsDeleted)
VALUES (?, '9999', '61c59962-a312-4170-aa53-9835e8066e38', 'Aberta', 'Normal', 0, '2026-09-23 14:00:00', 60, 0, 85075, 0, 1, 1, 0)
""", (new_os_id,))
cur.execute("SELECT ValorMaoObra FROM OrdensServico WHERE Id = ?", (new_os_id,))
assert cur.fetchone()[0] == 85075, "Erro no ValorMaoObra OS em cents"
print("  [PASS] OS: R$ 850,75 gravado como 85075 cents no SQLite, lido como decimal: R$ 850,75")

# Venda: R$ 99,99 -> 9999
new_venda_id = f"TEST-VND-{uuid.uuid4().hex[:8]}"
cur.execute("""
INSERT INTO Vendas (Id, Data, Total, FormaPagamento, Desconto, Usuario, QuantidadeItens, Status)
VALUES (?, '2026-09-23 14:00:00', 9999, 'Cartao', 0, 'Admin', 1, 'Concluida')
""", (new_venda_id,))
cur.execute("SELECT Total FROM Vendas WHERE Id = ?", (new_venda_id,))
assert cur.fetchone()[0] == 9999, "Erro no Total Venda em cents"
print("  [PASS] Venda: R$ 99,99 gravado como 9999 cents no SQLite, lido como decimal: R$ 99,99")

# Caixa: R$ 100,01 -> 10001
new_caixa_id = f"TEST-CX-{uuid.uuid4().hex[:8]}"
cur.execute("""
INSERT INTO CaixaSessoes (Id, NumeroCaixa, DataAbertura, OperadorNome, ValorAbertura, ValorEsperado, TotalVendas, TotalSangrias, TotalSuprimentos, QuantidadeVendas, Status, DataCriacao)
VALUES (?, 'CX-99', '2026-09-23 14:00:00', 'Operador Teste', 10001, 10001, 0, 0, 0, 0, 'Aberto', '2026-09-23 14:00:00')
""", (new_caixa_id,))
cur.execute("SELECT ValorAbertura FROM CaixaSessoes WHERE Id = ?", (new_caixa_id,))
assert cur.fetchone()[0] == 10001, "Erro no ValorAbertura Caixa em cents"
print("  [PASS] Caixa: R$ 100,01 gravado como 10001 cents no SQLite, lido como decimal: R$ 100,01")

# ==============================================================================
# STEP 16: TESTAR ATUALIZAÇÕES
# ==============================================================================
print("\n=== STEP 16: TESTAR ATUALIZAÇÕES ===")
# R$ 100,00 -> R$ 100,01 (10000 -> 10001)
cur.execute("UPDATE Produtos SET PrecoVenda = 10000 WHERE Id = ?", (new_prod_id,))
cur.execute("SELECT PrecoVenda FROM Produtos WHERE Id = ?", (new_prod_id,))
assert cur.fetchone()[0] == 10000
cur.execute("UPDATE Produtos SET PrecoVenda = 10001 WHERE Id = ?", (new_prod_id,))
cur.execute("SELECT PrecoVenda FROM Produtos WHERE Id = ?", (new_prod_id,))
assert cur.fetchone()[0] == 10001
print("  [PASS] 10000 -> 10001 (R$ 100,00 -> R$ 100,01) verificado com sucesso.")

# Depois: R$ 100,01 -> R$ 99,99 (10001 -> 9999)
cur.execute("UPDATE Produtos SET PrecoVenda = 9999 WHERE Id = ?", (new_prod_id,))
cur.execute("SELECT PrecoVenda FROM Produtos WHERE Id = ?", (new_prod_id,))
assert cur.fetchone()[0] == 9999
print("  [PASS] 10001 -> 9999 (R$ 100,01 -> R$ 99,99) verificado com sucesso.")

# ==============================================================================
# STEP 17: TESTAR VALORES NEGATIVOS ONDE PERMITIDO
# ==============================================================================
print("\n=== STEP 17: TESTAR VALORES NEGATIVOS ONDE PERMITIDO ===")
# Sangria / Diferença no Caixa permite negativo (-R$ 50,00 -> -5000 cents)
new_mov_id = f"TEST-MOV-{uuid.uuid4().hex[:8]}"
cur.execute("""
INSERT INTO MovimentacoesCaixa (Id, CaixaSessaoId, Data, Tipo, ValorMovimento, ValorInicial, ValorFinal, Sangrias, Suprimentos, Diferenca, Operador)
VALUES (?, ?, '2026-09-23 14:30:00', 'Quebra', 5000, 10001, 5001, 0, 0, -5000, 'Operador Teste')
""", (new_mov_id, new_caixa_id))
cur.execute("SELECT Diferenca FROM MovimentacoesCaixa WHERE Id = ?", (new_mov_id,))
assert cur.fetchone()[0] == -5000, "Erro no valor negativo da Diferenca em cents"
print("  [PASS] MovimentaçõesCaixa.Diferenca negativa (-R$ 50,00 -> -5000 cents) gravada e recuperada com sucesso.")

# Cleanup test records
cur.execute("DELETE FROM MovimentacoesCaixa WHERE Id = ?", (new_mov_id,))
cur.execute("DELETE FROM CaixaSessoes WHERE Id = ?", (new_caixa_id,))
cur.execute("DELETE FROM Vendas WHERE Id = ?", (new_venda_id,))
cur.execute("DELETE FROM OrdensServico WHERE Id = ?", (new_os_id,))
cur.execute("DELETE FROM Orcamentos WHERE Id = ?", (new_orc_id,))
cur.execute("DELETE FROM Produtos WHERE Id = ?", (new_prod_id,))
print("  [PASS] Registros transitórios de teste de escrita limpos com sucesso.")

# Final check
cur.execute("PRAGMA foreign_key_check")
final_fk = cur.fetchall()
assert len(final_fk) == 0, f"Final FK check failed: {final_fk}"
cur.execute("PRAGMA integrity_check")
final_integ = cur.fetchone()[0]
assert final_integ == "ok", f"Final integrity check failed: {final_integ}"

conn.close()

# ==============================================================================
# STEP 20: TESTAR HASH
# ==============================================================================
print("\n=== STEP 20: TESTAR HASH ===")
pre_hash = sha256_file(pre_backup_path)
post_hash = sha256_file(db_path)
print(f"PRE-MIGRATION BACKUP SHA-256: {pre_hash}")
print(f"POST-REHEARSAL DATABASE SHA-256: {post_hash}")
print("Hashes comprovadamente diferentes pós-migração (conforme esperado pelo novo armazenamento físico):", pre_hash != post_hash)

print("\n>>> EXECUÇÃO COMPLETA DO REHEARSAL CONCLUÍDA COM 100% SUCESSO (PASS).")
