import sqlite3
import os
import uuid
from decimal import Decimal, ROUND_HALF_UP

temp_db_path = r"c:\Projetos\PrimoAutoEletrica\TestResults\Rehearsal_Fase2_2\temp_synthetic_empty_tables.db"
if os.path.exists(temp_db_path):
    os.remove(temp_db_path)

conn = sqlite3.connect(temp_db_path)
cur = conn.cursor()

# Enable FKs initially
cur.execute("PRAGMA foreign_keys = ON")

# Create original schema (REAL)
cur.execute("""
CREATE TABLE Vendas
(
    Id TEXT PRIMARY KEY,
    Data TEXT NOT NULL,
    ClienteId TEXT,
    ClienteNome TEXT,
    Total REAL NOT NULL,
    FormaPagamento TEXT NOT NULL,
    Desconto REAL NOT NULL DEFAULT 0,
    Usuario TEXT NOT NULL,
    QuantidadeItens INTEGER NOT NULL DEFAULT 0,
    Status TEXT NOT NULL DEFAULT 'Concluida',
    CaixaSessaoId TEXT,
    DataCancelamento TEXT,
    CanceladoPor TEXT,
    MotivoCancelamento TEXT
)
""")

cur.execute("""
CREATE TABLE VendaItens
(
    Id TEXT PRIMARY KEY,
    VendaId TEXT NOT NULL,
    ProdutoId TEXT,
    Tipo TEXT NOT NULL DEFAULT 'Produto',
    DescricaoItem TEXT,
    ProdutoNome TEXT NOT NULL,
    Quantidade INTEGER NOT NULL,
    PrecoUnitario REAL NOT NULL,
    CustoUnitario REAL NOT NULL DEFAULT 0,
    Desconto REAL NOT NULL DEFAULT 0,
    Subtotal REAL NOT NULL,
    FOREIGN KEY (VendaId) REFERENCES Vendas(Id)
)
""")

cur.execute("""
CREATE TABLE CaixaSessoes
(
    Id TEXT PRIMARY KEY,
    NumeroCaixa TEXT NOT NULL,
    DataAbertura TEXT NOT NULL,
    DataFechamento TEXT,
    OperadorId INTEGER,
    OperadorNome TEXT NOT NULL,
    PerfilOperador TEXT,
    ValorAbertura REAL NOT NULL DEFAULT 0,
    ValorEsperado REAL NOT NULL DEFAULT 0,
    ValorInformadoFechamento REAL,
    TotalVendas REAL NOT NULL DEFAULT 0,
    TotalSangrias REAL NOT NULL DEFAULT 0,
    TotalSuprimentos REAL NOT NULL DEFAULT 0,
    QuantidadeVendas INTEGER NOT NULL DEFAULT 0,
    Status TEXT NOT NULL DEFAULT 'Fechado',
    Observacoes TEXT,
    DataCriacao TEXT NOT NULL,
    DataUltimaMovimentacao TEXT
)
""")

cur.execute("""
CREATE TABLE MovimentacoesCaixa
(
    Id TEXT PRIMARY KEY,
    CaixaSessaoId TEXT NOT NULL,
    Data TEXT NOT NULL,
    Tipo TEXT NOT NULL,
    ValorMovimento REAL NOT NULL DEFAULT 0,
    ValorInicial REAL NOT NULL DEFAULT 0,
    ValorFinal REAL NOT NULL DEFAULT 0,
    Sangrias REAL NOT NULL DEFAULT 0,
    Suprimentos REAL NOT NULL DEFAULT 0,
    Diferenca REAL NOT NULL DEFAULT 0,
    Operador TEXT,
    FormaPagamento TEXT,
    ReferenciaId TEXT,
    Observacoes TEXT,
    FOREIGN KEY (CaixaSessaoId) REFERENCES CaixaSessoes(Id)
)
""")
conn.commit()

