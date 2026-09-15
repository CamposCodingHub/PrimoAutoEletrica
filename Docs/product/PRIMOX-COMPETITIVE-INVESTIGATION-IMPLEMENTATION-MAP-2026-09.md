# PRIMOX — Investigação competitiva e mapa de implementação

**Data:** 2026-09-15  
**Branch:** `audit/product-discovery-2026-09`  
**Objetivo:** Documentar o que concorrentes (BR + EUA) fazem melhor, como se comportam no fluxo completo, e o que o PRIMOX pode absorver **com calma**, sem inventar o que ainda depende de credencial externa.

---

## 1. Como ler este arquivo

| Coluna / símbolo | Significado |
|------------------|-------------|
| **PARIDADE** | Mercado já espera; PRIMOX tem parcial ou completo |
| **GAP** | Concorrente forte; PRIMOX fraco ou ausente |
| **DIFERENCIAL PRIMOX** | Onde já somos competitivos ou únicos |
| **BLOCKED_EXTERNAL** | Precisa SEFAZ / Meta / adquirente / certificado — não “só código” |
| **FASE** | Sugestão de ordem (A = agora, B = próximo trimestre, C = futuro) |

Este documento **não** é roadmap de sprint. É base para decidir juntos o que implementar.

---

## 2. Universo pesquisado (fontes públicas)

### Brasil (SaaS / oficina)
| Produto | Modelo | Preço público (aprox.) | Ênfase de marketing |
|---------|--------|------------------------|---------------------|
| **Oficina.app (Garage)** | SaaS + app | ~R$74–100/mês | OS no celular, PDF no WhatsApp, aprovação registrada |
| **Wüst** | SaaS | R$79,90/mês | OS + estoque baixa auto + NFS-e + financeiro |
| **MecPro** | SaaS | R$100–150+/mês | WhatsApp automático, PIX, NF-e, Kanban, “IA”, multiunidade |
| **MecânicaFlow** | SaaS | sob consulta / trial | Kanban, XML compras, comissão, NF, WhatsApp |
| **AutoERP** | SaaS + Android | planos Pro/Multi | OS, fotos, compras, NF-e/NFC-e/NFS-e, multi-CNPJ |

### EUA / global (shop management “best of”)
| Produto | Modelo | Ênfase |
|---------|--------|--------|
| **Tekmetric** | Cloud ~US$179–199/mês | DVI + fotos/vídeo, estimate→RO, SMS aprovação, inventory, reporting |
| **Shopmonkey** | Cloud | Texto ilimitado, payments, DVI, light+heavy, i18n |
| **Fullbay** | Cloud HD | Caminhão/diesel/frota, multi-dia |
| **Mitchell 1 Manager SE** | Desktop + add-ons | Estimativa madura + ProDemand (dados OEM) |
| **AutoLeap / NAPA TRACS** | Cloud / legado | Marketing CRM / preço entrada |

Fontes principais: páginas oficiais Tekmetric, Shopmonkey blog 2026, ShopTechScore Fullbay vs Tekmetric, MecPro, Oficina.app, Wüst, MecânicaFlow, AutoERP, Mitchell 1 Manager SE PDF.

> **Nota de honestidade:** claims de “IA”, “2.500 oficinas”, “NFS-e automática” são **marketing**. Validar em demo real antes de copiar expectativa comercial.

---

## 3. Jornada completa do cliente (como os melhores se comportam)

Fluxo “ouro” que Tekmetric / MecPro / Oficina.app vendem:

```
Agendamento → Recepção (placa/KM/fotos) → Diagnóstico/DVI
    → Orçamento com fotos → Aprovação digital (SMS/WhatsApp/link)
    → Execução (Kanban + timer) → Peças (baixa estoque / pedido)
    → Pagamento (PIX/cartão/TEF) → NF → Pós-venda (lembrete / declined jobs)
```

### O que cada etapa exige (padrão mercado)

