# PRIMOX Workshop — B5.4: Homologação do Pacote Instalador

**Data:** 2026-09-24 20:45  
**Status:** **PASS**

---

## 1. Artefato do Instalador Oficial

- **Arquivo:** `artifacts/installer/PRIMOX-Workshop-Setup-1.0.0.exe`
- **Tamanho:** 55,93 MB (58.647.784 bytes)
- **SHA-256:** `17DAEB5F8B2B058C52D1FCBAC3246E47B65A9F4490F3515CB109F84C8E697FE2`
- **Compilador:** Inno Setup 6 (ISCC 6.7.3)
- **Modo:** Standalone self-contained win-x64 (.NET 10 incluído)
- **Banco de Produção:** **NÃO EMPACOTADO** (banco de produção `primoauto.db` preservado isoladamente).

---

## 2. Ciclo de Instalação e Preservação

1. **Instalação:** Criação de diretórios em `%LOCALAPPDATA%\PrimoAutoEletrica\App\` e `%PROGRAMFILES%\PRIMOX\`.
2. **Atalho de Desktop:** Gerado apontando para `PrimoAutoEletrica.exe`.
3. **Backup pré-update:** Criação de backup automático da instalação anterior em `Backups/BeforeDeploy/`.
4. **Preservação de Dados:** Dados do usuário e histórico permanecem intactos.
