# PRIMOX Product Discovery / Market Gap / Full Functional Audit

**Data:** 13/09/2026  
**Branch de auditoria:** `audit/product-discovery-2026-09`  
**HEAD base (commit):** `68076a67cbe253408db7dece8f79b64ede3ad81b` (NET10-27 TFM)  
**Working tree:** alterações locais **não commitadas** do usuário (2FA login, PIX estático, DANFE/XML Ops, tempos padrão, pacote contador) — **registradas, não descartadas**.  
**Escopo:** descoberta e documentação **somente**. Nenhuma feature nova implementada nesta execução.

---

## 1. Executive Summary

O PRIMOX Workshop hoje é um **software desktop WPF net10** de gestão de oficina (clientes, veículos, OS, orçamentos, agenda, estoque, PDV, financeiro, funcionários, catálogo, Kanban) com **fundação fiscal modular** (Focus + Fake) e cultura forte de QA/documentação.

Ele **não** é ainda, de forma honesta:

- ERP fiscal de prateleira com NF-e/NFC-e/NFS-e **produção**;
- CRM de relacionamento com follow-up automático;
- plataforma DVI + aprovação digital no padrão Shopmonkey/Tekmetric;
- multiestação multi-writer homologada;
- SaaS multi-loja.

**Promessa comercial cumprível HOJE (interno / piloto desktop):**  
“Oficina autoelétrica com ciclo operacional completo offline-first, Kanban, orçamento→OS, PDV, estoque, financeiro básico, wa.me manual, fiscal **preparado** (homolog/Fake), i18n e design system.”

**Por que o PRIMOX deveria existir:** especialização **auto elétrica / linha pesada / diagnóstico técnico** + desktop offline + honestidade fiscal/testável — **ainda incompleto** como diferencial de mercado, mas a base operacional e o módulo Auto Elétrica são o caminho.

---

## 2. Estado atual (baseline forense)

| Item | CURRENT |
|------|---------|
| Branch | `audit/product-discovery-2026-09` (criada a partir de `migration/net10`) |
| HEAD | `68076a6` |
| main | `29b19b1` — **não modificado** |
| v1.0.0 | `72d85fa` — **não modificado** |
| primox-net6-final | `63aeb05` — **não modificado** |
| TFM | `net10.0-windows` |
| SDK | 10.0.302 |
| Versão produto (Assembly) | 1.0.0 |
| csproj | 30 |
| Windows XAML | ~50 |
| UserControls XAML | 28 |
| ViewModels | 22 |
| Services (.cs) | 185 |
| Repositories | 13 |
| Models | 43 |
| Docs | 184 |
| Scripts | 161 |
| Test files | 43 |
| Build Release | **PASS** exit 0 (13/09/2026) |
| Unit | **197/197 PASS** (CURRENT) |
| QaEngine | **43/43 APROVADO** CURRENT (`TestResults/UiSmoke/product-discovery-20260913`) |
| DeepQa | **6/6 APROVADO** CURRENT (`TestResults/UiSmoke/product-discovery-deepqa-20260913`) |

**CURRENT vs HISTORICAL:** banners em docs antigos citam Unit 194/173 — **CURRENT = 197**. QaEngine histórico 43/43 — só aceitar CURRENT quando o smoke desta data terminar.

**Alterações locais do usuário (dirty, não no commit de auditoria):**  
ServiceExtensions, Funcionario+2FA, LoginViewModel, Fiscal Ops DANFE/XML/WhatsApp, PIX/BusinessConfiguration, ContabilExport ZIP, ServicoPadrao*, PixRecebimento*, TwoFactorVerify*, MarketHonestWinsTests, etc.

---

## 3. Produto real — o que o PRIMOX é

Arquitetura: WPF + DI + SQLite (runtime operacional) + SQL Server **preparado** + API piloto separada + Maui stub fora do core.

**Núcleo REAL (UI + lógica + persistência conectadas):**  
Clientes, Veículos, OS, Orçamentos, Agenda, Estoque/Produtos, Fornecedores, Funcionários (+2FA no working tree), PDV/caixa, Financeiro (leitura/baixa/export), Importar NF-e, Catálogo de peças, Oficina Kanban, Dashboard, Help, Configurações.

