# PRIMOX WORKSHOP — B5.0
## ESPECIFICAÇÃO TÉCNICA DO DIFERENCIAL AUTOELÉTRICA TECH/HEAVY 2.0

**Módulo:** Autoelétrica Técnica  
**Abrangência:** Linha Leve (12V) e Linha Pesada / Máquinas Agrícolas / Diesel (24V)  
**Status:** **ESPECIFICADO E ESTRUTURADO**

---

### 1. Proposta de Valor e Diferencial Competitivo

A maioria dos softwares de gestão para oficinas mecânicas trata serviços elétricos como simples itens textuais em uma OS ("Troca de escova", "Reparo no chicote"). 

O PRIMOX Workshop introduz a **Autoelétrica Técnica Estruturada**, que transforma a oficina em um centro de diagnóstico profissional:
- O sistema registra **medições físicas reais** com grandezas, momentos, condições de teste e tolerâncias.
- Calcula a **eficácia do reparo** comparando medições de entrada com medições pós-conserto (Delta pós-reparo).
- Fornece um **laudo técnico auditável** para o cliente ou frotista, comprovando a necessidade técnica da substituição de componentes.

---

### 2. Grandezas Elétricas e Físicas Suportadas

| Grandeza | Unidade | Aplicação Típica | Valores Típicos 12V | Valores Típicos 24V |
| :--- | :---: | :--- | :--- | :--- |
| **Tensão (V)** | Volts | Bateria em repouso, alternador em carga | 12.2V - 14.4V | 24.4V - 28.8V |
| **Queda de Tensão (ΔV)** | Volts / mV | Aterramentos, cabo positivo de partida | Máx. 0.20V (200mV) | Máx. 0.40V (400mV) |
| **Corrente (A)** | Ampères | Consumo de motor de partida, carga alternador | 90A - 220A (Partida) | 180A - 450A (Partida) |
| **Corrente de Fuga (mA)**| Miliampères | Consumo parasita em repouso | Máx. 50mA (0.05A) | Máx. 80mA (0.08A) |
| **Resistência (Ω)** | Ohms | Chicotes, bobinas, velas, sensores | 0.2Ω - 50kΩ | 0.2Ω - 50kΩ |
| **Capacidade de Partida**| CCA (A) | Saúde de baterias de partida | 350A - 850A CCA | 800A - 1400A CCA |
| **Temperatura (°C)** | Graus Celsius | Alternador, bateria, relé | -10°C a +110°C | -10°C a +120°C |
| **Rotação (RPM)** | RPM | Regime de carga do alternador | 850 RPM / 2500 RPM | 650 RPM / 1800 RPM |
| **Duty Cycle (%)** | Porcentagem | Válvulas PWM, controle alternador LIN/RVC| 0% a 100% | 0% a 100% |
| **Pressão (Bar/PSI)** | Bar / PSI | Linha de combustível, ar comprimido freio | 3.0 - 4.5 Bar | 8.0 - 12.0 Bar |

---

### 3. Matriz de Componentes Críticos — Linha Pesada (24V)

Nas operações com caminhões, cavalos mecânicos, ônibus e máquinas pesadas, o sistema contempla as especificidades de arquitetura elétrica dupla:

1. **Conjunto de Baterias em Série (2x 12V = 24V):**
   - Medição individual de cada bateria para detecção precoce de desbalanceamento de carga (desequalização).
   - Teste de CCA individual e conjunto.
2. **Motores de Partida Pesados:**
   - Teste de corrente de pico (Inrush current) e sustentação.
   - Avaliação de queda de tensão em relés de partida e chaves gerais.
3. **Alternadores de Alta Amperagem (28V / 80A - 150A):**
   - Verificação de ondulação (Ripple AC) para detecção de diodos em curto ou abertos.
   - Teste de regulação sob carga plena (faróis, ar condicionado, implementos).
4. **Sistemas de Aterramento de Chassi:**
   - Medição de queda de tensão entre polo negativo da bateria, chassi e carcaça do motor.
5. **Rede de Comunicação CAN:**
   - Resistência de terminação da rede CAN (60Ω nominais entre pinos CAN-H e CAN-L com chave desligada).

---

### 4. Classificação de Hardware Externo

O PRIMOX Workshop **NÃO depende de hardware proprietário para funcionar**. A digitação dos valores das medições é estruturada diretamente pelo operador técnico.

- **Equipamentos Compatíveis (Operação Manual Assistida):**
  - Multímetros digitais automotivos
  - Alicates amperimétricos AC/DC
  - Analisadores de bateria por condutância (CCA)
  - Osciloscópios automotivos portáteis
- **Integração Eletrônica Direta (Hardware Scanners):**
  - PassThru J2534, scanners OBD2 ELM327 ou osciloscópios USB: **Classificados estritamente como EXTERNAL_DEPENDENCY / FUTURE**. Não há promessa comercial de leitura automatizada via cabo na versão 1.0.
