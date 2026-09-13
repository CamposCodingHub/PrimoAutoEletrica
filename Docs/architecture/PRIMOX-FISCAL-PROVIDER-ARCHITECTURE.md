# PRIMOX — Fiscal Provider Architecture

**Atualizado:** 2026-09-13 · NET10-26  
**Decisão:** Opção B (provedor) · Focus principal · PlugNotas alternativa scaffold  
**Canônico:** [`PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md`](../qa/PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md)

---

## Estado atual

```text
IFiscalProvider
 ├── FocusNfeProvider     → HTTP homolog (emit/consult/cancel/XML)
 ├── PlugNotasProvider    → scaffold NotImplemented controlado
 └── FakeFiscalProvider   → TEST ONLY (fora DI comercial)
```

| Capacidade Focus | Código | Live |
|------------------|--------|------|
| POST /v2/nfe | SIM | BLOCKED_EXTERNAL |
| GET /v2/nfe/{ref} | SIM | BLOCKED_EXTERNAL |
| DELETE cancel | SIM | BLOCKED_EXTERNAL |
| XML download | SIM | BLOCKED_EXTERNAL |
| DANFE provider | Delegado / informativo local | BLOCKED_EXTERNAL oficial |

Produção: URL/guard bloqueiam.

---

## Avanços desde a Decision Architecture 1.0 (08/09)

Na decisão original, emission/live HTTP = not implemented.  
NET10-26 entregou o adapter HTTP Focus + testes controlados. Live continua externo.

---

## Regras

- Nunca inventar endpoint PlugNotas  
- Nunca logar token  
- Fake nunca no DI comercial  
- Homolog host only