| Etapa | UX esperada | Alertas | Dados |
|-------|-------------|---------|-------|
| **Agendamento** | Calendário por box/técnico; link online | Lembrete 24h | ClienteId, VeiculoId, slot |
| **Recepção** | Placa → histórico em 1 tela | Débito aberto, garantia | Contatos, KM, fotos entrada |
| **Diagnóstico / DVI** | Checklist + foto/vídeo por item (vermelho/amarelo/verde) | Item crítico | Achados ligados a “canned jobs” |
| **Orçamento** | Itens aprováveis um a um; total dinâmico | Margem baixa | Labor matrix, peças |
| **Aprovação** | Link no celular do cliente; assinatura | Timeout / recusa | Audit trail aprovação |
| **Execução** | Kanban; timer; fotos progresso | Atraso vs prazo | Status OS, técnico |
| **Estoque** | Baixa na OS; XML compra; ABC | Mínimo / ruptura | Kardex, fornecedor |
| **Pagamento** | PIX QR / TEF / parcelas | Falha webhook | ContasReceber |
| **Fiscal** | NF-e/NFC-e/NFS-e no fechamento | Rejeição SEFAZ | XML, DANFE |
| **Pós-venda** | Declined jobs + revisão | Revisão por KM/data | Histórico veículo |

### Como o PRIMOX se comporta hoje (honestidade)

| Etapa | PRIMOX CURRENT | Gap vs “ouro” |
|-------|----------------|---------------|
| Agendamento | Existe (calendário/agenda) | Pouco self-service online / lembrete WA auto |
| Recepção | Clientes + Veículos + 360 parcial | Débito por nome ainda frágil (G001) |
| Diagnóstico | Auto Elétrica + mídias OS | **Sem DVI** tipo Tekmetric |
| Orçamento | Completo + PDF + wa.me | Sem aprovação item-a-item no celular |
| Aprovação | Manual / crítica local | Sem portal/link assinado |
| Execução | OS + Kanban | Timer/ranking mecânico incompletos vs MecPro |
| Estoque | PDV/OS + XML NFe import | Curva ABC / sugestão compra parciais |
| Pagamento | PDV + financeiro | **Sem PIX dinâmico / TEF** |
| Fiscal | Foundation / fake / Focus path | **LIVE BLOCKED_EXTERNAL** |
| Pós-venda | Histórico / 360 | Sem “declined jobs” CRM |

---

## 4. Funções — matriz competitiva vs PRIMOX

| Função | Oficina.app | MecPro | Wüst | Tekmetric | Shopmonkey | **PRIMOX** | Fase |
|--------|:-----------:|:------:|:----:|:---------:|:----------:|:----------:|:----:|
| OS digital | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| Orçamento → OS | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| Histórico por placa | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ parcial | A |
| Cliente/Veículo/OS 360 | ~ | ✓ | ~ | ✓ | ✓ | ✓ parcial | A |
| Kanban oficina | ~ | ✓ | ~ | ~ | ✓ | ✓ | — |
| Fotos na OS | ✓ | ✓ | ~ | ✓ (DVI) | ✓ | ✓ | — |
| DVI checklist colorido | ~ | ~ | ✗ | **✓✓** | **✓✓** | **✗** | B |
| Aprovação remota | WA | WA auto | ~ | SMS/email | SMS | wa.me manual | B |
| Declined jobs CRM | ✗ | ~ | ✗ | **✓✓** | ✓ | **✗** | B |
| Estoque + baixa OS | Premium | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| Entrada XML NF | ~ | ✓ | ~ | ~ | ~ | ✓ (Importar NFe) | — |
| Catálogo fabricante PDF | ✗ | ✗ | ✗ | parts vendors | ~ | **✓ (DIF)** | A |
| PDV balcão | ~ | ✓ | ~ | ~ | ~ | ✓ | — |
| PIX dinâmico | ~ | **✓** | ~ | payments US | payments | **✗** | C |
| TEF/cartão | ~ | claim | ✗ | ✓ | ✓ | **✗** | C |
| NF-e/NFC-e/NFS-e live | planos | planos | NFS-e claim | N/A BR | N/A | PARTIAL / BLOCKED | C |
| App técnico móvel | ✓✓ | ✓ | browser | ✓ | ✓ | scaffold | C |
| Offline desktop | ✗ | ✗ | ✗ | ✗ | ✗ | **✓✓ DIF** | — |
| Auto elétrica / prontuário | ✗ | “IA” mkt | ✗ | ✗ | ✗ | **✓ DIF** | A |
| Multi-filial | Enterprise | ✓ | ✗ | ✓ | ✓ | flag off | C |
| RBAC / senha admin crítica | ~ | ✓ | ~ | ✓ | ✓ | ✓ (reforçado) | — |
| i18n PT/EN/ES | PT | PT | PT | EN | multi | parcial | B |
| QA smoke/evidence | ✗ | ✗ | ✗ | ✗ | ✗ | **✓✓ DIF interno** | — |

