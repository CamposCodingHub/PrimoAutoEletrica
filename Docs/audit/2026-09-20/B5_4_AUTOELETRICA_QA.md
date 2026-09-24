# PRIMOX Workshop — B5.4: Homologação Técnica de Autoelétrica (12V / 24V)

**Data:** 2026-09-24 20:45  
**Status:** **PASS**

---

## 1. Cobertura dos Sistemas de Diagnóstico (D01 a D06)

| Código | Subsistema | Modos Testados | Grandezas & Unidades | Status |
| :--- | :--- | :--- | :--- | :---: |
| **D01** | Bateria & Sistema de Partida | 12V e 24V | Tensão Repouso (V), Queda Partida (V), CCA (A) | **PASS** |
| **D02** | Alternador & Sistema de Carga | 12V e 24V | Tensão Carga (V), Corrente Máxima (A), Ripple (mV) | **PASS** |
| **D03** | Iluminação & Sinalização | 12V e 24V | Consumo por Circuito (A), Queda de Tensão (V) | **PASS** |
| **D04** | Ignição & Injeção Eletrônica | 12V | Resistência Primário/Secundário (Ω), Sinal Sensor | **PASS** |
| **D05** | Redes de Comunicação (CAN/LIN) | 12V e 24V | Resistência de Linha (60Ω), Tensão CAN-H / CAN-L | **PASS** |
| **D06** | Ar Condicionado & Auxiliares | 12V e 24V | Pressão Alta/Baixa (PSI), Corrente Eletroventilador | **PASS** |

---

## 2. Preservação de Histórico e Imutabilidade

- **Multi-Diagnóstico:** Criação de Diagnóstico A e Diagnóstico B para o mesmo veículo confirmou que **A não sobrescreve B**.
- **Histórico Histórico no Vehicle360:** Ambos os diagnósticos permanecem vinculados por chave estrangeira e são renderizados na linha do tempo técnica.
