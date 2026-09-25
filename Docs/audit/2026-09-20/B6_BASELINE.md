# PRIMOX WORKSHOP — B6 BASELINE AUDIT
**Data:** 2026-09-25  
**Hora:** 05:30 BRT  
**Fase:** B6 — Piloto Comercial Controlado (30 Dias)  
**Status:** PASS — BASELINE CONFERIDA E PROTEGIDA  

---

## 1. Identificação do Ambiente e Sistema
- **Máquina:** Estação de Trabalho Oficina / Host
- **Sistema Operacional:** Microsoft Windows 11 Pro 64-bit (Build 26200)
- **Processador:** Intel(R) Xeon(R) CPU E5-2697A v4 @ 2.60GHz (16 Cores, 32 Threads)
- **Memória RAM:** 32.0 GB (34.359.738.368 bytes)
- **Disco Local C:**: 475.89 GB (138.12 GB livres)
- **.NET SDK / Runtime:** 10.0.302
- **Versão PRIMOX Workshop:** 1.0.0 (Assembly 1.0.0.0)

---

## 2. Controle de Versão (Git Baseline)
- **Branch Ativo:** `audit/product-discovery-2026-09`
- **Commit Baseline B5.5:** `11b56d466f6a1ca327546ca5c0e1472dc6c016ac`
- **Branch Main:** Preservado e Intacto no commit `29b19b16d0e6e3413bdba20c505e20c992596c24`
- **Status do Repositório:** Limpo de modificações não auditadas.

---

## 3. Integridade do Banco de Dados Protegido (Regra Zero)
- **Caminho:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db`
- **SHA-256:** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`
- **IsReadOnly:** `True` (Atributo de somente leitura mantido rigorosamente)
- **Tamanho:** `20.201.472 bytes`
- **Veredito:** Intacto. Zero escritas, zero migrações destrutivas.

---

## 4. Banco Operacional do Piloto Comercial
- **Caminho:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto_operacional.db`
- **Modo SQLite:** WAL (Write-Ahead Logging), cache compartilhado
- **PRAGMA integrity_check:** `ok`
- **PRAGMA foreign_key_check:** `0 violations`
- **Credencial de Acesso Homologada:** `admin@primoauto.com` / `Julia#258` (PBKDF2-HMAC-SHA256, 600.000 iterações)

---

## 5. Pacote de Instalação Comercial
- **Instalador Oficial:** `artifacts/installer/PRIMOX-Workshop-Setup-1.0.0.exe`
- **SHA-256:** `5C6DA93EA795CE0F1611A7EEBF1AD08473F4078422D5277B609F3CAB80CF524C`
- **Tamanho:** `58.655.903 bytes` (55.94 MB)
- **Destino da Instalação:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\`
- **Atalho da Área de Trabalho:** `C:\Users\campo\OneDrive\Desktop\PRIMOX Workshop.lnk`

---

## 6. Snapshot de Segurança
- **Último Backup Atômico Criado:** `PrimoAutoEletrica_Backup_2026-09-25_05-25-45.db`
- **SHA-256:** `02562F4630A4FC8542D981ADF6301F0C4E1F90EB169F5F65CF33F57AE268B714`
- **Tamanho:** `21.131.264 bytes`
