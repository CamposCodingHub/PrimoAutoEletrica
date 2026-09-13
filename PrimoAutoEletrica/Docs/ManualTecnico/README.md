# Manual Tecnico - Primo Auto Eletrica

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

Este manual resume como o projeto esta organizado, como o banco funciona, quais servicos sustentam a aplicacao e como rodar validacoes, pacote limpo e release.

## Arquivos deste manual

- `01_ESTRUTURA_PROJETO.md`: estrutura do projeto e responsabilidades principais.
- `02_BANCO_DADOS_SERVICOS.md`: banco de dados, migracoes e servicos centrais.
- `03_TESTES_PACOTE_RELEASE.md`: testes, pacote limpo, instalador, atualizacao e release.

## Regra de manutencao

Antes de uma mudanca grande, rode build e smoke filtrado do modulo alterado. Antes de liberar para uso real, rode a validacao completa e atualize o checklist final de qualidade.
