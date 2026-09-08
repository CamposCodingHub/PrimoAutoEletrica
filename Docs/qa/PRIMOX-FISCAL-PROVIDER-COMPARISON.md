# PRIMOX — Fiscal Provider Comparison Matrix 1.0

**Consulta:** 2026-09-08  
**Regra:** sem inventar. Lacunas = `NÃO CONFIRMADO`.

## Fontes (prioridade)

| Fonte | URL | Data | Uso |
|-------|-----|------|-----|
| Focus preços | https://focusnfe.com.br/precos/ | 2026-09-08 | Planos Solo/Start/Growth/Retail |
| Focus ambiente | https://doc.focusnfe.com.br/reference/ambiente | 2026-09-08 | Homolog/Prod URLs |
| Focus home/NFSe | https://focusnfe.com.br/ · /produtos/nota-fiscal-servico-nfse/ | 2026-09-08 | Docs, webhooks, municípios, A1 |
| TecnoSpeed PlugNotas | https://tecnospeed.com.br/plugdfe/plugnotas/ | 2026-09-08 | Cobertura, webhooks, SaaS |
| TecnoSpeed primeiros passos | https://atendimento.tecnospeed.com.br/hc/pt-br/articles/23715383551767 | 2026-09-08 | Homolog/produção |
| Nuvem Fiscal docs limites | https://dev.nuvemfiscal.com.br/docs/limites | 2026-09-08 | Cotas Fiscal I/II |
| Nuvem Fiscal suporte webhook | https://suporte.nuvemfiscal.com.br/t/implementacao-do-recurso-de-webhooks-callback/2826 | histórico | Polling; webhooks ausentes (à época) |
| Nuvem Fiscal site | https://www.nuvemfiscal.com.br/ | 2026-09-08 | **HTTP 500** nesta consulta |
| DotCompany (secundária) | https://dotcompany.com.br/api | 2026-09-08 | Afirma encerramento Nuvem Fiscal 31/07/2026 — **não prova oficial** |
| eNotas Gateway portal | https://portal.enotasgw.com.br/ | 2026-09-08 | API REST NFS-e/NF-e |
| Nota Gateway | https://notagateway.com.br/ · docs.enotasgw.com.br | 2026-09-08 | NF-e/NFC-e |
| Notaas | https://notaas.com.br/ · https://docs.notaas.com.br/ | 2026-09-08 | Marketing API; maturidade software house **NÃO CONFIRMADA** vs Focus |
| NFS-e Nacional (gov) | https://www.gov.br/nfse/pt-br/biblioteca/documentacao-tecnica/ | 2026-09-08 | ADN / DPS / API nacional |
| Código PRIMOX | `NFeService`, `NFeEmissaoService` 0b | 2026-09-08 | Verdade de produto |

---

## Matriz comparativa