# Insert Synthetic data representing all required cases:
# R$ 0,01, R$ 0,05, R$ 0,10, R$ 1,23, R$ 99,99, R$ 100,01, R$ 1.005,67
# Casos: desconto, acréscimo, sangria, suprimento, saldo, diferença, venda, item de venda, fechamento de caixa

sessao_id_1 = str(uuid.uuid4())
sessao_id_2 = str(uuid.uuid4())

# CaixaSessoes
# Sessao 1: Abertura 100.01, Suprimento 99.99, Sangria 1.23, Vendas 1005.67, Esperado 1204.44, Fechamento 1204.44, Diferenca 0.00
# Sessao 2: Abertura 0.10, Suprimento 0.05, Sangria 0.01, Vendas 1.23, Esperado 1.37, Fechamento 1.32, Diferenca -0.05
caixas = [
    (sessao_id_1, "CX-01", "2026-09-23 08:00:00", "2026-09-23 18:00:00", 1, "Operador A", "Caixa", 100.01, 1204.44, 1204.44, 1005.67, 1.23, 99.99, 3, "Fechado", "Sessao Normal", "2026-09-23 08:00:00", "2026-09-23 18:00:00"),
    (sessao_id_2, "CX-02", "2026-09-23 09:00:00", "2026-09-23 12:00:00", 2, "Operador B", "Caixa", 0.10, 1.37, 1.32, 1.23, 0.01, 0.05, 1, "Fechado", "Com diferenca", "2026-09-23 09:00:00", "2026-09-23 12:00:00")
]

cur.executemany("""
INSERT INTO CaixaSessoes (Id, NumeroCaixa, DataAbertura, DataFechamento, OperadorId, OperadorNome, PerfilOperador,
    ValorAbertura, ValorEsperado, ValorInformadoFechamento, TotalVendas, TotalSangrias, TotalSuprimentos, QuantidadeVendas,
    Status, Observacoes, DataCriacao, DataUltimaMovimentacao)
VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
""", caixas)

# MovimentacoesCaixa
movs = [
    (str(uuid.uuid4()), sessao_id_1, "2026-09-23 08:00:00", "Abertura", 100.01, 0.00, 100.01, 0.00, 0.00, 0.00, "Operador A", "Dinheiro", None, "Abertura"),
    (str(uuid.uuid4()), sessao_id_1, "2026-09-23 10:00:00", "Suprimento", 99.99, 100.01, 200.00, 0.00, 99.99, 0.00, "Operador A", "Dinheiro", None, "Suprimento de troco"),
    (str(uuid.uuid4()), sessao_id_1, "2026-09-23 12:00:00", "Sangria", 1.23, 200.00, 198.77, 1.23, 0.00, 0.00, "Operador A", "Dinheiro", None, "Sangria despesa miúda"),
    (str(uuid.uuid4()), sessao_id_2, "2026-09-23 09:00:00", "Abertura", 0.10, 0.00, 0.10, 0.00, 0.00, 0.00, "Operador B", "Dinheiro", None, "Abertura minima"),
    (str(uuid.uuid4()), sessao_id_2, "2026-09-23 09:30:00", "Suprimento", 0.05, 0.10, 0.15, 0.00, 0.05, 0.00, "Operador B", "Dinheiro", None, "Suprimento centavos"),
    (str(uuid.uuid4()), sessao_id_2, "2026-09-23 10:30:00", "Sangria", 0.01, 0.15, 0.14, 0.01, 0.00, 0.00, "Operador B", "Dinheiro", None, "Sangria 1 centavo"),
    (str(uuid.uuid4()), sessao_id_2, "2026-09-23 12:00:00", "Fechamento", 1.32, 1.37, 1.32, 0.00, 0.00, -0.05, "Operador B", "Dinheiro", None, "Fechamento com quebra")
]

cur.executemany("""
INSERT INTO MovimentacoesCaixa (Id, CaixaSessaoId, Data, Tipo, ValorMovimento, ValorInicial, ValorFinal, Sangrias, Suprimentos, Diferenca, Operador, FormaPagamento, ReferenciaId, Observacoes)
VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
""", movs)

