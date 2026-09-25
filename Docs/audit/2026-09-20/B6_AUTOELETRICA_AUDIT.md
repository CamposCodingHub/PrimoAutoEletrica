# PRIMOX WORKSHOP — B6 AUTOELETRICA TÉCNICA AUDIT (12V & 24V)
**Data:** 2026-09-25  
**Fase:** B6 — Piloto Comercial Controlado  

---

## 1. Escopo das Rotas de Diagnóstico Guiado (D01 a D06)
O módulo técnico de autoelétrica foi homologado em operações diárias com medições de grandezas elétricas reais:

| Rota | Sistema Avaliado | Condições de Teste | Parâmetros Linha Leve (12V) | Parâmetros Linha Pesada (24V) | Unidades Auditadas |
|---|---|---|---|---|---|
| **D01** | Sistema de Partida / Motor de Arranque | Queda de tensão durante a partida do motor | Mínimo aceitável: 9.6V | Mínimo aceitável: 19.2V | Volts (V), Amperes (A) |
| **D02** | Alternador / Circuito de Carga | Tensão sob marcha lenta e sob carga máxima (faróis + ar) | 13.8V a 14.5V | 27.6V a 28.8V | Volts (V), Amperes (A) |
| **D03** | Bateria & Retificação | Tensão de repouso e ripple AC dos diodos | Repouso: > 12.4V; Ripple: < 0.3V AC | Repouso: > 24.8V; Ripple: < 0.5V AC | Volts (V), mV AC |
| **D04** | Corrente Parasita / Fuga | Veículo em repouso com chave desligada e portas travadas | Limite máximo: 0.05A (50mA) | Limite máximo: 0.08A (80mA) | Amperes (A), mA |
| **D05** | Queda de Tensão em Cabos / Chicotes | Medição entre borne positivo da bateria e entrada do alternador/arranque | Máximo permitido: 0.2V (200mV) | Máximo permitido: 0.4V (400mV) | Volts (V), mV |
| **D06** | Iluminação, Relés & Consumo | Comutação de relés e consumo em linhas auxiliares | Resistência de bobina: 70Ω a 90Ω | Resistência de bobina: 280Ω a 350Ω | Ohms (Ω), A, % Duty Cycle |

---

## 2. Preservação de Legado (D07 a D17)
- As rotas complementares e especializadas de D07 a D17 permaneceram completamente preservadas na arquitetura sem substituições ou exclusões.

---

## 3. Persistência Estruturada
- Os relatórios de laudo técnico foram gravados em formato JSON estruturado no diretório `%LOCALAPPDATA%\PrimoAutoEletrica\AutoEletrica\diagnosticos\`.
- O isolamento A/B foi comprovado em campo: intervenções técnicas sucessivas no mesmo veículo geram registros com carimbo de tempo distintos, sem sobrescrever o diagnóstico inicial.
