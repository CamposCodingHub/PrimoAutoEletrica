# PRIMOX Workshop — Commercial Readiness 1.0

**Produto:** PRIMOX Workshop **desktop** 1.0.0  
**Tag:** `v1.0.0` → `a4ad6fe`  
**Auditoria:** Product Truth 1.0 · 2026-09-08  

Legenda: `READY` | `READY WITH LIMITATION` | `PILOT ONLY` | `NOT READY` | `FUTURE` | `NOT APPLICABLE`

| Área | Classificação | Nota |
|------|---------------|------|
| Produto desktop oficina | READY WITH LIMITATION | Core CRUD/OS/PDV/Estoque/Financeiro reais |
| Instalação (Inno/E2E) | READY WITH LIMITATION | 15C PASS; signing ausente |
| Desinstalação | READY WITH LIMITATION | Validado em cleanup legado |
| Backup | READY | UI + serviço + E2E |
| Restore | READY | UI + serviço |
| Banco SQLite | READY WITH LIMITATION | integrity OK; 27 vs ~32 migrations históricas |
| Segurança login | READY WITH LIMITATION | PBKDF2+lockout; 2FA **não** no fluxo login |
| Login | READY | — |
| CRUD Clientes/Veículos | READY | Exhaustive + domínio |
| Ordens de Serviço | READY WITH LIMITATION | Kanban/transições: usar com piloto |
| Orçamentos | READY | — |
| Agenda | READY WITH LIMITATION | Conflitos: não overclaim |
| Estoque | READY WITH LIMITATION | Paginação em memória |
| Financeiro | READY WITH LIMITATION | Sem DRE enterprise completo |
| PDV + Caixa | READY WITH LIMITATION | Print nativo limitado |
| Relatórios | READY WITH LIMITATION | Export real; alguns truncamentos |
| NF-e import | READY | — |
| NF-e emissão | NOT READY | NÃO IMPLEMENTADO |
| API REST | PILOT ONLY | Minimal; sem auth real |
| LGPD | READY WITH LIMITATION | Soft delete/consent; não compliance SaaS |
| Notificações SMS/Twilio | NOT READY | PLACEHOLDER |
| WhatsApp share (wa.me) | READY | — |
| Atualização automática | NOT READY | Deploy/manual/script |
| Assinatura digital código | NOT READY | NOT CONFIGURED |
| Documentação | READY WITH LIMITATION | PROJECT_STATUS historicamente inflado — reconciliado nesta auditoria |
| Suporte | PILOT ONLY | Depende operação humana |
| Licenciamento SaaS | FUTURE / NOT APPLICABLE | Não iniciado |
| Multi-usuário (local) | READY WITH LIMITATION | Sessões/locks existem; escala limitada |
| Multi-filial real | NOT READY | SCAFFOLD |
| Cloud / sync remoto | NOT READY / FUTURE | — |
| SaaS | NOT APPLICABLE / FUTURE | Explicitamente fora |

---

## Matriz “Pode vender?”

| Pergunta | Resposta |
|----------|----------|
| Pode instalar em oficina própria? | **SIM** |
| Pode ser usado por oficina piloto? | **SIM** |
| Pode ser vendido como desktop? | **SIM — COM LIMITAÇÕES** (sem emissão SEFAZ; sem multi-filial real; update manual; signing ausente) |
| Pode ser vendido como SaaS? | **NÃO** |
| Pode emitir NF-e real? | **NÃO** |
| Pode operar multi-filial real? | **NÃO** |
| Pode funcionar offline com sincronização remota? | **NÃO** |
| Pode suportar 100+ oficinas? | **NÃO / NECESSITA ARQUITETURA** (produto é desktop single-workshop) |

---

## Separação de métricas (obrigatória)

| Métrica | Valor | Significado |
|---------|------:|-------------|
| UI BUTTON TEST (executáveis) | 1909/1909 PASS | Cobertura de **botões testáveis**, não do domínio |
| PRODUCT FUNCTIONAL COVERAGE | ver Truth Matrix | Mistura REAL / PARCIAL / SCAFFOLD |
| ARCHITECTURAL COMPLETENESS | Parcial | API/auth/sync/fiscal incompletos |
| DOCUMENTATION ACCURACY | Melhorada após Truth Audit | Histórico PROJECT_STATUS inconsistente |
| COMMERCIAL READINESS | Desktop piloto/comercial limitado | Não SaaS |

**Não** converter 100% tested/executable em “produto 100% completo”.