# Vendas & VendaItens
venda_id_1 = str(uuid.uuid4())
venda_id_2 = str(uuid.uuid4())

vendas = [
    (venda_id_1, "2026-09-23 11:00:00", "CLI-01", "Cliente Alpha", 1005.67, "Cartao", 99.99, "Operador A", 2, "Concluida", sessao_id_1, None, None, None),
    (venda_id_2, "2026-09-23 11:30:00", "CLI-02", "Cliente Beta", 1.23, "Dinheiro", 0.10, "Operador B", 2, "Concluida", sessao_id_2, None, None, None)
]

cur.executemany("""
INSERT INTO Vendas (Id, Data, ClienteId, ClienteNome, Total, FormaPagamento, Desconto, Usuario, QuantidadeItens, Status, CaixaSessaoId, DataCancelamento, CanceladoPor, MotivoCancelamento)
VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
""", vendas)

itens = [
    (str(uuid.uuid4()), venda_id_1, "PROD-01", "Produto", "Lampada H7", "Lampada Philips H7", 10, 100.01, 50.00, 0.00, 1000.10),
    (str(uuid.uuid4()), venda_id_1, "PROD-02", "Produto", "Fusivel 10A", "Fusivel Lamina 10A", 5, 21.11, 10.00, 0.00, 105.56),
    (str(uuid.uuid4()), venda_id_2, "PROD-03", "Produto", "Terminal Ilhos", "Terminal 0.5mm", 10, 0.10, 0.05, 0.00, 1.00),
    (str(uuid.uuid4()), venda_id_2, "PROD-04", "Produto", "Arruela Pequena", "Arruela Lisa 3mm", 33, 0.01, 0.00, 0.00, 0.33)
]

cur.executemany("""
INSERT INTO VendaItens (Id, VendaId, ProdutoId, Tipo, DescricaoItem, ProdutoNome, Quantidade, PrecoUnitario, CustoUnitario, Desconto, Subtotal)
VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
""", itens)

conn.commit()

# Record pre-rebuild baseline
def get_table_snapshot(tbl, money_cols):
    cur.execute(f"SELECT count(*) FROM {tbl}")
    cnt = cur.fetchone()[0]
    col_data = {}
    for c in money_cols:
        cur.execute(f"SELECT \"{c}\" FROM {tbl}")
        vals = [r[0] for r in cur.fetchall()]
        col_data[c] = {
            "sum": sum(Decimal(str(v)) for v in vals),
            "vals": vals
        }
    return cnt, col_data

pre_vendas_cnt, pre_vendas_data = get_table_snapshot("Vendas", ["Total", "Desconto"])
pre_itens_cnt, pre_itens_data = get_table_snapshot("VendaItens", ["PrecoUnitario", "CustoUnitario", "Desconto", "Subtotal"])
pre_caixa_cnt, pre_caixa_data = get_table_snapshot("CaixaSessoes", ["ValorAbertura", "ValorEsperado", "ValorInformadoFechamento", "TotalVendas", "TotalSangrias", "TotalSuprimentos"])
pre_mov_cnt, pre_mov_data = get_table_snapshot("MovimentacoesCaixa", ["ValorMovimento", "ValorInicial", "ValorFinal", "Sangrias", "Suprimentos", "Diferenca"])

print("--- BASELINE SINTÉTICO PRÉ-REBUILD REGISTRADO ---")
print(f"Vendas: {pre_vendas_cnt} rows, Total SUM={pre_vendas_data['Total']['sum']}")
print(f"VendaItens: {pre_itens_cnt} rows, Subtotal SUM={pre_itens_data['Subtotal']['sum']}")
print(f"CaixaSessoes: {pre_caixa_cnt} rows, TotalVendas SUM={pre_caixa_data['TotalVendas']['sum']}")
print(f"MovimentacoesCaixa: {pre_mov_cnt} rows, ValorMovimento SUM={pre_mov_data['ValorMovimento']['sum']}")

# NOW EXECUTE 12-STEP REBUILD FOR EACH TABLE
cur.execute("PRAGMA foreign_keys = OFF")

