# PRIMOX — Fiscal Homologation Plan 1.0

**Status:** PLANO — **não executar emissão** nesta auditoria  
**Provedor alvo:** Focus NFe (alternativa: PlugNotas)  
**Ambientes:** DEV → HOMOLOGAÇÃO → PRODUÇÃO (nunca misturar)

---

## 1. Pré-requisitos (gates)

| # | Gate | Responsável |
|---|------|-------------|
| G1 | Dono aceita Focus (ou PlugNotas) | Produto |
| G2 | Conta trial Focus criada (sem produção) | Produto/Dev |
| G3 | Certificado **A1 eCNPJ** de **homologação** | Contabilidade/AR |
| G4 | Dados empresa piloto (CNPJ, IE, CRT, endereço) | Produto |
| G5 | Município ISS conhecido (se NFS-e) | Produto |
| G6 | `IFiscalProvider` + adapter em branch de feature | Dev |
| G7 | Banco isolado QA (`QA_FISCAL_`) | Dev/QA |
| G8 | Checklist homolog PASS antes de qualquer produção | QA + Produto |

**Sem G1–G4: NO-GO técnico.**

---

## 2. Ambientes Focus (referência oficial)

| Ambiente | Base URL | Efeito fiscal |
|----------|----------|---------------|
| Homologação | `https://homologacao.focusnfe.com.br` | Sem validade fiscal |
| Produção | `https://api.focusnfe.com.br` | Validade real |

Rotas `/v2/...`. Fonte: doc.focusnfe.com.br (2026-09-08).

Tokens Homolog ≠ Produção. UI futura deve exibir banner **HOMOLOGAÇÃO**.

---

## 3. Escopo da primeira onda (após GO de implementação)

**Somente:**

1. NF-e saída em **homologação**  
2. Consulta status até Autorizada/Rejeitada  
3. Persistência XML + protocolo + chave  
4. Obtenção DANFE/PDF se API fornecer  
5. Idempotência `FiscalOperationId`  
6. Testes `QA_FISCAL_`

**Fora da primeira onda:** produção, NFC-e, NFS-e, cancelamento completo, contingência completa.

---

## 4. Casos de teste (homologação)

### Autorização / rejeição

| ID | Caso | Esperado |
|----|------|----------|
| H-01 | Nota válida mínima | Processando → Autorizada |
| H-02 | CPF inválido | Rejeitada + motivo |
| H-03 | CNPJ inválido | Rejeitada |
| H-04 | IE inválida | Rejeitada |
| H-05 | NCM inválido | Rejeitada |
| H-06 | CFOP inválido | Rejeitada |
| H-07 | Produto sem descrição/valor | Rejeitada / validação local |
| H-08 | Totais inconsistentes | Rejeitada / validação local |

### Certificado / API

| ID | Caso | Esperado |
|----|------|----------|
| H-10 | Certificado inválido | Erro claro, sem “autorizada” |
| H-11 | Certificado expirado | Erro claro |
| H-12 | Token errado | 401/403 tratado |
| H-13 | API indisponível / timeout | Status Erro/Processando + retry idempotente |
| H-14 | Duplo clique emitir | Uma única nota (mesmo FiscalOperationId) |
| H-15 | App fecha no meio | Retoma consulta, não duplica |

### Pós-autorização (fase 2 homolog)

| ID | Caso | Esperado |
|----|------|----------|
| H-20 | Consultar autorizada | Chave + protocolo |
| H-21 | Baixar XML | Arquivo íntegro |
| H-22 | DANFE/PDF | Arquivo gerado |
| H-23 | Cancelamento (se habilitado) | Cancelada |
| H-24 | Webhook duplicado (se API) | Idempotente |

### NFC-e / NFS-e (ondas posteriores)

Casos análogos + CSC (NFC-e) + município/Nacional (NFS-e).

---

## 5. Dados de teste

Usar apenas:

```text
QA_FISCAL_CLIENTE
QA_FISCAL_PRODUTO
QA_FISCAL_OS
```

Nunca CPF/CNPJ reais de terceiros.  
Homologação SEFAZ/provedor: seguir regras do ambiente de teste do provedor.

---

## 6. Critério PASS homologação NF-e

- [ ] Emissão homolog autorizada com chave/protocolo reais do ambiente  
- [ ] Rejeição tratada com mensagem legível  
- [ ] Sem falso “autorizada”  
- [ ] Idempotência verificada  
- [ ] XML persistido fora de produção AppData do cliente  
- [ ] Secrets não aparecem em logs  
- [ ] Relatório `PRIMOX-NFE-HOMOLOG-REPORT.md` anexado  
- [ ] Tag `v1.0.0` não movida  

Produção = **etapa separada** com checklist próprio e GO comercial.

---

## 7. Estratégia desktop-only

Enquanto não houver API pública:

1. Enviar emissão  
2. Guardar `providerDocumentId` + `FiscalOperationId`  
3. Polling com backoff (ex.: 2s, 5s, 10s, 30s…)  
4. Timeout configurável → status Erro + botão “Consultar novamente”  

Webhook fica no backlog da API.

---

## 8. Rollback

Se homolog falhar:

- Desligar feature flag fiscal  
- Manter import NF-e intacto  
- Não habilitar botões de emissão em builds comerciais 1.0.0  

---

## 9. O que esta etapa NÃO faz

- Não cria conta Focus automaticamente  
- Não sobe certificado  
- Não chama API  
- Não emite nota  
- Não altera schema produção  