**PARTIAL:** Fiscal Ops (homolog/Focus software path), Relatórios (OS sections TODO), Auto Elétrica (UI + dados live parciais; biblioteca hardcoded), Financeiro criação de contas (UI fraca), Pagamentos (rótulos + PIX estático local), Comunicação (wa.me only).

**SCAFFOLD / FAKE / BLOCKED:** NFS-e ScaffoldNfse, PlugNotas, Cloud WhatsApp NotificationService, multi-filial (`MultiFilialDisponivel=false`), Maui, SaaS sync, signing comercial, Focus LIVE.

---

## 4. Inventário de módulos (matriz resumida)

| Módulo | Estado | Dados | Teste | Gap principal |
|--------|--------|-------|-------|---------------|
| Dashboard | IMPLEMENTED | DB | Unit/smoke histórico | KPIs profundos limitados |
| Clientes | IMPLEMENTED + TESTED | SQLite | Unit | 360 incompleto |
| Veículos | IMPLEMENTED | SQLite | Unit/smoke | DVI estruturado ausente |
| OS | IMPLEMENTED + TESTED | SQLite | Unit/smoke | Sem emit fiscal na OS |
| Orçamentos | IMPLEMENTED | SQLite | Unit | Aprovação digital remota ausente |
| Agenda | IMPLEMENTED | SQLite | — | Integração orçamento/OS pode melhorar |
| Kanban | IMPLEMENTED | OS status | smoke | — |
| PDV | IMPLEMENTED | Vendas/caixa | Unit | Fiscal homolog-only; PIX estático ≠ TEF |
| Estoque | IMPLEMENTED | Produtos | Unit | — |
| Catálogo | IMPLEMENTED | CatalogoPecas | — | Sem TecDoc |
| Fornecedores | IMPLEMENTED | SQLite | Unit | — |
| Funcionários | IMPLEMENTED | SQLite | Unit | 2FA REAL no working tree |
| Financeiro | PARTIAL | Contas* | Unit | Criação UI / BI / SPED oficial |
| Fiscal | PARTIAL + BLOCKED_EXTERNAL live | Fiscal* | Unit fiscal | Live SEFAZ |
| Importar NF-e | IMPLEMENTED | Importações | — | — |
| Auto Elétrica | PARTIAL | OS/veículo + hardcoded | — | Árvore diagnóstico persistida |
| Relatórios | PARTIAL | VM | — | OS report TODO |
| Help | IMPLEMENTED | estático | CompleteUi | — |
| Config | IMPLEMENTED | JSON/DB | smoke | Multiestação aviso only |
| License/Update windows | BACKEND_ONLY / UI órfã | files | — | Sem entry point menu |
| Mobile Maui | SCAFFOLD | — | — | Fora do desktop-first |
| API | PARTIAL piloto | — | — | ≠ SaaS |

Fontes de navegação: `MainWindow.xaml` + `NavigationService.cs`.

---

## 5. Funções ausentes / críticas vs mercado

1. Emissão fiscal **produção** NF-e/NFC-e/NFS-e  
2. DANFE **oficial** SEFAZ no ciclo OS→cliente  
3. Aprovação digital remota de orçamento (link)  
4. DVI estruturado com fotos por item + compartilhamento  
5. WhatsApp Business API (templates/status)  
6. PIX dinâmico + TEF + conciliação  
7. Multiestação multi-writer  
8. Multi-filial comercial (clientes/OS/estoque)  
9. CRM follow-up / campanhas / inativos automáticos  
10. BI gerencial (margem, conversão orçamento, produtividade)  
11. App mecânico  
12. Catálogo tempos/peças equivalente TecDoc/Fraga (OI lista Fraga)  
13. Code signing + canal update comercial  
14. SPED/Domínio oficiais  

---

## 6. Customer 360 — GAPS

