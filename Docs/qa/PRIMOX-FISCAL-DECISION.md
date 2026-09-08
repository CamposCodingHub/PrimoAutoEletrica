# PRIMOX — Fiscal Decision Record 1.0

**Data:** 2026-09-08  
**Tipo:** Decisão de produto + arquitetura (sem implementação)  
**Referências:**  
`PRIMOX-FISCAL-PROVIDER-AUDIT-1.0.md` · `PRIMOX-FISCAL-PROVIDER-COMPARISON.md` · `PRIMOX-FISCAL-PROVIDER-ARCHITECTURE.md` · `PRIMOX-FISCAL-HOMOLOGATION-PLAN.md`

---

## Decisão

| Item | Escolha |
|------|---------|
| SEFAZ direta vs Provedor | **Provedor (Opção B)** |
| Provedor principal | **Focus NFe** |
| Alternativa | **TecnoSpeed PlugNotas** |
| Terceira (NFS-e pesado) | **eNotas / Nota Gateway** |
| Evitar agora | **Nuvem Fiscal** (risco de continuidade) |
| Certificado | **A1 eCNPJ** |
| Documentos onda 1 | **NF-e homologação** |
| Documentos onda 2 | NFC-e (PDV) |
| Documentos onda 3 | NFS-e / NFS-e Nacional |
| Implementar agora? | **NÃO** |
| Próxima fase? | **GO CONDICIONAL** |

---

## GO / NO-GO

### GO CONDICIONAL para iniciar implementação futura

Condições:

1. Douglas/produto confirma Focus (ou escolhe PlugNotas por motivo comercial).  
2. Certificado A1 de homologação disponível.  
3. Trial Focus aberto.  
4. Escopo travado: NF-e **homolog only** + `IFiscalProvider`.  
5. Nova branch de feature; **não** alterar significado de `v1.0.0`.

### NO-GO imediato

- Emitir em produção  
- Contratar Enterprise sem volume  
- Implementar NFS-e antes de NF-e  
- SEFAZ direta  
- Usar Nuvem Fiscal sem prova de continuidade  

---

## Scores (Focus)

| Dimensão | Nota |
|----------|------|
| Técnica | 9/10 |
| Comercial | 9/10 |
| Arquitetural | 9/10 |
| Escalabilidade | 8/10 |
| Fiscal | 9/10 |
| Risco | MÉDIO |

---

## Custo esperado (ordem de grandeza — Focus Solo)

Oficina 1 CNPJ, até ~100 notas/mês: **~R$ 89,90/mês** + certificado A1 anual.  
SaaS multi-CNPJ: plano **Growth (~R$ 548/mês / 4.000 notas)** ou Enterprise (consultar).

Preços públicos consultados em 2026-09-08; **revalidar na contratação**.

---

## Pergunta do dono — resposta

> Qual provedor usar, custo, documentos, homologação, riscos e integração?

**Usar Focus NFe** atrás de `IFiscalProvider`; começar por NF-e em homologação; NFC-e e NFS-e depois; custo Solo adequado a oficina; risco principal = dependência de fornecedor e complexidade NFS-e municipal — mitigados por abstração e ondas. **Não emitir nada até gates do plano de homologação.**

---

## Assinatura de decisão

| Papel | Status |
|-------|--------|
| Auditoria técnica | COMPLETA |
| Aceite produto (Douglas) | **PENDENTE** |
| Contratação Focus | PENDENTE |
| Início de código | BLOQUEADO até aceite |

**PARE.**
