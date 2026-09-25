# PRIMOX Workshop — Fase B5.5: Auditoria de Atualização de Versão

**Data:** 2026-09-24  
**Escopo:** Simulação de Ciclo de Update de Aplicação Preservando Dados e Atalhos

---

## 1. Validação de Atualização

- **Versão Homologada:** 1.0.0.0 (ProductVersion 1.0.0).
- **Preservação de Dados:** Atualização de binários no diretório `%LOCALAPPDATA%\PrimoAutoEletrica\App` preserva integralmente os arquivos de dados em `%LOCALAPPDATA%\PrimoAutoEletrica`.
- **Atalho da Área de Trabalho:** Atalho `PRIMOX Workshop.lnk` mantém o destino do executável atualizado sem apontar para arquivos temporários.
- **Rollback:** Procedimento testado e comprovado caso ocorra interrupção de instalação.
