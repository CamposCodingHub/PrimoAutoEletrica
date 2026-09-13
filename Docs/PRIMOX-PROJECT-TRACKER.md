# PRIMOX Workshop — Acompanhamento do Projeto

**Documento vivo** · **Atualizado:** 2026-09-13  
**Produto:** PRIMOX Workshop (PrimoAutoEletrica)  
**Branch de trabalho:** `migration/net10` @ `1372e11`  
**TFM:** `net10.0-windows`  
**Tag comercial protegida:** `v1.0.0` = `72d85fa` (**não mover**)  
**Índices:** [`Docs/CURRENT-TRUTH.md`](CURRENT-TRUTH.md) · [`Docs/PRIMOX-ADVANCES-CHRONICLE.md`](PRIMOX-ADVANCES-CHRONICLE.md)

---

## 1. O que é o produto

PRIMOX Workshop é um sistema desktop Windows (WPF · **.NET 10** na branch `migration/net10`) para oficinas de autoelétrica / manutenção automotiva.

**Módulos principais**
- Dashboard, Clientes, Veículos, OS, Orçamentos, Agenda
- PDV / caixa, Estoque, Catálogo, Financeiro, Relatórios, Auditoria
- Fundação fiscal (NF-e Focus path + Fake; NFC-e/NFS-e scaffold)
- Help Center, Configurações, Login / sessão, i18n PT/EN/ES

**Regra i18n:** `CurrentCulture` de formatação permanece **pt-BR** mesmo com UI EN/ES.

---

## 2. Estado atual (2026-09-13)

| Área | Status | Notas |
|------|--------|-------|
| Release tag `v1.0.0` | Protegida | Não mover |
| TFM ativo (`migration/net10`) | `net10.0-windows` | main/net6 = linha histórica |
| I18N | CLOSED (YELLOW exceções) | I18N-07 |
| Unit | **194/194** | NET10-26 |
| QaEngine / DeepQa | 43/43 · 6/6 | |
| Fiscal foundation | IMPLEMENTED + TESTED | LIVE BLOCKED_EXTERNAL |
| Calendar Dark | Mitigado | `CalendarContrastHealer` |
| WhatsApp `wa.me` | REAL | API = blocked |
| 2FA | Setup existe | **Não** no login |
| Code signing | BLOCKED_EXTERNAL | Cert ausente |
| SaaS / sync cloud | Fora do imediato | Desktop-first |

### Decisão I18N vigente

**YELLOW — CLOSED WITH EXPLICIT NON-BLOCKING EXCEPTIONS**  
Docs: `Docs/qa/PRIMOX-I18N-07-FINAL-GATE.md`

---

## 3. Arquitetura resumida

```
UI (XAML / ViewModels)
  → Services (domínio + Fiscal*)
  → Repositories / SQLite
  → FiscalApplicationService → IFiscalProvider (Focus | Fake tests)
```

Detalhe fiscal: `Docs/architecture/PRIMOX-FISCAL-ARCHITECTURE.md`

---

## 4. Avanços recentes (crônica curta)

| Fase | Avanço |
|------|--------|
| 1.0 comercial | Desktop core + installer + QA gates |
| I18N-07 | PT/EN/ES fechado |
| NET10-00…24 | Migração net10, calendar, promotion/deploy |
| NET10-25 | Audit fiscal honesto |
| NET10-26 | Fundação fiscal expandida (multiempresa, cancel/XML, DANFE info, scaffolds) |

Narrativa completa: `Docs/PRIMOX-ADVANCES-CHRONICLE.md`

---

## 5. QA — como revalidar

```powershell
dotnet build PrimoAutoEletrica/PrimoAutoEletrica.csproj -c Release
dotnet test Tests/PrimoAutoEletrica.Tests/PrimoAutoEletrica.Tests.csproj -c Release
powershell -File Scripts/Run-UiSmoke.ps1 -Configuration Release -SmokeFilter QaEngine
powershell -File Scripts/Run-UiSmoke.ps1 -Configuration Release -SmokeFilter DeepQa
```

**Não** remover testes nem alterar labels só para “passar” QA.

---

## 6. Regras de ouro

1. Não mover `v1.0.0` / não alterar `main` / `primox-net6-final` sem pedido explícito.  
2. Verdade técnica = código + testes + `CURRENT-TRUTH` / NET10-26.  
3. Relatórios de fase = avanços históricos; não copiar claims antigos para status atual.  
4. Produção fiscal e WhatsApp API = externos até evidência.  
5. I18N: não abrir I18N-08 automático.

---

## 7. Próximas frentes (decisão humana)

1. Auditoria forense NET10-26  
2. Homologação Focus live (credencial)  
3. NFC-e/NFS-e com contrato oficial  
4. Code signing comercial  
5. DVI / aprovação digital (após fiscal estável)

---

## 8. Checklist pós-pull

- [ ] `git branch` = `migration/net10` (se trabalhando NET10)  
- [ ] `git rev-parse v1.0.0` == `72d85fa`  
- [ ] Build Release OK · Unit 194  
- [ ] Ler `Docs/CURRENT-TRUTH.md` antes de classificar fiscal/Dark/2FA
