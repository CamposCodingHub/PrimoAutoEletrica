# PRIMOX — Help Coverage Matrix 1.0

> **DOCUMENTO REESCRITO EM CAMADAS — 2026-09-13**
>
> | Camada | Uso |
> |--------|-----|
> | **Estado atual** | Fonte operacional hoje · ver também `Docs/CURRENT-TRUTH.md` e NET10-26 |
> | **Avanços desta fase (histórico)** | Registro do que esta execução entregou — **não** sobrescrever mentalmente o estado atual |
>
> HEAD de referência pós-NET10-26: `1372e11` · TFM `net10.0-windows` · Branch `migration/net10`
> Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md`

## Estado atual (pós NET10-26 · 2026-09-13)

| Item | Valor |
|------|-------|
| Branch | `migration/net10` |
| HEAD fiscal foundation | `1372e11` |
| TFM | `net10.0-windows` |
| Unit | **194/194** |
| QaEngine | **43/43** |
| DeepQa | **6/6** (baseline NET10-26) |
| Fiscal LIVE / WhatsApp API / Code signing | **BLOCKED_EXTERNAL** |
| Calendar Dark | Mitigado (`CalendarContrastHealer`) — não citar KNOWN LIMITATION antigo como atual |
| NF-e | PARTIAL + TESTED (Focus path + Fake) |
| NFC-e / NFS-e | SCAFFOLD + FAKE_ONLY |
| DANFE | PDF informativo (≠ SEFAZ oficial) |
| Multiempresa fiscal | IMPLEMENTED + TESTED (DB) |

**Claims abaixo sobre net6, “emissão NÃO IMPLEMENTADO”, Unit 173, DANFE/cancel NI, Calendar Dark KNOWN LIMITATION, etc. pertencem ao registro histórico da fase.**

---

## Avanços desta fase (registro histórico — preservar)

**Etapa:** Codebase Sanitization 1.0 / Help Center 1.0  
**Idioma principal do conteúdo:** pt-BR (UI do shell continua i18n pt/en/es; textos longos da Ajuda = pt-BR)  
**Screenshots/vídeos versionados:** não (ilustrações mock na própria Ajuda; captura real = evidência local opcional)

| Módulo | Cargo | Procedimento | Tela | Botão / caminho | Explicação | Exemplo | Erro | Screenshot | Vídeo | Testado | Idioma | Status |
|--------|-------|--------------|------|-----------------|------------|---------|------|------------|-------|---------|--------|--------|
| Login | Todos | Entrar | Login | Entrar | primeiros-10min | QA user | informar-problema | mock | — | DeepQa/Exhaustive | pt-BR | DONE |
| Dashboard | Gerente/Prop. | Visão do dia | Dashboard | menu | modulo-dashboard | — | problemas | mock | — | Exhaustive | pt-BR | DONE |
| Clientes | Recepção/Adm | Cadastrar | Clientes | Novo | criar-cliente | QA_HELP_CLIENTE | erros-evitar | mock | — | CompleteUi Help | pt-BR | DONE |
| Veículos | Recepção | Cadastrar | Veículos | Novo | registrar-veiculo | QAH1A23 | erros | mock | — | Exhaustive | pt-BR | DONE |
| Orçamentos | Recepção | Criar | Orçamentos | Novo | criar-orcamento | — | erros | mock | — | Exhaustive | pt-BR | DONE |
| OS | Técnico | Abrir/atualizar | OS/Kanban | Nova OS | criar-os | Scania ex. | erros | mock | — | Exhaustive | pt-BR | DONE |
| PDV | Caixa | Vender | PDV | Abrir caixa / PIX | venda-pdv | — | erros | mock | — | Exhaustive | pt-BR | DONE |
| Financeiro | Financeiro | Baixa | Financeiro | Baixar | usar-financeiro | — | erros | mock | — | Exhaustive | pt-BR | DONE |
| Estoque | Adm | Consultar | Estoque | — | modulo-estoque | — | problemas | mock | — | Exhaustive | pt-BR | DONE |
| Relatórios | Gerente | Gerar | Relatórios | Gerar/Exportar | gerar-relatorio | — | problemas | mock | — | Exhaustive | pt-BR | DONE |
| NF-e | Adm | Importar XML | Import NF-e | Escolher XML | modulo-nfe | — | limites | mock | — | Exhaustive | pt-BR | DONE |
| Config | Prop. | Backup | Configurações | Backup | fazer-backup | — | problemas | — | — | Exhaustive | pt-BR | DONE |
| Ajuda | Todos | Pesquisar/navegar | Help | SearchBox | Comece aqui | cliente | — | theme | — | CompleteUiHelp | pt-BR | DONE |
| Limites | Todos | Honestidade | Ajuda | limites-produto | limites-produto | — | — | — | — | CompleteUiHelp | pt-BR | DONE |
| Cargo Caixa | Caixa | Rotina | PDV | — | cargo-caixa | — | — | — | — | Manual+nav | pt-BR | DONE |
| Cargo Técnico | Eletricista | Rotina | OS/Kanban | — | cargo-eletricista | — | — | — | — | Manual+nav | pt-BR | DONE |
| Cargo Financeiro | Financeiro | Rotina | Financeiro | — | cargo-financeiro | — | — | — | — | Manual+nav | pt-BR | DONE |
| Cargo Adm | Recepção | Rotina | Cadastros | — | cargo-recepcao | — | — | — | — | Manual+nav | pt-BR | DONE |
| Cargo Gerente | Gerente | Rotina | Dashboard | — | cargo-gerente | — | — | — | — | Manual+nav | pt-BR | DONE |
| Cargo Proprietário | Proprietário | Rotina | Config/Relatórios | — | cargo-proprietario | — | — | — | — | Manual+nav | pt-BR | DONE |
| Dia de trabalho | Todos | Mapa rotina | — | — | dia-trabalho | — | — | — | — | Manual | pt-BR | DONE |
| Problemas | Todos | Troubleshooting | Ajuda | — | problemas-resolver | — | sim | — | — | Manual | pt-BR | DONE |

## Lacunas conscientes

- Tradução completa en-US/es-ES dos artigos longos: **não** (declarado)
- Vídeos curtos: **não versionados** nesta etapa
- Screenshots reais de produção: não (usar mocks + QA)
- Modo tutorial overlay na UI: futuro (não implementado)
