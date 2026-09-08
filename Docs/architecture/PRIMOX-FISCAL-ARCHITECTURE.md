# PRIMOX — Arquitetura Fiscal (FASE A)

**Status do documento:** ARCHITECTURE PROPOSAL — **não implementado**  
**Emissão atual:** NÃO IMPLEMENTADO (`Services/Fiscal/NFeEmissaoService.cs` vazio)  
**Importação atual:** REAL (`NFeService`)  
**Tag v1.0.0:** intacta — emissão fiscal **não** faz parte do significado comercial do 1.0.0

---

## 1. Escopo fiscal típico da oficina

| Operação | Documento | Estado atual |
|----------|-----------|--------------|
| Compra de peças (entrada) | NF-e fornecedor (XML) | REAL — import |
| Venda de peças B2B | NF-e saída | NÃO IMPLEMENTADO |
| Venda balcão consumidor | NFC-e (futuro) | NÃO IMPLEMENTADO |
| Serviço (mão de obra) | NFS-e municipal (futuro) | NÃO IMPLEMENTADO / FORA até decisão |

**Primeiro entregável autorizado (após GO):** NF-e **homologação** apenas.

---

## 2. Estados obrigatórios do documento

```text
RASCUNHO → VALIDANDO → ENVIANDO → AUTORIZADA | REJEITADA | ERRO
                              ↘ CANCELADA (após autorizada)
```

XML de rascunho **nunca** = NF-e autorizada.

---

## 3. Decisão arquitetural — Opção A vs B

| Critério | A — SEFAZ direta | B — Provedor fiscal |
|----------|------------------|---------------------|
| Custo inicial | Médio (dev alto) | Médio/alto (licença + por nota) |
| Complexidade | Muito alta (schemas, eventos, contingência UF) | Alta (API provedor) mas menor que schemas crús |
| Manutenção | Crítica a cada NT SEFAZ | Provedor absorve parte |
| Certificado | A1/A3 no cliente ou HSM | Frequentemente A1 no provedor/cliente |
| Homologação | SEFAZ-UF | Provedor + SEFAZ |
| Contingência | Implementar SVC/FS-DA etc. | Depende do provedor |
| Risco compliance | Alto se mal feito | Compartilhado; vendor lock-in |
| Dependência externa | SEFAZ | Provedor + SEFAZ |
| Adequação PRIMOX 1.x | Só com time fiscal dedicado | **Recomendada** para time produto |

### Recomendação técnica (FASE A)

**Opção B (provedor especializado) como caminho preferencial**, com adapter interno:

```text
PRIMOX Domain (Venda/OS)
        │
        ▼
IFiscalEmissionPort
        │
        ▼
FiscalProviderAdapter (sandbox/homolog)
        │
        ▼
Provedor → SEFAZ
```

Opção A só se houver decisão explícita de internalizar manutenção de schemas/NTs.

**Bloqueio:** escolha A/B + regime tributário (Simples/Presumido/Real) + UF emitente + certificado A1 de testes.

---

## 4. Fluxo mínimo homologação

```text
Operação (PDV/OS)
  → DocumentoFiscal (RASCUNHO)
  → Validação domínio (CFOP/NCM/CST|CSOSN/totais)
  → XML
  → Assinatura (certificado — nunca logar senha/PFX)
  → Envio
  → Resposta real
  → Protocolo + XML autorizado persistidos
  → DANFE (quando aplicável)
```

Logs: número, status, cStat, xMotivo — **sem** secrets.

---

## 5. Persistência (proposta — ainda não migrar)

Entidades conceituais:

- `DocumentoFiscal` (Id, Tipo, Ambiente Homolog|Prod, Status, Chave, Protocolo, XmlPath, FilialId?, TenantId?)
- `DocumentoFiscalEvento` (cancelamento, CCe, inutilização — fases posteriores)
- `EmitenteConfig` (CNPJ, IE, CSC NFC-e futuro, paths cert **protegidos**)

**Não** criar migrations nesta FASE A.

---

## 6. Segurança

- Certificado via DPAPI / store / path configurável — nunca source control  
- Ambientes Homolog vs Produção **flags explícitas** na UI e no DB  
- Produção: bloqueio até checklist homologação PASS  

---

## 7. Critérios “NF-e REAL” (homologação)

certificado + assinatura + XML válido + envio real + resposta real + protocolo + persistência + consulta + erro tratado + testes QA_NFE_ + relatório `PRIMOX-NFE-REPORT.md`.

Produção = etapa separada após evidência.

---

## 8. Itens NÃO nesta fase

NFC-e, NFS-e, cancelamento produção, inutilização, contingência completa, “simular autorização”.

---

## 9. Atualização — TOTAL CODEBASE + INTEGRATION AUDIT 1.0 (2026-09-08)

| Capacidade | Estado comprovado |
|------------|-------------------|
| NF-e importação | REAL + TESTADO (`NFeService`) |
| NF-e emissão / SEFAZ / DANFE / cancel / inutilização | NÃO IMPLEMENTADO |
| `Services/Fiscal/NFeEmissaoService.cs` | **0 bytes** — PLACEHOLDER de arquivo |
| NFC-e / NFS-e | NÃO IMPLEMENTADO |
| Certificado X509/PFX/A1/A3 | NÃO IMPLEMENTADO (package Cryptography.Xml sem uso SignedXml) |
| Recomendação A vs B | **Mantida: Opção B (provedor)** preferencial |
| Abstração alvo | `IFiscalProvider` / `IFiscalEmissionPort` — **não implementar fake** |

Portabilidade conceitual do Audit 1.0:

```text
EmitirNotaAsync / ConsultarNotaAsync / CancelarNotaAsync
InutilizarNumeroAsync / ConsultarStatusAsync
```

Somente após GO de produto + escolha A/B + certificado de homologação.