# 1. Rebuild Vendas
cur.execute("BEGIN TRANSACTION")
cur.execute("""
CREATE TABLE Vendas_new (
    Id TEXT PRIMARY KEY,
    Data TEXT NOT NULL,
    ClienteId TEXT,
    ClienteNome TEXT,
    Total INTEGER NOT NULL,
    FormaPagamento TEXT NOT NULL,
    Desconto INTEGER NOT NULL DEFAULT 0,
    Usuario TEXT NOT NULL,
    QuantidadeItens INTEGER NOT NULL DEFAULT 0,
    Status TEXT NOT NULL DEFAULT 'Concluida',
    CaixaSessaoId TEXT,
    DataCancelamento TEXT,
    CanceladoPor TEXT,
    MotivoCancelamento TEXT
)
""")
cur.execute("""
INSERT INTO Vendas_new (Id, Data, ClienteId, ClienteNome, Total, FormaPagamento, Desconto, Usuario, QuantidadeItens, Status, CaixaSessaoId, DataCancelamento, CanceladoPor, MotivoCancelamento)
SELECT Id, Data, ClienteId, ClienteNome,
       CAST(ROUND(Total * 100) AS INTEGER),
       FormaPagamento,
       CAST(ROUND(Desconto * 100) AS INTEGER),
       Usuario, QuantidadeItens, Status, CaixaSessaoId, DataCancelamento, CanceladoPor, MotivoCancelamento
FROM Vendas
""")
cur.execute("DROP TABLE Vendas")
cur.execute("ALTER TABLE Vendas_new RENAME TO Vendas")
cur.execute("COMMIT")

# 2. Rebuild VendaItens
cur.execute("BEGIN TRANSACTION")
cur.execute("""
CREATE TABLE "VendaItens_new" (
    Id TEXT PRIMARY KEY,
    VendaId TEXT NOT NULL,
    ProdutoId TEXT,
    Tipo TEXT NOT NULL DEFAULT 'Produto',
    DescricaoItem TEXT,
    ProdutoNome TEXT NOT NULL,
    Quantidade INTEGER NOT NULL,
    PrecoUnitario INTEGER NOT NULL,
    CustoUnitario INTEGER NOT NULL DEFAULT 0,
    Desconto INTEGER NOT NULL DEFAULT 0,
    Subtotal INTEGER NOT NULL,
    FOREIGN KEY (VendaId) REFERENCES Vendas(Id)
)
""")
cur.execute("""
INSERT INTO "VendaItens_new" (Id, VendaId, ProdutoId, Tipo, DescricaoItem, ProdutoNome, Quantidade, PrecoUnitario, CustoUnitario, Desconto, Subtotal)
SELECT Id, VendaId, ProdutoId, Tipo, DescricaoItem, ProdutoNome, Quantidade,
       CAST(ROUND(PrecoUnitario * 100) AS INTEGER),
       CAST(ROUND(CustoUnitario * 100) AS INTEGER),
       CAST(ROUND(Desconto * 100) AS INTEGER),
       CAST(ROUND(Subtotal * 100) AS INTEGER)
FROM VendaItens
""")
cur.execute("DROP TABLE VendaItens")
cur.execute("ALTER TABLE VendaItens_new RENAME TO VendaItens")
cur.execute("COMMIT")

