# PRIMOX 360 Technical Spec — NET10-28 → NET10-29

**Propósito:** especificação verificável Cliente/Veículo/OS 360.  
**Status implementação:** NET10-29 **PARTIAL** em HEAD `5d7c374` (`Primox360Service` + UI).  
**Spec base forense:** NET10-28 · Evidence: `PRIMOX-CUSTOMER-VEHICLE-OS-360-EVIDENCE-NET10-29-2026-09.md`

---

## 1. Princípios

1. Toda agregação financeira de cliente usa **ClienteId** (Guid/int conforme entidade), nunca nome.  
2. Toda agregação de veículo usa **VeiculoId**, com fallback placa apenas como diagnóstico.  
3. OS é o hub; Orçamento/Venda/Fiscal/Financeiro apontam por ID.  
4. Campos calculados são **views/DTOs**, não necessariamente novas tabelas na v1.

---

## 2. Modelo atual (AS-IS)

### Relacionamentos

| De | Para | Campo | Classe |
|----|------|-------|--------|
| Veiculo | Cliente | ClienteId | STRONG_ID |
| OrdemServico | Cliente | ClienteId | STRONG_ID |
| OrdemServico | Veiculo | VeiculoId (+ placa snapshot) | STRONG_ID + WEAK |
| Orcamento | Cliente | ClienteId | STRONG_ID |
| Orcamento | OS | OrdemServicoId / OrcamentoId | STRONG_ID |
| Venda | Cliente | ClienteId | STRONG_ID |
| Agendamento | Cliente | ClienteId | STRONG_ID |
| ContaReceber | Cliente | **Cliente (TEXT)** | TEXT_MATCH |
| ContaPagar | Fornecedor | Fornecedor (TEXT) | TEXT |
| FiscalOperations | OS/Venda/Orç | OrdemServicoId/VendaId/OrcamentoId | STRONG_ID schema; emissão só Venda |

### Gaps bloqueadores 360

1. **G001/G002:** `ContasReceber` sem `ClienteId` / `OrdemServicoId` tipados confiáveis para dívida.  
2. **G010:** emissão fiscal não parte da OS.  
3. Sem entidade DVI / InspectionItem.  
4. Sem ApprovalToken / DigitalApproval.

---

## 3. TO-BE mínimo NET10-29 (spec)

### 3.1 Migration proposta (NÃO executar agora)

```
ContasReceber:
  + ClienteId TEXT NULL
  + OrdemServicoId TEXT NULL
  + VendaId TEXT NULL
  INDEX IX_ContasReceber_ClienteId

-- Backfill ESTIMATIVA: match nome normalizado + ReferenciaExterna quando origem OS
```

### 3.2 Cliente360Dto (calculável)

| Campo | Fonte | Status AS-IS |
|-------|-------|--------------|
| ClienteId | Clientes.Id | EXISTE |
| Nome, contatos, LGPD | Clientes | EXISTE |
| VeiculosCount | Veiculos WHERE ClienteId | EXISTE |
| OsCount / OsTotalValor | OrdensServico | EXISTE |
| Receita12m | OS+Venda last 12m | POSSÍVEL |
| TicketMedio | Total/Count | POSSÍVEL |
| OrcamentosAprovados/Recusados | Orcamentos.Status | POSSÍVEL |
| ValorPerdido | Recusados.Total | POSSÍVEL |
| UltimaVisita / DiasSemVisita | MAX(OS.Data*) | POSSÍVEL |
| DividaAberta | ContasReceber unpaid | **IMPOSSÍVEL confiável até G001** |
| TotalGasto cache | Cliente.TotalGasto | EXISTE (UI não mostra) |

### 3.3 Veiculo360Dto

| Campo | Fonte | Status |
|-------|-------|--------|
| VeiculoId, placa, km, elétrico | Veiculos | EXISTE |
| ProprietarioClienteId | ClienteId | EXISTE |
| OsHistorico / TotalHistorico | OS filter VeiculoId | EXISTE (VisualizarVeiculo) |
| DiasDesdeUltimoServico | Today - last OS | POSSÍVEL (não UI) |
| DefeitosRecorrentes | texto OS aggregate | PARTIAL AutoElétrica |
| ProximaRevisao / Garantia | campos veículo/OS | PARTIAL |
| DviSummary | — | NOT_IMPLEMENTED |

### 3.4 Os360Dto / workflow links

| Ligação | AS-IS | TO-BE |
|---------|-------|-------|
| Orçamento→OS | REAL (ConverterEmOrdemServico) | Manter |
| OS→Financeiro | REAL (nome snapshot) | ClienteId+OS Id |
| OS→Fiscal | AUSENTE | Emitir a partir OS ou Venda derivada |
| Fotos/Assinatura/Checklist | REAL free-text/media | Evoluir → DVI |
| Pós-venda | AUSENTE auto | Jobs + templates |

---

## 4. API de serviço proposta (nomes)

```
ICliente360Service.ObterAsync(Guid clienteId) -> Cliente360Dto
IVeiculo360Service.ObterAsync(Guid veiculoId) -> Veiculo360Dto
IOrdemServico360Service.ObterAsync(Guid osId) -> Os360Dto
```

UI: elevar `HistoricoClienteWindow` / `VisualizarVeiculoWindow` / OS detail a consumir DTOs (sem inventar segunda tela paralela).

---

## 5. Critérios de pronto NET10-29

- [x] Dívida **vinculada** por Origem+ReferenciaExterna (OS/Orç) com teste  
- [ ] Dívida total por ClienteId (requer G001 migration) — **BLOCKED**  
- [x] Receita12m + ticket + dias sem visita na UI 360  
- [x] Dias desde serviço no veículo  
- [x] Zero TEXT_MATCH no caminho feliz financeiro do Cliente 360  
- [x] Spec DVI/Approval aberta (NET10-30+) sem implementação no 29  

**API entregue:** `IPrimox360Service` → `ObterCliente360` / `ObterVeiculo360` / `ObterOrdemServico360`  
**UI:** `HistoricoClienteWindow`, `VisualizarVeiculoWindow`, hub em `OrdensServicoControl`

---

## 6. Fora de escopo NET10-29

Fiscal live, TEF, WA API, Maui, multi-filial, compras, box, DVI, approval remoto — ver Roadmap / NET10-30+.