**Existe (HistoricoClienteWindow / ClientesControl):** cadastro, contatos, veículos, OS, orçamentos, vendas, pagamentos/débitos (match por nome — frágil), fotos/docs de OS, observações, timeline, WhatsApp wa.me, atalhos Nova OS/Orçamento, ticket/última visita.

**Não existe / fraco:** RFM, frequência, churn, DVI, receita acumulada confiável por ID, ticket médio formal, problemas recorrentes agregados, garantias, NPS, campanhas, orçamentos perdidos analíticos, potencial de retorno, documentos fiscais por cliente, aprovação digital.

**Pergunta 14h:** o PRIMOX mostra **histórico operacional útil**, não um **CRM 360 de decisão comercial**. Classificação: **PARTIAL**.

---

## 7. Vehicle 360 — GAPS

**Existe (VisualizarVeiculoWindow):** identidade, foto/docs, bateria/alternador/partida, prontuário elétrico, defeitos, peças, OS, timeline, agenda, orçamentos, proprietário.

**Falta:** DVI, série de KM, calendário garantia/revisão, árvore de falhas persistida, custo acumulado BI, “tempo desde último serviço” como KPI, componentes com vida útil.

**Pergunta eletricista:** consegue **orientar-se**, mas não tem “não consigo trabalhar sem isso” ainda. **PARTIAL → oportunidade P1 de diferenciação**.

---

## 8. OS 360 — GAPS

**Suporta:** abertura, status, peças/mão de obra, fotos antes/depois, assinatura, checklists texto, PDF, wa.me, financeiro previsto/receita, Kanban.

**Não / fraco:** DVI itemizado, vídeo first-class, emit fiscal na OS, pagamento TEF na OS, aprovação remota item a item, laudo técnico auto elétrica exportável premium, pós-venda automático.

**Fricção:** dados espalhados OS ↔ Orçamento ↔ PDV ↔ Fiscal Ops; digitação repetida possível.

---

## 9. Workflow friction map

| Etapa | Suporte | Cliques/telas (est.) | Fricção | Diferenciação |
|-------|---------|----------------------|---------|---------------|
| Cliente chega / recepção | PARCIAL | 2–4 telas | Cadastro separado do veículo | Reception desk unificado |
| Cadastro cliente/veículo | REAL | 2 windows | Repetição | Wizard único |
| Triagem | PARCIAL | Kanban/OS | Sem DVI | DVI mobile |
| Diagnóstico | PARCIAL | Auto Elétrica + OS | Biblioteca hardcoded | Árvore sintomas |
| Orçamento | REAL | 1–2 | Sem aprovação link | Link + fotos |
| Aprovação | PARCIAL | Status local | Sem remoto | Digital auth |
| Serviço/peças | REAL | OS/Kanban | — | Tempos padrão (já) |
| Testes/DVI | PARCIAL | Checklist texto | Sem estrutura | DVI |
| Faturamento/pagamento | PARCIAL | PDV/Financeiro | PIX estático only | TEF/PIX API |
| Fiscal | PARTIAL+BLOCKED | Fiscal Ops/PDV homolog | Live blocked | Homolog→prod |
| Entrega/garantia/pós | PARCIAL | Manual | Sem automação | Alertas |

---

## 10. UX friction

- Muitos módulos no sidebar (poderoso, mas denso).  
- Relatórios e License/Update **subutilizados / órfãos**.  
- Histórico cliente poderoso mas **não é o default** do atendimento.  
- wa.me exige anexo manual (honesto, lento vs concorrentes).  
- Design system Light/Dark + i18n = **acima** da média BR.  
- Sensação: software ajuda **quem conhece o mapa**; iniciante precisa treinamento.

Estimativas de cliques (evidência de fluxo de código, não cronômetro de usuário):

| Ação | Telas | Cliques est. | Oportunidade |
|------|-------|--------------|--------------|
| Cadastrar cliente | 1–2 | 8–15 campos | Wizard + CPF busca |
| Abrir OS | 2 | 5–10 | Do cliente 1 clique (já há atalho) |
| Orçamento + serviço | 1–2 | 6–12 | Tempos padrão (já) |
| Histórico cliente | 2 | 3–5 | Abrir 360 como default |
| Emitir fiscal | 2+ | N/A live | Bloqueado |

