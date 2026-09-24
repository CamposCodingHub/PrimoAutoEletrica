# PRIMOX WORKSHOP — B5.0
## REQUISITOS E CRITÉRIOS PARA OFICINA PILOTO (FASE B6)

**Escopo:** Seleção, Preparação e Acompanhamento do Piloto Comercial Controlado  
**Duração do Piloto:** 30 dias corridos  
**Status:** **DEFINIDO PARA EXECUÇÃO NA FASE B6**

---

### 1. Perfil da Oficina Piloto Candidata

A oficina parceira para o piloto controlado deve preencher os seguintes critérios técnicos e operacionais:
- **Segmento:** Oficina de Autoelétrica ou Mecânica Geral com forte demanda elétrica (veículos leves ou pesados).
- **Volume Operacional:** Média de 5 a 20 veículos atendidos por dia.
- **Equipe:** Mínimo de 1 recepcionista/orçamentista, 1 responsável financeiro/caixa e 2 eletricistas/mecânicos.
- **Disponibilidade:** Comprometimento do gestor em utilizar o PRIMOX como software principal durante o período de homologação de 30 dias.

---

### 2. Requisitos de Infraestrutura de Hardware e Software

| Requisito | Especificação Mínima | Especificação Recomendada |
| :--- | :--- | :--- |
| **Processador** | Intel Core i3 (7ª geração+) ou AMD Ryzen 3 | Intel Core i5 (10ª geração+) ou AMD Ryzen 5 |
| **Memória RAM** | 8 GB | 16 GB |
| **Armazenamento** | SSD com 20 GB livres | SSD NVMe com 50 GB livres |
| **Sistema Operacional** | Windows 10 (64-bit, versão 21H2+) | Windows 11 Pro (64-bit) |
| **Resolução de Tela** | 1366x768 pixels | 1920x1080 (Full HD) |
| **Periféricos** | Impressora não fiscal térmica (80mm) ou A4 | Impressora A4 laser + Leitor de código de barras USB |
| **No-Break (Nobreak)**| Nobreak de 600VA no computador mestre | Nobreak senoidal de 1200VA |

---

### 3. Protocolo de Treinamento da Equipe (Playbook de 4 Horas)

O treinamento pré-piloto é dividido em 4 módulos de 1 hora cada:
1. **Módulo 1: Recepção e Vendas (1h):** Cadastro de clientes/veículos, busca inteligente, elaboração de orçamentos e conversão em OS.
2. **Módulo 2: Operação Técnica (1h):** Uso do diagnóstico estruturado (D01-D06), preenchimento do prontuário elétrico e registro de medições antes/depois.
3. **Módulo 3: Estoque e Compras (1h):** Consulta de catálogo, aplicação de peças na OS, importação de NF-e via XML e controle de estoque mínimo.
4. **Módulo 4: Caixa e Fechamento (1h):** Recebimento de OS, parcelamento, abertura/fechamento de caixa e rotina diária de backup.

---

### 4. Protocolo de Suporte e Critérios de Rollback

- **SLA de Suporte no Piloto:**
  - P0 (Parada de atendimento ou erro de banco): Resposta em até 15 minutos / Solução em até 1 hora.
  - P1 (Falha não bloqueante em módulo): Solução em até 4 horas.
- **Critério de Rollback de Emergência:** Se for identificado qualquer risco de corrupção de dados ou paralisação do estabelecimento sem contorno imediato, o sistema volta ao estado original da oficina através da restauração do backup imediatamente anterior.
