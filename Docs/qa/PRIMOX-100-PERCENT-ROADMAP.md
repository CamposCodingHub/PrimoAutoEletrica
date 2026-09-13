# PRIMOX Workshop — 100% Roadmap (estratégico)

**Tipo:** Roadmap estratégico + estado reconciliado  
**Atualizado:** 2026-09-13 · pós NET10-26 (`1372e11`)  
**Tag protegida:** `v1.0.0` → `72d85fa`  
**Verdade atual:** [`Docs/CURRENT-TRUTH.md`](../CURRENT-TRUTH.md) · Crônica: [`Docs/PRIMOX-ADVANCES-CHRONICLE.md`](../PRIMOX-ADVANCES-CHRONICLE.md)

**Programa:** Desktop estável → Fiscal completo testável → WhatsApp API → DVI → Multiestação → (depois) Mobile → Cloud/SaaS

**Decisão de produto:** **desktop-first**. Não abandonar a base WPF para “começar SaaS”.

---

## 1. Estado atual (2026-09-13)

| Dimensão | Estado |
|----------|--------|
| Desktop oficina core | REAL + TESTADO |
| TFM (`migration/net10`) | `net10.0-windows` |
| UI Exhaustive (histórico) | 1909/1909 na época do gate — não reexecutado neste rewrite |
| WhatsApp `wa.me` | REAL |
| WhatsApp Cloud API | Abstração + BLOCKED_EXTERNAL |
| NF-e import | REAL + TESTADO |
| NF-e emissão (software) | PARTIAL + TESTED (Focus path + Fake) |
| NF-e emissão live | BLOCKED_EXTERNAL |
| NFC-e / NFS-e | SCAFFOLD + FAKE_ONLY |
| DANFE | Informativo local; oficial = externo |
| Multiempresa fiscal DB | IMPLEMENTED + TESTED |
| Multi-filial comercial | SCAFFOLD / futuro |
| Sync remoto / SaaS / billing | NÃO IMPLEMENTADO / fora do imediato |
| 2FA no login | NÃO (serviço/setup existem) |
| Code signing | BLOCKED_EXTERNAL |

Não existe percentual oficial “produto = X%”.

---

## 2. Matriz central (atualizada)

| Área | Estado atual | Gap | Prioridade |
|------|--------------|-----|------------|
| Acessibilidade | PARTIAL→melhorado em gates | Resíduos icon-only | Média |
| Integrações | wa.me REAL; API scaffold | Provider real | Média |
| NF-e | Foundation + Focus path | Homolog live + evidência | Alta |
| NFC-e/NFS-e | Scaffold | Contrato/CSC/município | Alta (após NF-e live) |
| Multi-filial | Fiscal DB sim; produto não | Ownership comercial | Alta (depois fiscal live) |
| Sync / SaaS | Ausente | Arquitetura cloud | Baixa agora |
| Segurança | Login forte; 2FA opcional não wired | Política 2FA | Média |
| Signing | Pipeline pronto | Cert comercial | Externo |

---

## 3. Avanços já conquistados (não reidratados como “todo”)

Ver crônica completa. Destaques:

1. Produto desktop 1.0 vendável com limitações documentadas  
2. QA industrial (QaEngine/DeepQa/Exhaustive baselines)  
3. I18N PT/EN/ES fechada  
4. Migração NET10 + calendar healer + deploy desktop  
5. NET10-26: motor fiscal modular testável  

---

## 4. Pilares — próximo degrau

| Pilar | Agora | Próximo |
|-------|-------|---------|
| Fiscal | Foundation NET10-26 | Audit forense → homolog live |
| Multiempresa | Fiscal DB | UI + isolamento comercial |
| WhatsApp | wa.me + interface | Business API |
| DVI | Ideia roadmap | Após fiscal estável |
| Cloud | Arquitetura paper | Após desktop+fiscal+API |

Arquitetura: `Docs/architecture/PRIMOX-FISCAL-ARCHITECTURE.md` · Sync/Cloud docs = proposta futura.

---

## 5. Ordem de execução recomendada

```text
1. Forensic audit NET10-26
2. Homolog Focus (token real)
3. NFC-e / NFS-e com fontes oficiais
4. Signing comercial
5. Aprovação digital + DVI
6. Multiestação / multi-filial produto
7. Mobile complementar
8. Cloud / multi-loja
```

---

## 6. Registro histórico (FASE A · 2026-09-08)

A versão original deste roadmap (HEAD `12ded6e`) classificava emissão NF-e como **NÃO IMPLEMENTADO** (`NFeEmissaoService` vazio) e TFM **net6**. Isso foi **verdade na época** e permanece como marco do gap que NET10-11…26 atacaram.

Não usar o texto histórico como estado atual.