| Critério | Focus NFe | Nuvem Fiscal | TecnoSpeed PlugNotas | Notaas | eNotas / Nota Gateway | Melhor p/ PRIMOX |
| -------- | --------- | ------------ | -------------------- | ------ | --------------------- | ---------------- |
| NF-e | SIM (oficial) | SIM (docs) | SIM (oficial) | SIM (site) | SIM (docs) | Focus / PlugNotas |
| NFC-e | SIM | SIM | SIM | SIM (site) | SIM (Nota Gateway) | Focus / PlugNotas |
| NFS-e | SIM (>3000 mun.) | SIM | SIM (>2200 mun.) | SIM (foco) | SIM (histórico forte) | Focus / eNotas / PlugNotas |
| NFS-e Nacional | SIM (site/docs RT) | NÃO CONFIRMADO nesta consulta | SIM (NT 1.01 / IBS-CBS) | NÃO CONFIRMADO | NÃO CONFIRMADO | Focus / PlugNotas |
| API REST | SIM JSON | SIM | SIM JSON | SIM | SIM | Empate |
| Webhooks | SIM (incluídos no site preços) | Historicamente NÃO (polling); status atual **NÃO CONFIRMADO** | SIM (tempo real) | SIM (HMAC) | SIM (portal) | Focus / PlugNotas |
| Homologação | SIM URL dedicada | Sandbox + cotas | Sandbox + flag produção | Sandbox | NÃO CONFIRMADO detalhe | Focus |
| Multi-CNPJ | SIM (Start/Growth) | SIM (cotas) | SIM (software house) | Parcial (site) | SIM (empresas) | Growth / PlugNotas |
| Certificado | **A1 apenas** | A1 (upload docs MCP) | A1 recomendado | NÃO CONFIRMADO | NÃO CONFIRMADO | Focus/PlugNotas A1 |
| Cancelamento | SIM (capacidade API) | SIM | SIM | SIM | SIM | Empate |
| Consulta | SIM | SIM (polling id) | SIM | SIM | SIM | Empate |
| XML | SIM armazenamento | SIM | SIM | SIM | SIM | Empate |
| DANFE/PDF | SIM (site NFSe PDF) | NÃO CONFIRMADO | NÃO CONFIRMADO detalhe | SIM (site) | SIM PDF | Focus |
| Contingência | NÃO CONFIRMADO detalhe NFC-e | NÃO CONFIRMADO | SIM (NFC-e contingência) | NÃO CONFIRMADO | NÃO CONFIRMADO | PlugNotas |
| Documentação | Boa (portal v2) | Boa (quando online) | Boa | Em evolução | Boa NFS-e | Focus |
| Suporte | Ticket dev-to-dev | Fórum/central | Dev-to-dev + onboarding | NÃO CONFIRMADO | Ticket | PlugNotas/Focus |
| Custo | **PÚBLICO** Solo 89,90… | Fiscal I ~R$180/10k ops (suporte 2025) | **PREÇO NÃO PÚBLICO** | Freemium marketing | Planos eNotas (secundário) | Focus (clareza) |
| Escalabilidade | Growth/Enterprise | Fiscal II 100k | Pensado multi-cliente | NÃO CONFIRMADO | NÃO CONFIRMADO | PlugNotas/Focus |
| SaaS futuro | Adequado Growth | Risco continuidade | Excelente fit | Marketing SaaS | Adequado | PlugNotas / Focus |
| Dependência | Médio | **ALTO** (site 500 + rumor shutdown) | Médio–Alto | Médio–Alto (player menor) | Médio | Focus |
| Complexidade integração | Baixa–média | Média | Média | Baixa (claim) | Média | Focus |
| Continuidade 2026 | Operacional (docs OK) | **ALTO RISCO** | Operacional | Operacional aparente | Operacional | Focus / PlugNotas |

---

## Cenários de custo (Focus — público)

Ver auditoria principal. Resumo Solo 1 CNPJ:

| Notas/mês | Estimativa |
|-----------|------------|
| 10–100 | R$ 89,90 |
| 200 | ~R$ 99,90 |
| 1000 | ~R$ 179,90 |

PlugNotas / Enterprise Focus: **PREÇO NÃO PÚBLICO**.

Certificado A1: custo separado (AR ICP-Brasil).

---

## Ranking PRIMOX

1. **Focus NFe** — melhor equilíbrio preço público + cobertura + homolog + webhooks + multi-CNPJ.  
2. **TecnoSpeed PlugNotas** — melhor se o caminho SaaS multi-cliente for imediato e houver orçamento negociado.  
3. **eNotas/Nota Gateway** — forte se NFS-e municipal for o dolor principal do piloto.  
4. **Notaas** — interessante freemium; validar maturidade NF-e/NFC-e e SLA antes de apostar o produto.  
5. **Nuvem Fiscal** — **não escolher agora**.

---

## SEFAZ direta (referência)

| Critério | Avaliação |
|----------|-----------|
| Custo R$ gateway | Baixo |
| Custo engenharia/manutenção | Muito alto |
| Contingência UF | Complexo |
| NFS-e municípios | Explosão de layouts |
| Adequação time PRIMOX | **NÃO RECOMENDADO** |

Mantém-se **Opção B (provedor)**.
