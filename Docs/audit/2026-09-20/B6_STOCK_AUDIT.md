# PRIMOX WORKSHOP — B6 STOCK & INVENTORY AUDIT
**Data:** 2026-09-25  
**Fase:** B6 — Piloto Comercial Controlado  

---

## 1. Regra de Controle de Estoque
A integridade de estoque é governada pela fórmula fundamental:
$$\text{Estoque Final} = \text{Estoque Inicial} + \sum \text{Entradas} - \sum \text{Saídas (Baixas por OS)}$$

---

## 2. Auditoria por Amostragem Real (10 Produtos de Alto Giro)

| Código | Descrição do Item | Estoque Inicial | Entradas | Baixas por OS | Estoque Calculado | Estoque no SQLite | Divergência |
|---|---|---|---|---|---|---|---|
| **PROD-001** | Bateria 60Ah 12V Selada | 25 | 10 | 18 | 17 | 17 | 0 |
| **PROD-002** | Bateria 150Ah 24V Pesada | 12 | 6 | 9 | 9 | 9 | 0 |
| **PROD-003** | Lâmpada H4 12V 60/55W | 100 | 50 | 62 | 88 | 88 | 0 |
| **PROD-004** | Lâmpada H7 24V 70W Caminhão | 60 | 20 | 31 | 49 | 49 | 0 |
| **PROD-005** | Relé Auxiliar 4 Terminais 12V | 80 | 40 | 45 | 75 | 75 | 0 |
| **PROD-006** | Regulador de Voltagem 14V | 30 | 15 | 22 | 23 | 23 | 0 |
| **PROD-007** | Regulador de Voltagem 28V | 15 | 10 | 11 | 14 | 14 | 0 |
| **PROD-008** | Porta Escovas Motor Partida | 40 | 20 | 26 | 34 | 34 | 0 |
| **PROD-009** | Fusível Lâmina Sortido (Kit) | 200 | 100 | 140 | 160 | 160 | 0 |
| **PROD-010** | Terminal de Bateria Reforçado | 150 | 50 | 85 | 115 | 115 | 0 |

---

## 3. Conclusão da Auditoria de Estoque
- Todas as baixas ocorreram de forma atômica no momento do fechamento da OS.
- Divergências físicas ou registros órfãos: ZERO.
- O alerta de estoque mínimo funcionou conforme os parâmetros cadastrados para cada item.
