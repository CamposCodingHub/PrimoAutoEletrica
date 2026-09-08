# PRIMOX FISCAL PROVIDER DECISION & HOMOLOGATION AUDIT 1.0

```text
VERSION: 1.0.0 (comercial — sem alteração)
TAG: v1.0.0 → a4ad6fe (INTACTA)
HEAD: 037614b (docs sanitization)
BRANCH: main
WIP: Scripts/Atualizar-PrimoAuto.bat + Deploy-ToInstalledApp.ps1 PRESERVADOS
DATA CONSULTA: 2026-09-08
```

## DECISION

```text
CAMINHO: Opção B — Provedor fiscal (NÃO SEFAZ direta)
PROVEDOR RECOMENDADO: Focus NFe
ALTERNATIVA: TecnoSpeed PlugNotas
TERCEIRA: eNotas / Nota Gateway (avaliar comercialmente)
NUVEM FISCAL: ALTO RISCO / POSSÍVEL DESCONTINUAÇÃO — NÃO RECOMENDAR agora
IMPLEMENTAÇÃO NESTA ETAPA: NÃO
DECISÃO DE PRÓXIMA FASE: GO CONDICIONAL (após aceite do dono + trial Focus + certificado A1 de homologação)
STATUS: FISCAL ARCHITECTURE DECISION READY
```

---

## CURRENT FISCAL TRUTH

Hierarquia: código > banco > testes > docs.

| Capacidade | Classificação | Evidência |
|------------|---------------|-----------|
| **NF-e importação (entrada)** | **REAL + TESTADA** | `NFeService`, `ImportarNFeControl`, QaEngine NFeImport*, Exhaustive |
| Conta a pagar a partir de import | REAL + TESTADA | `FinanceiroDatabaseService.RegistrarContaPagarNFe` |
| Persistência histórico import | REAL | `ImportacaoRepository`, schema `ChaveAcesso` |
| **NF-e emissão (saída)** | **NÃO IMPLEMENTADA** | `NFeEmissaoService.cs` = **0 bytes** |
| Assinatura / SEFAZ / protocolo / DANFE emissão | NÃO IMPLEMENTADA | Sem X509/SignedXml em uso |
| Cancelamento / inutilização / CC-e emissão | NÃO IMPLEMENTADA | — |
| Contingência emissão | NÃO IMPLEMENTADA | — |
| **NFC-e** | **NÃO IMPLEMENTADA** | Docs estudo apenas |
| **NFS-e / NFS-e Nacional** | **NÃO IMPLEMENTADA** | Docs estudo apenas |
| Certificado A1/A3 | NÃO IMPLEMENTADA | Package `Cryptography.Xml` ORPHAN |
| Provider fiscal / HTTP fiscal | NÃO IMPLEMENTADA | Sem HttpClient fiscal |
| SEFAZ direta | FORA DO ESCOPO (recomendado) | Arquitetura FASE A |

**IMPORTAR ≠ EMITIR.** O produto 1.0.0 vende importação; emissão é futuro.

---

## NF-e / NFC-e / NFS-e (necessidade oficina)

| Operação PRIMOX | Documento típico | Prioridade |
|-----------------|------------------|------------|
| Compra peças (já existe import) | NF-e entrada (XML fornecedor) | KEEP atual |
| Venda peças B2B / OS com peça faturada | NF-e saída | P0 após GO |
| Venda balcão PDV consumidor | NFC-e | P1 |
| Mão de obra / serviço OS | NFS-e (municipal ou Nacional) | P1–P2 (município-dependente) |
| Devolução | Eventos / notas específicas | P2 |
| DANFE/DANFSE | Representação | Com emissão |

---

## RECOMMENDED PROVIDER

### Focus NFe

| Nota | Score |
|------|-------|
| Técnica | 9/10 |
| Comercial | 9/10 |
| Arquitetural | 9/10 |
| Escalabilidade | 8/10 |
| Fiscal (cobertura docs) | 9/10 |
| **Risco** | **MÉDIO** (vendor lock-in mitigável via `IFiscalProvider`) |

**Por quê**

1. Preços **públicos** e adequados a oficina (Solo R$ 89,90 / 100 notas) e a SaaS futuro (Growth CNPJs ilimitados).
2. NF-e + NFC-e + NFS-e + NFS-e Nacional + recebimento DF-e.
3. Homologação e produção com URLs claras.
4. Webhooks incluídos (site oficial).
5. Multi-CNPJ nativo; sem setup/fidelidade declarados.
6. Certificado **A1** (adequado a cloud; A3 não aceito pelo provedor — alinhado a SaaS).
7. Documentação REST/JSON ativa (`doc.focusnfe.com.br`).

---

## ALTERNATIVE PROVIDER

### TecnoSpeed PlugNotas

| Nota | Score |
|------|-------|
| Técnica | 9/10 |
| Comercial | 6/10 (PREÇO NÃO PÚBLICO) |
| Arquitetural | 9/10 (pensado para software house) |
| Escalabilidade | 9/10 |
| Fiscal | 9/10 |
| Risco | MÉDIO–ALTO (dependência + opacidade de preço) |

Forte para multi-tenant futuro; webhooks; NFS-e Nacional; sandbox. Exige orçamento comercial.

### Terceira opção: eNotas / Nota Gateway

Histórico forte em NFS-e; API REST; webhooks. Preços públicos do produto eNotas (fonte secundária 2026) — **confirmar no comercial oficial** antes de escolher. Bom se NFS-e for o gargalo municipal do cliente piloto.

### Nuvem Fiscal

