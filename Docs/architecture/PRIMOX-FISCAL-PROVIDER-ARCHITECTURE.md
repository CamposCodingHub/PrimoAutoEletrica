# PRIMOX — Fiscal Provider Architecture (Decision 1.0)

**Status:** Decision architecture + **Fiscal Foundation 1.0 implemented** (see `PRIMOX-FISCAL-FOUNDATION-1.0.md`). Emission / live Focus HTTP = **not** implemented.  
**Tag v1.0.0:** intacta  
**Provedor alvo preferencial:** Focus NFe (adapter); alternativa PlugNotas  
**Atualização:** 2026-09-08 — Fiscal Foundation 1.0

---

## 1. Princípio

```text
IMPORTAR NF-e (já REAL)  ≠  EMITIR NF-e/NFC-e/NFS-e (futuro)
```

Emissão sempre via **provedor** (Opção B), atrás de abstração interna para trocar fornecedor.

---

## 2. Fluxo alvo

```text
PRIMOX (PDV / OS / Orçamento / Financeiro)
        │
        ▼
Preparação fiscal (CFOP, NCM, CST/CSOSN, totais, destinatário)
        │
        ▼
Validação de domínio (local)
        │
        ▼
IFiscalProvider.Emitir*(request, FiscalOperationId)
        │
        ▼
FocusNfeProvider (ou PlugNotasProvider)
        │
        ▼
Homologação OU Produção (flags explícitas)
        │
        ▼
SEFAZ / Prefeitura / NFS-e Nacional
        │
        ▼
Processamento assíncrono
        │
        ├── Webhook (quando houver endpoint público)
        └── Polling periódico (desktop-only)
        │
        ▼
Persistência PRIMOX: status + chave + protocolo + XML + PDF
        │
        ▼
DANFE / DANFSE + vínculo OS/Venda/Financeiro
```

---

## 3. Abstração (NÃO implementar agora)

```csharp
public interface IFiscalProvider
{
    Task<FiscalEmitResult> EmitirNFeAsync(FiscalNFeRequest request, Guid fiscalOperationId, CancellationToken ct);
    Task<FiscalEmitResult> EmitirNfceAsync(FiscalNfceRequest request, Guid fiscalOperationId, CancellationToken ct);
    Task<FiscalEmitResult> EmitirNfseAsync(FiscalNfseRequest request, Guid fiscalOperationId, CancellationToken ct);
    Task<FiscalStatusResult> ConsultarAsync(string providerDocumentId, CancellationToken ct);
    Task<FiscalEventResult> CancelarAsync(string providerDocumentId, string justificativa, CancellationToken ct);
    Task<FiscalEventResult> InutilizarAsync(FiscalInutilizacaoRequest request, CancellationToken ct);
    Task<FiscalEventResult> CartaCorrecaoAsync(string providerDocumentId, string correcao, CancellationToken ct);
    Task<Stream> ObterXmlAsync(string providerDocumentId, CancellationToken ct);
    Task<Stream> ObterDanfeAsync(string providerDocumentId, CancellationToken ct);
}
```

Implementações futuras: `FocusNfeProvider`, `PlugNotasProvider`.  
**Proibido:** stub que retorna “autorizada” sem resposta real.

---

## 4. Estados do documento

```text
Rascunho
Validando
Enviando
Processando
Autorizada
Rejeitada
Cancelada
Denegada
Contingencia
Erro
```

Regras:

- XML de rascunho **nunca** = autorizada.  
- HTTP 200 / “aceito na fila” **nunca** = autorizada.  
- Só `Autorizada` libera impressão fiscal definitiva e baixa fiscal.

---

## 5. Idempotência

Campo obrigatório futuro: **`FiscalOperationId`** (Guid).

Gerado **uma vez** por intenção de negócio (ex.: “faturar OS-123”).