# 3. Rebuild CaixaSessoes
cur.execute("BEGIN TRANSACTION")
cur.execute("""
CREATE TABLE CaixaSessoes_new (
    Id TEXT PRIMARY KEY,
    NumeroCaixa TEXT NOT NULL,
    DataAbertura TEXT NOT NULL,
    DataFechamento TEXT,
    OperadorId INTEGER,
    OperadorNome TEXT NOT NULL,
    PerfilOperador TEXT,
    ValorAbertura INTEGER NOT NULL DEFAULT 0,
    ValorEsperado INTEGER NOT NULL DEFAULT 0,
    ValorInformadoFechamento INTEGER,
    TotalVendas INTEGER NOT NULL DEFAULT 0,
    TotalSangrias INTEGER NOT NULL DEFAULT 0,
    TotalSuprimentos INTEGER NOT NULL DEFAULT 0,
    QuantidadeVendas INTEGER NOT NULL DEFAULT 0,
    Status TEXT NOT NULL DEFAULT 'Fechado',
    Observacoes TEXT,
    DataCriacao TEXT NOT NULL,
    DataUltimaMovimentacao TEXT
)
""")
cur.execute("""
INSERT INTO CaixaSessoes_new (Id, NumeroCaixa, DataAbertura, DataFechamento, OperadorId, OperadorNome, PerfilOperador,
    ValorAbertura, ValorEsperado, ValorInformadoFechamento, TotalVendas, TotalSangrias, TotalSuprimentos, QuantidadeVendas,
    Status, Observacoes, DataCriacao, DataUltimaMovimentacao)
SELECT Id, NumeroCaixa, DataAbertura, DataFechamento, OperadorId, OperadorNome, PerfilOperador,
       CAST(ROUND(ValorAbertura * 100) AS INTEGER),
       CAST(ROUND(ValorEsperado * 100) AS INTEGER),
       CASE WHEN ValorInformadoFechamento IS NOT NULL THEN CAST(ROUND(ValorInformadoFechamento * 100) AS INTEGER) ELSE NULL END,
       CAST(ROUND(TotalVendas * 100) AS INTEGER),
       CAST(ROUND(TotalSangrias * 100) AS INTEGER),
       CAST(ROUND(TotalSuprimentos * 100) AS INTEGER),
       QuantidadeVendas, Status, Observacoes, DataCriacao, DataUltimaMovimentacao
FROM CaixaSessoes
""")
cur.execute("DROP TABLE CaixaSessoes")
cur.execute("ALTER TABLE CaixaSessoes_new RENAME TO CaixaSessoes")
cur.execute("COMMIT")

# 4. Rebuild MovimentacoesCaixa
cur.execute("BEGIN TRANSACTION")
cur.execute("""
CREATE TABLE MovimentacoesCaixa_new (
    Id TEXT PRIMARY KEY,
    CaixaSessaoId TEXT NOT NULL,
    Data TEXT NOT NULL,
    Tipo TEXT NOT NULL,
    ValorMovimento INTEGER NOT NULL DEFAULT 0,
    ValorInicial INTEGER NOT NULL DEFAULT 0,
    ValorFinal INTEGER NOT NULL DEFAULT 0,
    Sangrias INTEGER NOT NULL DEFAULT 0,
    Suprimentos INTEGER NOT NULL DEFAULT 0,
    Diferenca INTEGER NOT NULL DEFAULT 0,
    Operador TEXT,
    FormaPagamento TEXT,
    ReferenciaId TEXT,
    Observacoes TEXT,
    FOREIGN KEY (CaixaSessaoId) REFERENCES CaixaSessoes(Id)
)
""")
cur.execute("""
INSERT INTO MovimentacoesCaixa_new (Id, CaixaSessaoId, Data, Tipo, ValorMovimento, ValorInicial, ValorFinal, Sangrias, Suprimentos, Diferenca, Operador, FormaPagamento, ReferenciaId, Observacoes)
SELECT Id, CaixaSessaoId, Data, Tipo,
       CAST(ROUND(ValorMovimento * 100) AS INTEGER),
       CAST(ROUND(ValorInicial * 100) AS INTEGER),
       CAST(ROUND(ValorFinal * 100) AS INTEGER),
       CAST(ROUND(Sangrias * 100) AS INTEGER),
       CAST(ROUND(Suprimentos * 100) AS INTEGER),
       CAST(ROUND(Diferenca * 100) AS INTEGER),
       Operador, FormaPagamento, ReferenciaId, Observacoes
FROM MovimentacoesCaixa
""")
cur.execute("DROP TABLE MovimentacoesCaixa")
cur.execute("ALTER TABLE MovimentacoesCaixa_new RENAME TO MovimentacoesCaixa")
cur.execute("COMMIT")