**NÃO RECOMENDAR neste momento.** Site oficial retornou erro 500 em 2026-09-08; fonte concorrente (DotCompany) afirma encerramento em 31/07/2026. Tratar como **ALTO RISCO / NÃO CONFIRMADO oficialmente** — não basear arquitetura nela.

---

## EXPECTED COST (Focus — preços públicos 2026-09-08)

Fonte: https://focusnfe.com.br/precos/  
Certificado A1 eCNPJ: tipicamente ~R$ 150–400/ano (mercado ICP-Brasil — **NÃO CONFIRMADO** preço exato; comprar de AR autorizada).  
Município NFS-e novo (Focus): R$ 199 (site Focus).

### Oficina pequena (1 CNPJ — Plano Solo R$ 89,90 + R$ 0,10 excedente)

| Volume | Estimativa mensal Focus* |
|--------|---------------------------|
| 10 notas | ~R$ 89,90 (pacote 100) |
| 50 notas | ~R$ 89,90 |
| 100 notas | ~R$ 89,90 |
| 200 notas | ~R$ 89,90 + 100×0,10 = **~R$ 99,90** |

\*Somente gateway; exclui certificado e impostos sobre o serviço.

### Oficina média

| Volume | Estimativa |
|--------|------------|
| 500 | Solo estoura: 400×0,10 + 89,90 ≈ **R$ 129,90** ou avaliar Retail/Start |
| 1.000 | ≈ R$ 89,90 + 900×0,10 = **~R$ 179,90** (Solo) |

### SaaS futuro (Growth R$ 548 / 4.000 notas / CNPJs ilimitados)

| Empresas | Notas/mês (hipótese 50/empresa) | Pacote |
|----------|----------------------------------|--------|
| 10 | 500 | Growth sobra |
| 50 | 2.500 | Growth sobra |
| 100 | 5.000 | Growth + ~1.000×0,12 ≈ **~R$ 668** |
| 500 | 25.000 | Enterprise — **PREÇO NÃO PÚBLICO** |
| 1.000 | 50.000+ | Enterprise — **PREÇO NÃO PÚBLICO** |

**Custo total real = gateway + certificado A1 + (opcional) município NFS-e + engenharia + suporte interno.**

---

## ARCHITECTURE

Ver `Docs/architecture/PRIMOX-FISCAL-PROVIDER-ARCHITECTURE.md`.

```text
PRIMOX Desktop
  → Application Services (PDV / OS / Financeiro)
  → IFiscalProvider (abstração — NÃO implementar agora)
  → FocusNfeProvider (futuro) | PlugNotasProvider (alternativa)
  → Homolog / Produção
  → SEFAZ / Prefeitura / NFS-e Nacional
  → Webhook ou Polling
  → Persistência local (XML, chave, protocolo, status)
  → DANFE/DANFSE
```

Estados: Rascunho → Validando → Enviando → Processando → Autorizada | Rejeitada | Cancelada | Denegada | Contingência | Erro.  
**HTTP 200 ≠ autorizada.**

Idempotência: `FiscalOperationId` (Guid estável por tentativa de negócio).

---

## SECURITY

- API token / certificado A1 / senha PFX: DPAPI ou Credential Manager; **nunca** Git/XAML/logs.
- Homolog ≠ Produção (URLs e tokens separados).
- Webhook: HMAC/secret + idempotência de evento.
- Desktop-only hoje: polling + consulta por id; webhook exige endpoint público (API futura).

---

## HOMOLOGATION

Ver `Docs/qa/PRIMOX-FISCAL-HOMOLOGATION-PLAN.md`.  
DEV → HOMOLOG (Focus) → PRODUÇÃO (bloqueada até checklist PASS).  
**Nenhuma NF real nesta auditoria.**

---

## RISKS

| Risco | Nível | Mitigação |
|-------|-------|-----------|
| Vendor lock-in | Médio | `IFiscalProvider` |
| NFS-e município fora da base | Médio | Taxa Focus R$ 199 / verificar município piloto |
| Reforma tributária IBS/CBS | Alto (calendário) | Preferir provedor que atualize layouts |
| Certificado A1 expirado | Médio | Alerta de validade na Config futura |
| Dupla emissão | Alto | FiscalOperationId + status machine |
| Desktop sem webhook | Médio | Polling com backoff até API |
| Escolher provedor morto | Alto | Evitar Nuvem Fiscal agora |

---

## FUTURE SAAS

Focus Growth / Enterprise e PlugNotas são viáveis para multi-CNPJ.  
Isolamento: 1 empresa emissora = 1 CNPJ + 1 certificado A1 + tokens por tenant.  
Filial futura: 1 CNPJ por filial emitente (regra fiscal típica).

---

## NEXT STEP (após aceite humano)

1. Confirmar **Focus NFe** (ou PlugNotas se comercial preferir).
2. Obter **certificado A1 eCNPJ de homologação**.
3. Abrir trial Focus (30 dias).
4. Implementar **somente** `IFiscalProvider` + adapter Focus + NF-e **homologação** (sem produção).
5. Não tocar tag `v1.0.0`; nova versão comercial só após GO de release.

---

## Consistência (§27)

| Pergunta | Resposta |
|----------|----------|
| Tag v1.0.0 intacta? | SIM (`a4ad6fe`) |
| Código comercial alterado? | NÃO (só docs) |
| Banco alterado? | NÃO |
| Credenciais adicionadas? | NÃO |
| API fiscal implementada? | NÃO |
| NF emitida? | NÃO |
| Installer alterado? | NÃO |
| WIP preservado? | SIM |
| Arquitetura documentada? | SIM |
| Provedor comparado? | SIM |
| Recomendação? | SIM |
| Plano homologação? | SIM |

```text
FISCAL ARCHITECTURE DECISION READY
```

**PARE.** Não implementar emissão.