| Evento | Comportamento |
|--------|----------------|
| Duplo clique | Reusa mesmo `FiscalOperationId` |
| Timeout | Consulta por `FiscalOperationId` / id provedor antes de reenviar |
| App fecha | Ao reabrir, recupera operação pendente |
| Webhook duplicado | Upsert por id evento / chave acesso |

Nunca criar segunda emissão “porque não veio resposta”.

---

## 6. Persistência proposta (sem migration agora)

| Entidade | Papel |
|----------|-------|
| `FiscalCompany` | CNPJ, IE, CRT, CSC (NFC-e), município ISS |
| `FiscalCertificate` | Metadados A1 (path/ref DPAPI), validade — **sem senha em claro** |
| `FiscalProviderAccount` | Token ambiente Homolog/Prod (protegido) |
| `FiscalDocument` | Tipo, status, chave, protocolo, ambiente, FilialId?, TenantId? |
| `FiscalDocumentItem` | Itens vinculados a venda/OS/estoque |
| `FiscalOperation` | FiscalOperationId, tentativas, last error |
| `FiscalEvent` | Cancel, CC-e, inutilização |
| `FiscalWebhookInbox` | Eventos recebidos (futuro API) |

---

## 7. Webhook vs polling

### Desktop 1.x (hoje)

```text
PRIMOX → Provider → consulta periódica (backoff) até status terminal
```

### Com API futura

```text
Provider → Webhook assinado → API PRIMOX → DB → Desktop sync
```

Desktop deve continuar funcionando **sem** depender de webhook.

---

## 8. Certificados

| Modelo | Adequação PRIMOX |
|--------|------------------|
| **A1 (.pfx)** | **Recomendado** — Focus exige A1; SaaS/cloud viável |
| A3 (token/cartão) | Evitar para emissão via provedor cloud |

Armazenamento futuro: DPAPI / Credential Manager; renovação alertada; nunca logar senha/PFX.

---

## 9. Segurança de secrets

Proibido: Git, XAML, appsettings públicos, logs.  
Permitido: DPAPI local; futuro secrets no backend SaaS; rotação de API key; auditoria de acesso.

---

## 10. Impacto nos módulos atuais

| Módulo | Preparação |
|--------|------------|
| Importar NF-e | Já REAL — manter isolado da emissão |
| PDV | Precisa NFC-e futura + status fiscal |
| OS / Orçamentos | Precisa gerar payload NF-e/NFS-e |
| Estoque | CFOP/NCM/itens — precisa campos fiscais |
| Financeiro | Vincular documento autorizado |
| Clientes | CPF/CNPJ/IE/endereço fiscais |
| Fornecedores | Já usado na importação |
| Configurações | Empresa emissora + cert + ambiente |
| Relatórios | Futuro: notas emitidas |
| Backup | Incluir XMLs fiscais |
| Auditoria | Logar operações sem secrets |
| API | Futuro webhook receiver |

---

## 11. Multi-filial / SaaS (futuro)

```text
Empresa (Tenant)
 └── Filial emitente (CNPJ)
      ├── Certificado A1
      ├── Série/numeração
      └── Documentos fiscais
```

Provedor deve permitir N CNPJs (Focus Growth / PlugNotas).  
**Não implementar** multi-filial nesta etapa.

---

## 12. Ordem de implementação futura (após GO)

1. Modelo de dados + status machine + FiscalOperationId  
2. Config empresa + A1 homolog (UI mínima)  
3. `IFiscalProvider` + `FocusNfeProvider`  
4. NF-e saída **somente homologação**  
5. Consulta/polling + XML/DANFE  
6. NFC-e (PDV)  
7. NFS-e (município piloto / Nacional)  
8. Cancelamento / inutilização  
9. Produção (checklist separado)

---

## 13. Relação com v1.0.0

Emissão fiscal **não** redefine a tag `v1.0.0`.  
Qualquer release com emissão = nova versão comercial após homologação PASS.