# Re-enable and verify integrity
cur.execute("PRAGMA foreign_keys = ON")
cur.execute("PRAGMA foreign_key_check")
fk_violations = cur.fetchall()
cur.execute("PRAGMA integrity_check")
integrity = cur.fetchone()[0]

print("\n--- VALIDAÇÃO PÓS-MIGRATION SINTÉTICA ---")
print(f"FK Violations: {len(fk_violations)}")
print(f"Integrity check: {integrity}")

# Validate roundtrip conversion
def validate_table(tbl, money_cols, pre_data):
    cur.execute(f"SELECT count(*) FROM {tbl}")
    post_cnt = cur.fetchone()[0]
    assert post_cnt == len(pre_data[money_cols[0]]["vals"]), "Row count mismatch"
    
    for c in money_cols:
        cur.execute(f"SELECT \"{c}\" FROM {tbl}")
        cents_vals = [r[0] for r in cur.fetchall()]
        orig_vals = pre_data[c]["vals"]
        for i, (orig, cents) in enumerate(zip(orig_vals, cents_vals)):
            if orig is None:
                assert cents is None, f"Expected NULL at row {i} col {c}"
            else:
                expected_cents = int(Decimal(str(orig)).quantize(Decimal('0.01'), rounding=ROUND_HALF_UP) * 100)
                assert cents == expected_cents, f"Discrepancy at {tbl}.{c} row {i}: orig={orig} cents={cents} expected={expected_cents}"
                # And roundtrip back
                assert Decimal(cents) / 100 == Decimal(str(orig)), f"Roundtrip mismatch at {tbl}.{c}: orig={orig}, cents={cents}"
        print(f"  [PASS] {tbl}.{c}: {len(cents_vals)} registros convertidos com 0 divergências")

validate_table("Vendas", ["Total", "Desconto"], pre_vendas_data)
validate_table("VendaItens", ["PrecoUnitario", "CustoUnitario", "Desconto", "Subtotal"], pre_itens_data)
validate_table("CaixaSessoes", ["ValorAbertura", "ValorEsperado", "ValorInformadoFechamento", "TotalVendas", "TotalSangrias", "TotalSuprimentos"], pre_caixa_data)
validate_table("MovimentacoesCaixa", ["ValorMovimento", "ValorInicial", "ValorFinal", "Sangrias", "Suprimentos", "Diferenca"], pre_mov_data)

# Test new writes directly in INTEGER cents
new_venda_id = str(uuid.uuid4())
cur.execute("""
INSERT INTO Vendas (Id, Data, ClienteId, ClienteNome, Total, FormaPagamento, Desconto, Usuario, QuantidadeItens, Status, CaixaSessaoId)
VALUES (?, '2026-09-23 15:00:00', 'CLI-NEW', 'Novo Cliente', 9999, 'Pix', 0, 'Admin', 1, 'Concluida', ?)
""", (new_venda_id, sessao_id_1))

cur.execute("SELECT Total, Desconto FROM Vendas WHERE Id = ?", (new_venda_id,))
row = cur.fetchone()
assert row[0] == 9999 and row[1] == 0, "Write test failed"
print("  [PASS] Nova escrita Venda R$ 99,99 gravada como 9999 cents com sucesso.")

# Test update existing
cur.execute("UPDATE Vendas SET Total = 10001 WHERE Id = ?", (new_venda_id,))
cur.execute("SELECT Total FROM Vendas WHERE Id = ?", (new_venda_id,))
assert cur.fetchone()[0] == 10001, "Update test failed"
print("  [PASS] Update Venda 9999 -> 10001 cents com sucesso.")

conn.close()

# Destroy temporary database
if os.path.exists(temp_db_path):
    os.remove(temp_db_path)
    print("  [PASS] Banco temporário destruído com sucesso.")

print("\n>>> TESTE SINTÉTICO DAS TABELAS VAZIAS CONCLUÍDO COM 100% SUCESSO (PASS).")