---

## 5. Atalhos, alertas, estilo e layout

### 5.1 Atalhos (o que o mercado treina o usuário a esperar)

| Padrão | Exemplos mercado | PRIMOX |
|--------|------------------|--------|
| Busca global | Command palette / placa | Command Center parcial |
| Novo orçamento / OS | 1 clique do cliente | Existe em 360 / atalhos |
| F-keys PDV | F6 etc. | Parcial no PDV |
| Esc fecha modal | Universal | Bom |
| Enter confirma | Universal | Bom |

**Implementar com calma (Fase A/B):** mapa de atalhos por módulo documentado na Ajuda; atalho “placa → 360”; atalho “repetir último serviço do veículo”.

### 5.2 Alertas (padrão de produto maduro)

| Tipo | Tekmetric / MecPro | PRIMOX hoje | Sugestão |
|------|--------------------|-------------|----------|
| Estoque mínimo | Badge + lista compra | Alertas existem | Unificar inbox de alertas |
| Orçamento sem resposta | Timer / follow-up | Fraco | Fila “aguardando aprovação” |
| OS atrasada | Kanban SLA | Parcial | Cor + prazo no Kanban |
| Débito cliente | Bloqueio venda | Parcial | Ligar a ClienteId (G001) |
| Revisão por KM | CRM | Parcial | Regra KM no veículo |
| Criticidade exclusão | — | Modal DS + admin password | Manter padrão |
| Qualidade import catálogo | — | Alerta prévia | Manter / melhorar fotos |

### 5.3 Estilo e layout (o que “parece premium”)

**SaaS BR (Oficina.app / MecPro / Wüst):**
- UI web clara, cards, mobile-first, muito azul/verde “confiança”
- Pouca densidade; prioriza velocidade no celular
- Dashboards com KPIs grandes (faturamento, OS abertas, ocupação)

**US (Tekmetric / Shopmonkey):**
- Densidade alta no balcão (tabelas + painéis laterais)
- DVI: grid de itens com semáforo + thumbnail
- Estimate: checkboxes por job; barra de total sempre visível
- Tipografia utilitária; pouca “decoração”

**PRIMOX:**
- WPF Design System próprio (Light/Dark, modais Premium) — **bom diferencial visual desktop**
- Risco: telas densas demais sem hierarquia mobile (não somos mobile ainda)
- Manter DS; copiar **padrões de fluxo** (não o skin SaaS genérico)

**Replicar com calma:**
1. Barra de total sticky em orçamento/OS (como Tekmetric).
2. Semáforo em checklist (quando houver DVI).
3. Inbox único de alertas no header (como “notificações” SaaS).
4. Empty states com CTA único (já parcialmente feito).

---

## 6. Banco de dados e comportamento no processo

### 6.1 Como concorrentes modelam (padrão)

Entidades centrais quase universais:

```
Cliente 1─* Veículo 1─* Visita/OS
OS 1─* Itens (serviço|peça)
OS 0─1 Orçamento / 1─* Aprovações
Peça *─* Fornecedor / NF entrada
OS → ContaReceber → Pagamentos
Usuário + Papéis (RBAC)
```

Extras dos melhores:
- **InspectionFinding** → liga foto + severidade + canned job
- **DeclinedJob** → histórico comercial
- **MessageOutbox** → WhatsApp/SMS com status
- **InventoryLedger** (kardex) imutável
- **AuditTrail** de alterações de preço/status

### 6.2 PRIMOX (SQLite AppData — offline-first)

**Forças:**
- Dado local, backup, sem mensalidade obrigatória de nuvem
- Migrations + smoke em AppData isolado
- Catálogo de peças + import PDF/CSV/XML (raro no SaaS BR barato)
- Audit de ações críticas

