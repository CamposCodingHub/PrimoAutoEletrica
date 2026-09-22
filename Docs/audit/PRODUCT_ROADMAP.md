# PRIMOX Workshop — PRODUCT ROADMAP

---

## FASE 0 — HARDENING (Atual)

**Prioridade**: Máxima
**Status**: Em execução

- [x] API auth JWT + RBAC + SafeProblem
- [x] PermissionService fail-closed
- [x] ID-based relationships (não nome)
- [ ] License assinada (RSA/ECDSA) — requer license server
- [x] .NET 10 migration
- [x] Testes reais de segurança (PermissionService, PasswordHasher, API JWT)
- [ ] CI true quality gates (recomendação documentada)
- [x] Tests suite consolidation (.sln)
- [x] MoneyCents struct + testes
- [ ] Money column migration (REAL → INTEGER centavos) — plano documentado
- [ ] DateTime UTC padronização — plano documentado

---

## FASE 1 — WORKSHOP 2.0

**Foco**: Experiência completa da oficina

- Cliente 360 completo (cadastro → veículos → OS → orçamentos → pagamentos → garantia → timeline)
- Veículo 360 (proprietário → OS → orçamentos → fotos → diagnósticos → próxima revisão)
- Digital Inspection (check-in → fotos → vídeos → checklist → defeitos → gravidade → orçamento → aprovação → assinatura)
- Motor de garantia/retorno/lembrete (30/60/90 dias, km, horas)
- WhatsApp Cloud API (templates, webhook, fila, retry, opt-in/out, auditoria)
- Fluxos: agendamento → confirmação → recebido → diagnóstico → orçamento → aprovação → peça chegou → pronto → pós-venda

---

## FASE 2 — PRIMOX TECH

**Foco**: Diagnóstico técnico avançado

- Prontuário elétrico estruturado
- Diagnóstico guiado (roteiros interativos)
- Medições: tensão, queda de tensão, corrente, fuga, CCA, ripple, continuidade, resistência
- Componentes: bateria, alternador, motor de partida, BCM, ECU, sensores, atuadores, relés, fusíveis, chicotes
- Defeitos recorrentes (estruturados, pesquisáveis)
- Biblioteca técnica (veículo → sintoma → teste → causa → peça → solução)
- Sugestão de peças baseada em histórico

---

## FASE 3 — PRIMOX HEAVY

**Foco**: Linha pesada (12V/24V, caminhões, ônibus, máquinas, agrícola)

- Domínio especializado: 12V/24V
- Protocolos: CAN, J1939, J1708, OBD, SPN/FMI, DM1/DM2, DTC
- Gestão de frota: Empresa → Frota → Veículo → Motorista → OS → KM → Manutenção → Custo
- Indicadores: custo/km, custo por veículo, tempo parado, manutenção recorrente, peças recorrentes
- Histórico técnico estruturado por veículo/componente/falha

---

## FASE 4 — CLOUD

**Foco**: Infraestrutura cloud + multi-tenant

- PostgreSQL (migração de SQLite)
- Tenant → Branch → Users → Data isolation
- API portável (remover -windows)
- Sync offline-first (Outbox → Sync Engine → Retry → Idempotency → Conflict Resolution)
- Mobile (MAUI ou web responsivo)
- Web portal
- Backup cloud

---

## FASE 5 — BUSINESS

**Foco**: Monetização e integrações

- PIX dinâmico (QR Code, link de pagamento, gateway, webhook, conciliação, baixa automática)
- WhatsApp Cloud API production
- CRM avançado (leads, funil, conversão)
- BI operacional (DRE, fluxo de caixa, margem por OS/peça/técnico/cliente/serviço)
- Portal do cliente (visualizar veículo, diagnóstico, fotos, orçamento, aprovar, acompanhar OS, pagamentos, garantia, próxima manutenção)
- Agendamento online
- Parcelamento real

---

## FASE 6 — INTELLIGENCE

**Pré-requisito**: Massa de dados confiável

- Diagnóstico assistido (baseado em dados reais)
- Previsão de estoque (demanda, reposição, curva ABC, giro)
- Previsão de retorno (falha recorrente, manutenção preventiva)
- Sugestão de peças (histórico de uso por veículo/sintoma)
- Insights financeiros ("onde ganho dinheiro", "onde perco dinheiro")

> [!CAUTION]
> Não criar IA artificial apenas para marketing. Primeiro estruturar os dados. Depois criar inteligência baseada neles.

---

## Arquitetura Futura

```
PRIMOX Platform
├── Workshop (oficina geral)
├── Heavy (linha pesada)  
├── Fleet (frotas)
├── Tech (diagnóstico técnico)
└── Intelligence (dados + insights)

Camadas:
├── CRM, Workshop, Finance, Fiscal, Inventory, Payments, Communication, Analytics
├── API
├── Cloud Core
├── PostgreSQL
└── Desktop / Mobile / Web
```
