import os
import shutil
import sqlite3
import hashlib
import tempfile
import gc

def test_backup_restore():
    print("=== TESTE DE BACKUP / RESTORE / INTEGRIDADE (FASE 13) ===")
    oper_db = r"C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto_operacional.db"
    
    temp_dir = tempfile.mkdtemp()
    try:
        # 1. Realizar Backup do banco operacional para temp
        backup_file = os.path.join(temp_dir, "backup_teste.db")
        print(f"1. Criando backup consistente de {oper_db} para {backup_file}...")
        
        src_conn = sqlite3.connect(oper_db)
        dst_conn = sqlite3.connect(backup_file)
        with dst_conn:
            src_conn.backup(dst_conn)
        dst_conn.close()
        src_conn.close()
        
        # 2. Validar integridade do backup gerado
        print("2. Validando integridade do backup (PRAGMA integrity_check)...")
        b_conn = sqlite3.connect(backup_file)
        b_cur = b_conn.cursor()
        res = b_cur.execute("PRAGMA integrity_check").fetchall()
        fk_res = b_cur.execute("PRAGMA foreign_key_check").fetchall()
        t_count = b_cur.execute("SELECT count(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'").fetchone()[0]
        b_conn.close()
        
        print(f"   Integrity check: {res[0][0]}")
        print(f"   Foreign key violations: {len(fk_res)}")
        print(f"   Tabelas no backup: {t_count}")
        assert res[0][0] == "ok", "Integrity check falhou no backup gerado"
        assert len(fk_res) == 0, "Violacoes de FK no backup gerado"
        
        # 3. Teste de backup corrompido (header corrompido)
        corrupted_file = os.path.join(temp_dir, "backup_corrompido.db")
        shutil.copyfile(backup_file, corrupted_file)
        with open(corrupted_file, "r+b") as fp:
            fp.seek(0)
            fp.write(b"CORRUPTED_NOT_SQLITE_HEADER!")
            
        print("3. Testando deteccao de corrupcao no arquivo alterado...")
        c_conn = None
        try:
            c_conn = sqlite3.connect(corrupted_file)
            c_cur = c_conn.cursor()
            c_res = c_cur.execute("PRAGMA integrity_check").fetchall()
            corrompido_detectado = c_res[0][0] != "ok" or len(c_res) > 1
        except Exception as ex:
            corrompido_detectado = True
            c_res = [str(ex)]
        finally:
            if c_conn:
                c_conn.close()
            c_conn = None
            c_cur = None
            gc.collect()

        print(f"   Deteccao de corrupcao no backup adulterado: {corrompido_detectado} (resultado: {c_res[:1]})")
        assert corrompido_detectado, "Falha: corrupcao nao foi detectada"
        
        # 4. Teste de Restore simulado em destino isolado
        restored_file = os.path.join(temp_dir, "restored_test.db")
        print(f"4. Restaurando backup valido para {restored_file}...")
        r_src = sqlite3.connect(backup_file)
        r_dst = sqlite3.connect(restored_file)
        with r_dst:
            r_src.backup(r_dst)
        r_src.close()
        r_dst.close()
        
        # 5. Validar integridade da base restaurada
        print("5. Validando base restaurada...")
        r_conn = sqlite3.connect(restored_file)
        r_cur = r_conn.cursor()
        r_res = r_cur.execute("PRAGMA integrity_check").fetchall()
        r_t_count = r_cur.execute("SELECT count(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'").fetchone()[0]
        r_conn.close()
        
        print(f"   Integrity check pos-restore: {r_res[0][0]}")
        print(f"   Tabelas pos-restore: {r_t_count}")
        assert r_res[0][0] == "ok", "Restore falhou no integrity_check"
        assert r_t_count == t_count, "Tabelas restauradas divergentes"
        
        print("\n============================================================")
        print("RESULTADO DO TESTE DE BACKUP/RESTORE/INTEGRIDADE: PASS")
        print("============================================================\n")
    finally:
        gc.collect()
        shutil.rmtree(temp_dir, ignore_errors=True)

if __name__ == "__main__":
    test_backup_restore()