**Fraquezas estruturais vs SaaS:**
- Multiestação real exige SQL Server (ainda não vendável)
- Sync nuvem / app técnico = não existe
- `ContasReceber.ClienteId` **BLOCKED** → financeiro↔cliente frágil
- Sem outbox de mensagens / webhooks PIX

**Comportamento desejado (alvo de longo prazo):**

```
Toda peça na OS → ledger estoque
Todo orçamento enviado → registro de canal + timestamp
Toda aprovação/recusa → audit + declined job
Todo pagamento → ContasReceber com ClienteId + VeiculoId
Toda exclusão → senha admin + audit (já melhorado)
```

---

## 7. Potencial real do PRIMOX (sem marketing interno)

### 7.1 Onde já somos fortes de verdade
1. **Desktop offline-first** para oficina/autoelétrica brasileira que não quer depender de internet no box.
2. **Ciclo operacional local:** Cliente → Veículo → Orçamento → OS → Estoque → PDV → Financeiro → Kanban.
3. **Auto Elétrica / prontuário técnico** — mercado SaaS genérico quase não cobre bem.
4. **Importação de catálogo PDF/CSV + fotos** — diferencial de balcão/estoque.
5. **Cultura de QA** (smoke Exhaustive/Complete/Modals) — reduz regressão; confiança para evoluir com calma.
6. **Honestidade comercial** documentada (não vender fiscal live / WA Cloud / TEF como prontos).

### 7.2 Onde perdemos para SaaS barato (R$80–150/mês)
- App no bolso do mecânico
- PDF no WhatsApp com 1 toque + aprovação registrada
- PIX QR com baixa automática
- NFS-e “sem dor” (quando o SaaS realmente entrega)
- Onboarding em 30 minutos no navegador

### 7.3 Onde perdemos para Tekmetric/Shopmonkey (nível mundial)
- DVI com foto/vídeo e aprovação job-a-job
- Declined jobs + remarketing
- Labor guides / Smart Jobs / parts vendors
- SMS nativo + payments integrados
- Multi-loja cloud maduro

### 7.4 Posicionamento honesto sugerido
> **PRIMOX Workshop** = sistema **local e profundo** para autoelétrica/oficina que precisa de prontuário, catálogo, OS e caixa **sem nuvem obrigatória**.  
> Não competir de frente com “MecPro all-in-one cloud” até ter aprovação digital + PIX + fiscal live.  
> Competir com Excel/WhatsApp/papel e com ERPs genéricos sem DNA de oficina.

---

## 8. Estado atual dos erros / gaps (setembro/2026)

### 8.1 Bugs / UX recentemente tratados
| Item | Status |
|------|--------|
| Modal exclusão “digite EXCLUIR” sem campo / fora do DS | **Corrigido** (modal DS + fluxo admin password unificado em Veículos) |
| ComboBox Marca/Modelo sem texto visível | **Corrigido** (PremiumComboBox editable) |
| Catálogo GF virando códigos `GERAL ABAS…` | **Corrigido** (marca custom preservada + regex com dígito) |
| Smoke visível para acompanhar | **Disponível** (`--smoke-visible` / `-Visible`) |

### 8.2 Ainda abertos (produto / arquitetura)
| ID / tema | Severidade | Tipo |
|-----------|------------|------|
| G001 ContasReceber.ClienteId | Alta dados | BLOCKED / dívida estrutural |
| Fiscal LIVE (Focus/certificado) | Alta comercial | BLOCKED_EXTERNAL |
| Code signing Windows | Média trust | BLOCKED_EXTERNAL |
| WhatsApp Cloud API | Alta UX mercado | MISSING / EXTERNAL |
| TEF / PIX dinâmico | Alta PDV | MISSING / EXTERNAL |
| DVI + aprovação remota | Alta conversão | MISSING |
| App móvel técnico | Alta operação | SCAFFOLD |
| Multiestação SQL vendável | Média crescimento | FUTURE |
| 2FA login challenge | Média segurança | PARTIAL |
| Catálogo PDF: nomes/fotos 100% | Média qualidade | Melhorar continuamente |

