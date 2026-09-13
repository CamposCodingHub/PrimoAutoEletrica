# PRIMOX Workflow Master — NET10-28

**Tipo:** ESTIMATIVA DE CÓDIGO (sem teste humano cronometrado).  
**HEAD:** `a016fed` · 13/09/2026

---

## 1. Fluxo operacional AS-IS (conceitual)

```
Cliente chega
 → ClientesControl / NovoCliente (cadastro separado)
 → VeiculosControl (cadastro separado; ClienteId)
 → Agendamentos OU OS / Orçamento / Recepção informal
 → Diagnóstico (OS texto + Auto Elétrica opcional)
 → Orçamento (serviços/peças)
 → Aprovação LOCAL (status)
 → OS execução / Kanban
 → Peças estoque
 → Pagamento (PDV / Financeiro / PIX label)
 → Fiscal (PDV→NFe path; OS path AUSENTE)
 → Entrega (status OS)
 → Garantia (campos PARTIAL)
 → Pós-venda AUTO AUSENTE
```

---

## 2. Matriz por etapa

| # | Etapa | Entrada | Ação | Tela | Dados | Saída | Próxima | Fricção |
|---|-------|---------|------|------|-------|-------|---------|---------|
| 1 | Chegada | — | Abrir app | Main/sidebar | — | Sessão | Cadastro | Menu denso |
| 2 | Cadastro cliente | Dados PF/PJ | CRUD | ClientesControl | Clientes | ClienteId | Veículo | Separado |
| 3 | Veículo | ClienteId | CRUD | VeiculosControl | Veiculos | VeiculoId | Sintoma | Separado |
| 4 | Reclamação | Texto | Digitar | OS / Agendamento | Observação | Texto | Triagem | Sem wizard |
| 5 | Triagem | Agenda | Slot | Agendamentos | Agendamento | Status | Diag | Sem box |
| 6 | Diagnóstico | Sintoma | Roteiro | AutoElétrica / OS | Hardcoded+OS | Notas | Orç | Não aprende |
| 7 | Orçamento | Peças/serviços | Montar | OrcamentosControl | Orcamento | Total | Aprov | Muitos passos |
| 8 | Aprovação | Status | Local | Orçamento/OS | Status | Aprovado | Exec | Sem link cliente |
| 9 | Execução | OS | Kanban | OficinaKanban | Status OS | Em serviço | Peças | OK |
| 10 | Peças | Itens | Baixa | Estoque/OS | Produto | Estoque− | Pag | Sem compra auto |
| 11 | Serviço | Tempo | Registrar | OS | Serviços | Custo | Pag | Tempos manuais |
| 12 | Pagamento | Valor | PDV/CR | PDV/Financeiro | Venda/CR | Quitado | Fiscal | PIX/TEF rótulo |
| 13 | Fiscal | Venda | Emitir | Fiscal Ops | Focus/Fake | XML | Entrega | OS path gap |
| 14 | Entrega | OS | Concluir | OS | Status | CONCLUÍDA | Garantia | OK |
| 15 | Garantia | Campos | Manual | OS/Veículo | Datas | Registro | Pós | Frágil |
| 16 | Pós-venda | — | — | — | — | — | — | **AUSENTE auto** |

**Reception desk:** NÃO existe Cliente+Veículo+Sintoma em uma operação. Cadastro separado vs reception wizard = **AUSENTE**. ESTIMATIVA: 3+ telas / 5–8 passos.

---

## 3. Ligação Orçamento → OS

| Aspecto | Classificação |
|---------|---------------|
| Conversão | **REAL** (`ConverterEmOrdemServico` / fluxos OS) |
| Automática total | **PARCIAL** — acionada pelo usuário |
| Lost quotes | **PARTIAL** — contagem; analytics fracos |
| Validade/comunicação | PARTIAL / wa.me manual |

---

## 4. Pós-venda (“o carro saiu — o PRIMOX ainda trabalha?”)

| Capacidade | Classificação |
|------------|---------------|
| Garantia campos | PARTIAL |
| Revisão/retorno lembrete | AUSENTE auto |
| Notificação pós-OS | AUSENTE (`NotificationService` stub) |
| Histórico consultável | REAL (telas histórico) |
| Comunicação auto | AUSENTE |

**Resposta:** Depois que o carro sai, o PRIMOX **não trabalha automaticamente**. Só sob consulta manual.

---

## 5. Day in the life (fricção — conceitual)

| Hora | Cena típica | Fricção PRIMOX |
|------|-------------|----------------|
| 08:00 | Abrir oficina | Onboarding zero-data: catálogo/serviços vazios |
| 09:00 | Recepção | Troca Cliente↔Veículo↔OS |
| 10:00 | Diagnóstico | Auto Elétrica vs OS desconectados |
| 12:00 | Orçamento | Aprovação só presencial |
| 14:00 | Peças | Sem sugestão de compra |
| 16:00 | Pagamento | PIX/TEF não integrados |
| 18:00 | Fechar | Sem follow-up automático |

---

## 6. UX forensic (oportunidades)

| Ação | Onde |
|------|------|
| UNIFICAR | Reception wizard |
| DESTACAR | Histórico 360 (default pós-cadastro) |
| AUTOMATIZAR | Pós-OS wa.me |
| MOVER | License/Update para Config |
| REMOVER/ESCONDER | Relatório OS stub; labels TEF |
| SIMPLIFICAR | Sidebar densidade |

---

## 7. Repagination proposta (não implementar)

**CURRENT → PROPOSTA**

| CURRENT | PROPOSTA |
|---------|----------|
| Sidebar módulo-lista longa | **RECEPÇÃO** (wizard) + módulos |
| Clientes / Veículos separados | 360 tabs sob entidade |
| OS + Kanban + Orçamentos | Workflow unificado status |
| Auto Elétrica isolada | Aba da OS/Veículo |
| Fiscal Ops | Pós-pagamento contextual |
| Relatórios modernos | BI Owner (KPIs verdadeiros) |

**Navegação sugerida:** Recepção · Clientes · Veículos · OS · Orçamentos · Kanban · Estoque · Financeiro · Relacionamento · Relatórios · Auto Elétrica · Fiscal · Configurações

**REPAGINATION:** YES — densidade e fluxo recepção/360 justificam remodelar páginas chave sem trocar stack.