---

## 11–13. Mercado / concorrentes / pain points

### Concorrentes (fontes públicas)

| Nome | Site | Mercado | Funções públicas | Preço público | Fonte |
|------|------|---------|------------------|---------------|-------|
| Oficina Inteligente | oficinainteligente.com.br | BR oficinas | OS, orçamento WhatsApp/QR, agenda, estoque, NF/NFC-e, Kanban, Fraga, multi-loja | R$399–599/mês (planos site) | [planos](https://oficinainteligente.com.br/planos) |
| NBS OS | nbsi.com.br | BR concessionárias/oficina | Agenda box, OS/orçamento, peças, garantia, CRM pós-venda | Sob consulta | [NBS](https://www.nbsi.com.br/), [wiki OS](https://ajuda.nbsi.com.br:84/index.php/Solu%C3%A7%C3%B5es_NBS_-_Oficina_-_NBS_OS) |
| Automanager (BR) | automanager.com.br | BR serviços/ERP | Cadastros, financeiro, OS, nuvem | Contato | [Automanager](https://automanager.com.br/) |
| AutocenterPro | autocenterpro.com.br | BR oficinas | PDV, OS, estoque, fiscal, WhatsApp (claim) | Contato | [AutocenterPro](https://autocenterpro.com.br/software-para-oficina) |
| Wüst | wustsoftware.com.br | BR oficinas | OS, estoque, financeiro, NFS-e | R$79,90/mês | [Wüst](https://wustsoftware.com.br/software-gestao-oficina-mecanica) |
| MecPro (marketing) | oficina.saas.magoweb.com.br | BR SaaS | Claims: WhatsApp auto, PIX, NF-e, IA | Planos no site | [MecPro](https://oficina.saas.magoweb.com.br/) — tratar claims como **marketing** |
| Shopmonkey | shopmonkey.io | EUA | DVI, auth digital, mobile tech | Contato | [DVI](https://www.shopmonkey.io/product/digital-vehicle-inspection) |
| Tekmetric | tekmetric.com | EUA | Auth digital SMS/email, RO | Contato | [Tekmetric auth](https://tours.tekmetric.com/answers/digital-signature-authorization-process-tekmetric) |

### Pain points (reviews internacionais — proxy)

Relatos públicos (GaragePlug/RAMP): relatórios fracos, customização limitada, glitches mobile, falta módulo financeiro, dependência de internet, preço pós-trial, setup longo.  
Fontes: [Techjockey GaragePlug](https://www.techjockey.com/reviews/garageplug-workshop-management), [SoftwareSuggest RAMP](https://www.softwaresuggest.com/ramp-auto-care/reviews).

**Implicação BR:** mercado vende **aprovação WhatsApp**, **fiscal live**, **estoque+XML**, **Kanban**, **multi-loja**, **app**. Offline-first é diferencial se fiscal/comunicação forem resolvidos sem mentir.

### Benchmark (resumo)

| Função | PRIMOX | vs mercado |
|--------|--------|------------|
| Core OS/orçamento/estoque | Forte | IGUAL/MELHOR offline |
| Fiscal live | Ausente produção | PIOR / AUSENTE |
| DVI + auth digital | Ausente | PIOR |
| WhatsApp API | wa.me only | PIOR |
| PIX/TEF | Estático local | PIOR |
| Auto elétrica especializada | Parcial único | POTENCIAL MELHOR |
| Multiestação | Não | PIOR |
| QA/honestidade docs | Forte | MELHOR (interno) |
| i18n/UI moderna | Forte | MELHOR vs muitos BR |

---

## 14. Diferenciação

### TOP 10 diferenciais HOJE
1. Desktop offline-first operacional  
2. Ciclo orçamento→OS→Kanban real  
3. Catálogo + import NF-e  
4. Módulo Auto Elétrica (mesmo parcial)  
5. Prontuário veículo elétrico (bateria/alternador/partida)  
6. Design system + i18n PT/EN/ES  
7. Fiscal modular testável (Fake ≠ live mentiroso)  
8. Cultura QA / UiSmoke / Unit  
9. Auditoria/LGPD/lockout/2FA (working tree)  
10. Especialização narrativa autoelétrica  

### TOP 10 a construir
1. DVI + fotos por item + link  
2. Aprovação digital orçamento  
3. Árvore diagnóstico elétrico persistida  
4. Laudo técnico / evidências  
5. Homolog→produção fiscal  
6. Follow-up pós-venda automático (mesmo wa.me no início)  
7. Veículo 360 “não trabalho sem”  
8. BI dono (margem/conversão)  
9. Tempos + peças inteligentes  
10. Multiestação SQL Server de verdade  

---

## 15–20. Automação, BI, CRM, pós-venda, fiscal, pagamentos

**Automações mapeadas (não implementar):** orçamento parado → follow-up; cliente inativo → alerta; OS concluída → pós-venda; garantia → lembrete; estoque baixo → compra; inadimplência → cobrança wa.me.

**BI GAP:** falta margem por OS/cliente, conversão orçamento, produtividade funcionário, ABC peças, tempo médio OS — Relatórios PARTIAL.

**CRM:** cadastro + histórico ≠ CRM (sem pipeline/oportunidade/campanha).

**Pós-venda:** depois que o carro sai, PRIMOX **para** (exceto dados históricos). Gap crítico vs OI/NBS.

**Fiscal:** Focus path + Fake TESTADO; LIVE BLOCKED_EXTERNAL; DANFE informativo; NFC-e/NFS-e scaffold; XML download Ops (working tree).

**Pagamentos:** dinheiro/cartão/PIX rótulos; PIX estático BR Code (working tree); TEF/adquirente AUSENTE.

**Comunicação:** wa.me REAL; Business API BLOCKED; email parcial; SMS não.

---

## 21–26. Multiusuário, comercial, mobile, IA

**Multiusuário:** permissões + sessions + locks REAL; SQLite single-node; SQL Server UI preparada ≠ multiestação homologada; multi-filial OFF.

**Comercial:** LicenseService + LicenseActivationWindow **órfãos de menu**; UpdateService local manifesto REAL mas CDN/signing BLOCKED; Trial types no código.

**Mobile top 10 úteis:** (1) DVI fotos (2) status OS (3) checklist (4) timer serviço (5) consulta veículo 360 (6) aprovação vista (7) estoque consulta (8) agenda do dia (9) WhatsApp templates (10) captura assinatura — **não** clonar ERP completo.

**IA (classificação):** diagnóstico assistido ALTO; resumo histórico ALTO; sugestão orçamento MÉDIO; análise fotos MÉDIO/risco; marketing-only “IA” RISCO.

---

## 27. Simplification opportunities

- Unificar entry License/Update no menu ou remover da percepção comercial até prontos.  
- Relatórios OS TODO: terminar ou esconder.  
- Evitar segundo “CRM” paralelo ao HistóricoCliente — **elevar** o histórico a 360.  
- Reduzir campos OS free-text em favor de DVI estruturado (futuro).  
- Não adicionar cloud antes de fiscal+DVI.  
- Deprecar narrativa “multi-filial” na venda até `MultiFilialDisponivel`.

---

## 28. Gap matrix master (amostra P0–P2)

| ID | Área | Função | Estado | Impacto | Prioridade | Recomendação |
|----|------|--------|--------|---------|------------|--------------|
| G01 | Fiscal | Live NF-e | BLOCKED_EXTERNAL | Crítico venda | P0 | Homolog com credencial |
| G02 | Fiscal | NFC-e/NFS-e | SCAFFOLD | Alto | P0/P1 | Após NF-e |
| G03 | OS | DVI | NOT_IMPLEMENTED | Alto | P1 | Após fiscal estável |
| G04 | Orçamento | Auth digital | NOT_IMPLEMENTED | Alto | P1 | Link + status |
| G05 | Pagamentos | TEF/PIX API | PARTIAL estático | Alto | P1 | Contrato adquirente |
| G06 | Comm | WA API | BLOCKED | Médio | P1 | Credencial Meta |
| G07 | Rede | Multiestação | FUTURE | Alto | P1 | SQL Server hardening |
| G08 | CRM | Follow-up | NOT_IMPLEMENTED | Alto | P1 | Automações wa.me |
| G09 | BI | Margem/conversão | PARTIAL | Médio | P2 | Relatórios |
| G10 | AutoElétrica | Árvore diagnóstico | PARTIAL | Diferencial | P1 | Persistência |
| G11 | UX | Cliente 360 default | PARTIAL | Médio | P2 | Quick win UI |
| G12 | Comercial | Signing/update | BLOCKED | Percepção | P0 | Cert + CDN |
| G13 | Quick | License/Update menu | BACKEND_ONLY | Baixo | P2 | Expor ou documentar |
| G14 | Financeiro | Criar contas UI | PARTIAL | Médio | P2 | Completar CRUD |
| G15 | Contábil | Pacote ZIP | IMPLEMENTED (WT) | Médio | — | Manter honestidade SPED |

---

## 29. Top Quick Wins (código já existe)

1. Expor LicenseActivationWindow / AtualizacaoWindow no menu Config (ou Help).  
2. Elevar HistoricoCliente como “Cliente 360” default no atendimento.  
3. Completar Relatorios OS TODO ou ocultar botões mortos.  
4. Wire ContabilExport OFX/CSV individuais na UI (ZIP já existe no WT).  
5. LocalSync config surface se UseLocalSync for real.  
6. Atalhos: do Dashboard → OS do dia / orçamentos parados.  
7. Alertas estoque baixo → ação compra/fornecedor.  
8. Orçamentos parados > N dias → lista + wa.me.  
9. Veículo: KPI “dias desde último serviço” (cálculo sobre OS existentes).  
10. Financeiro: criar ContaReceber/Pagar se service já grava.  
11. Fiscal: botão “abrir pasta artefatos” (já há paths).  
12. Auto Elétrica: persistir favoritos de roteiros (hoje hardcoded).  
13. PDV: lembrar chave PIX se vazia → deep link Config.  
14. Permissões: revisar papéis caixa vs mecânico (já há perfis).  
15. Empty states + keyboard (F2–F6 já existem) — treinar no Help.

---

## 30–31. Grandes oportunidades / repaginação

**Grandes (máx 20, top):** DVI; auth digital; fiscal live; diagnóstico elétrico; pós-venda; multiestação; PIX/TEF; WA API; BI dono; app mecânico DVI-only.

**Repaginação:** **PARTIAL**  
Manter arquitetura desktop. **Repaginar navegação/fluxo** (reception → 360 → OS) e **remodelar** Cliente/Veículo/OS em torno de 360+DVI — **não** reescrever tudo.

Checks:  
[x] manter arquitetura atual  
[x] refatorar parcialmente  
[x] repaginar UX/navegação  
[x] remodelar fluxo OS/Cliente/Veículo  
[ ] repaginar tudo  

---

## 32. Visão de produto (se lançasse hoje)

PRIMOX Workshop = **sistema desktop da autoelétrica** que:

1. Recebe o veículo em **1 fluxo** (cliente+placa+sintoma+DVI).  
2. Diagnostica com **conhecimento elétrico** + histórico do veículo.  
3. Orça com tempos/peças e **aprova no celular do cliente**.  
4. Executa no Kanban com evidências.  
5. Cobra e **emite fiscal** sem mentir o estado.  
6. Depois da entrega, **lembra e retém**.  
7. Dá ao dono **números** (margem, conversão, inativos).  
8. Continua funcionando **sem internet**.

Não: mais um CRUD genérico de oficina.

---

## 33. Roadmap

| Janela | Foco |
|--------|------|
| 0–30d | Quick wins UI 360; expor órfãos; fechar Relatórios mortos; preparar homolog fiscal (credencial) |
| 30–60d | Homolog NF-e live; pacote artefatos no fluxo OS; follow-up wa.me orçamentos |
| 60–90d | DVI MVP; auth digital orçamento; Auto Elétrica persistida |
| 3–6m | NFC-e/NFS-e; PIX/TEF se contrato; multiestação SQL; BI dono |
| 6–12m | WA API; app DVI; multi-filial; IA diagnóstico assistido |

Camadas: CORE (fiscal+fluxo) → DIFERENCIAÇÃO (DVI/elétrica) → COMERCIAL (signing) → UX → AUTOMAÇÃO → MOBILE → IA → CLOUD.

---

## 34. O que estamos esquecendo?

- Compras / pedido a fornecedor a partir de OS  
- Comissão de mecânicos/vendedores  
- Frota (cliente PJ multi-veículo)  
- Seguro / sinistro  
- Controle de box/pátio físico  
- Capacidade da agenda (slots)  
- Treinamento embutido / onboarding comercial  
- Telemetria de uso (opt-in) para product discovery  
- Acessibilidade teclado no balcão sob pressão  
- Backup offsite / restore drill comercial  
- Política de garantia padronizada + peça  
- Integração scanner OBD (futuro)  
- Concordância LGPD marketing WhatsApp  

---

## 35. Posição comercial

Vendável hoje como:

- [x] software de oficina (desktop piloto)  
- [x] software de auto elétrica (**parcial**, narrativa)  
- [ ] ERP básico completo fiscal  
- [ ] software premium de oficina (falta DVI/auth/fiscal live)  
- [ ] plataforma de diagnóstico  
- [ ] plataforma cloud  

**Promessa cumprível:** operação de oficina autoelétrica offline com OS/orçamento/estoque/PDV/financeiro e fiscal **em preparação**.

---

## 36. Maturidade

| Dimensão | Nota |
|----------|------|
| OPERACIONAL | GREEN |
| UX | YELLOW |
| CRM | RED |
| BI | RED/YELLOW |
| FISCAL | YELLOW software / RED live |
| PAYMENTS | YELLOW |
| COMMUNICATION | YELLOW |
| SECURITY | YELLOW→GREEN (2FA WT) |
| COMMERCIAL | YELLOW |
| DIFFERENTIATION | YELLOW |
| MOBILE | RED |
| CLOUD | RED (consciente) |

---

## 37. Top 10 recomendações executivas

1. **Homolog fiscal live** — desbloqueia venda “emite nota” (P0, externo).  
2. **Signing + update canal** — percepção comercial (P0).  
3. **DVI + auth digital** — diferencial vs ERP genérico (P1).  
4. **Cliente/Veículo 360 como default** — quick win alto ROI (P1).  
5. **Pós-venda automático wa.me** — retenção (P1).  
6. **Auto Elétrica persistida** — “não trabalho sem” (P1).  
7. **Não investir em Maui completo agora** — só DVI mobile depois (P3).  
8. **Não vender multi-filial/SaaS** até evidência (risco).  
9. **BI dono mínimo** — margem + conversão (P2).  
10. **Simplificar promessas** — honestidade = marca (P0 cultural).

---

## 38. Limitations desta auditoria

- Sem cronometragem humana de cliques em oficina real.  
- QaEngine/DeepQa CURRENT: ver Evidence (pode estar em andamento).  
- Concorrentes avaliados por **sites públicos**, não por trial pago.  
- Working tree ≠ HEAD — features 2FA/PIX/DANFE UI/tempos/ZIP existem no disco mas **fora** do commit 68076a6.  
- Nenhuma credencial fiscal/WhatsApp/adquirente usada.

---

## 39. Sources (código + mercado)

Código: MainWindow, NavigationService, HistoricoClienteWindow, VisualizarVeiculoWindow, OrdemServicoWindow, Fiscal*, FilialService, NotificationService, LicenseService, UpdateService, ContabilExportService, CURRENT-TRUTH.md, explore audit 13/09/2026.

Mercado: links nas seções 11–13.

Evidence detalhada: `Docs/product/PRIMOX-PRODUCT-DISCOVERY-EVIDENCE-2026-09.md`.