### 8.3 Erros “de qualidade de dados” (não crash)
- Import PDF genérico ainda pode gerar nomes fracos → alerta de qualidade na prévia (esperado).
- Fotos só quando o PDF embute imagem decodificável pelo PdfPig (limite técnico).

---

## 9. O que falta — backlog priorizado para implementar com calma

### FASE A — Fechar o ciclo local (sem dependência externa)
1. Completar **360** (cliente/veículo/OS) com IDs estáveis (atacar G001).
2. Inbox de alertas (estoque, OS atrasada, orçamento parado).
3. Orçamento: barra de total sticky + status “aguardando aprovação”.
4. Catálogo: melhorar nomes/fotos; converter para estoque em lote revisado.
5. Atalhos documentados + Command Center por placa.
6. Declined-lite: marcar item de orçamento como “recusado” e guardar no veículo.

### FASE B — Paridade de experiência “cliente no celular” (ainda sem Meta API plena)
1. **Link de orçamento** (HTML/PDF hospedado local ou tunnel) com approve/decline por item.
2. Checklist DVI básico (semáforo + foto) na OS / tablet Windows.
3. Lembretes de revisão por KM/data (geração de lista; envio ainda wa.me).
4. Relatórios de conversão orçamento→OS (close ratio).

### FASE C — Integrações que o mercado vende como “obrigatório”
1. Fiscal LIVE real (certificado + provedor).
2. PIX cobrança + webhook.
3. TEF.
4. WhatsApp Business API.
5. App Android/iOS do técnico + sync.
6. Multi-loja.

---

## 10. Ideias concretas para “replicar” (traduzidas ao PRIMOX)

| Ideia do mercado | Como encaixa no PRIMOX | Esforço | Risco |
|------------------|------------------------|---------|-------|
| DVI Tekmetric | Nova entidade `InspecaoItem` + UI checklist na OS | Alto | Baixo (só código) |
| Approve/decline por job | Status por linha do orçamento + tela leitura | Médio | Baixo |
| Declined jobs | Tabela + card no Veículo 360 | Médio | Baixo |
| WhatsApp Oficina.app | Continuar wa.me; depois Cloud API | Baixo→Alto | Externo |
| PIX MecPro | QR + ContasReceber | Alto | Externo banco |
| NFS-e Wüst | Focus/live path | Alto | Externo |
| Smart Jobs | Templates de serviço por sintoma (Auto Elétrica) | Médio | Baixo — **encaixa no diferencial** |
| Parts matrix | Tabela margem por faixa de custo | Médio | Baixo |
| Curva ABC | Relatório estoque | Baixo | Baixo |
| Multi-CNPJ AutoERP | Só após SQL multiestação | Muito alto | Arquitetural |

---

## 11. Recomendação de produto (para decidirmos juntos)

**Não tentar virar MecPro/Tekmetric em 30 dias.**  
**Sim:**
1. Blindar o **ciclo offline** (360 + G001 + alertas + declined-lite).
2. Usar **Auto Elétrica + Catálogo** como história de venda vs SaaS genérico.
3. Desenhar DVI/aprovação como FASE B com protótipo Windows (tablet na oficina) antes de mobile.
4. Só então gastar energia em PIX/NF/WA Cloud.

---

## 12. Anexos — URLs consultadas

- https://www.tekmetric.com/shop-management  
- https://www.tekmetric.com/digital-vehicle-inspection  
- https://www.shopmonkey.io/blog/best-auto-repair-shop-management-software-2026-top-picks-compared  
- https://shoptechscore.com/fullbay-vs-tekmetric/  
- https://mitchell1.com/wp-content/uploads/2024/11/ManagerSE.pdf  
- https://oficina.app/  
- https://oficina.saas.magoweb.com.br/  
- https://wustsoftware.com.br/software-gestao-oficina-mecanica  
- https://mecanicaflow.sistemasaas.com.br/  
- https://www.autoerp.app.br/  
- Docs internos: `PRIMOX-DIFFERENTIATION-STRATEGY-2026-09.md`, `CURRENT-TRUTH.md`, `PROJECT_STATUS.md`

---

*Arquivo vivo: atualizar quando fecharmos G001, DVI ou qualquer integração externa.*
