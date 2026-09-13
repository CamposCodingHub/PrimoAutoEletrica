# PRIMOX — Fiscal Operations 2.x (reconciliado NET10-26)

**Atualizado:** 2026-09-13  
**Canônico:** [`PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md`](../qa/PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md)

---

## Operações e estado

| Operação | Software | Live homolog | Produção |
|----------|----------|--------------|----------|
| Emitir NF-e | Focus + Fake | BLOCKED_EXTERNAL | BLOCKED |
| Consultar | Focus + Fake | BLOCKED_EXTERNAL | BLOCKED |
| Cancelar | Focus DELETE + Fake | BLOCKED_EXTERNAL | BLOCKED |
| Obter XML | Focus download + Fake fixture + storage | BLOCKED_EXTERNAL | BLOCKED |
| DANFE | PDF informativo (+ provider futuro) | BLOCKED_EXTERNAL oficial | BLOCKED |
| NFC-e / NFS-e | Scaffold + Fake | BLOCKED_EXTERNAL | BLOCKED |
| Webhook inbound | Processor scaffold | Host adequado futuro | — |
| Eventos CC-e / inutilização / manif. | Não inventados | — | — |

---

## Fluxo canônico

```text
Documento → Validação → Provider → Status → Persistência → (XML/DANFE) → Eventos → Audit
```

Idempotência: mesma `IdempotencyKey` não cria outro documento lógico.  
Timeout/Unknown → consultar antes de reemitir.

---

## Avanços vs Operations 2.0 original

Cancel/XML/DANFE info/NFC-e·NFS-e Fake/multiempresa saíram de OUT OF SCOPE / NOT IMPLEMENTED para o estado da tabela acima. Live Focus continua não executado sem credencial.
