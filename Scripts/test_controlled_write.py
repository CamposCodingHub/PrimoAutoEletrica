import sqlite3
import hashlib
import os

BASELINE_SHA = "C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B"
ORIGINAL_DB = r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db"
OPERATIONAL_DB = r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto_operacional.db"

def get_sha256(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest().upper()

def main():
    print("=== FASE 8: TESTE CONTROLADO DE ESCRITA ===")
    
    # 1. Verify before
    sha_orig_before = get_sha256(ORIGINAL_DB)
    print(f"SHA original antes: {sha_orig_before}")
    assert sha_orig_before == BASELINE_SHA, "SHA original diverge do baseline antes do teste!"

    # 2. Insert test record in operational DB
    con_op = sqlite3.connect(OPERATIONAL_DB)
    cur_op = con_op.cursor()
    
    test_name = "B2.1 — TESTE DESKTOP"
    cur_op.execute("""
        INSERT INTO Clientes (Nome, Telefone, Email, Rua, Numero, Cidade, Estado, CEP, DataCadastro, Observacoes, Ativo)
        VALUES (?, '11999990000', 'b21_teste@primoauto.local', 'Rua Teste B2.1', '100', 'Sao Paulo', 'SP', '01000-000', datetime('now'), 'Registro de teste controlado FASE 8 B2.1', 1)
    """, (test_name,))
    con_op.commit()
    inserted_id = cur_op.lastrowid
    print(f"Registro inserido com sucesso em primoauto_operacional.db (Id: {inserted_id})")
    
    # 3. Verify persistence in operational DB
    cur_op.execute("SELECT Id, Nome FROM Clientes WHERE Nome = ?", (test_name,))
    record_op = cur_op.fetchone()
    print(f"Registro lido do operacional: {record_op}")
    assert record_op is not None and record_op[1] == test_name, "Falha ao persistir no operacional!"
    
    # 4. Verify absence in original DB
    con_orig = sqlite3.connect(f"file:{ORIGINAL_DB}?mode=ro", uri=True)
    cur_orig = con_orig.cursor()
    cur_orig.execute("SELECT Id, Nome FROM Clientes WHERE Nome = ?", (test_name,))
    record_orig = cur_orig.fetchone()
    con_orig.close()
    print(f"Registro em primoauto.db original: {record_orig}")
    assert record_orig is None, "ALERTA CRITICO: Registro de teste encontrado no banco original!"
    print("CONFIRMADO: O registro existe em primoauto_operacional.db e NAO existe em primoauto.db.")
    
    # 5. Clean up test record from operational DB
    cur_op.execute("DELETE FROM Clientes WHERE Id = ?", (inserted_id,))
    con_op.commit()
    con_op.close()
    print(f"Registro de teste Id {inserted_id} removido do banco operacional com sucesso.")
    
    # 6. FASE 9: Verify SHA-256 of original DB after test
    print("\n=== FASE 9: PROVA DE INTEGRIDADE DO BANCO ORIGINAL ===")
    sha_orig_after = get_sha256(ORIGINAL_DB)
    print(f"SHA original depois: {sha_orig_after}")
    assert sha_orig_after == BASELINE_SHA, "ALERTA CRITICO: SHA do banco original mudou apos o teste!"
    
    is_readonly = not os.access(ORIGINAL_DB, os.W_OK)
    print(f"Atributo ReadOnly do banco original preservado: {is_readonly}")
    
    print("\nFASE 8 = PASS")
    print("FASE 9 = PASS")

if __name__ == "__main__":
    main()
