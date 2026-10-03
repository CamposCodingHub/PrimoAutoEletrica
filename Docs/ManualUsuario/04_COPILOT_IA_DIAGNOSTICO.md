# PRIMOX Workshop 2.0 — Manual do Usuário
## 04. Copilot de IA Dual-Engine (Operacional + Especialista Elétrico)

---

### 1. O que é o PRIMOX Copilot?

O **PRIMOX Copilot** é o assistente inteligente integrado nativamente ao ERP PRIMOX Workshop. Ele atua em duas frentes fundamentais:
1. **Assistente de Operação do ERP:** Auxilia você a navegar pelo sistema, localizar peças em falta, verificar ferramentas emprestadas, consultar clientes e orientar rotinas do software.
2. **Especialista Sênior em Auto Elétrica Automotiva:** Fornece respostas técnicas detalhadas, procedimentos de bancada, tolerâncias elétricas (Volts, Amperes, Ohms, milissegundos) e testes passo a passo com multímetro e osciloscópio.

---

### 2. Como Acessar o Copilot

- **Atalho do Teclado:** Pressione `Ctrl + I` em qualquer tela do sistema para abrir ou fechar o painel lateral do Copilot.
- **Botão no Topo:** Clique no botão com ícone de lâmpada/IA (**Copilot**) localizado na barra superior do ERP.
- **Fechar o Painel:** Pressione `Esc` ou clique no botão `X` no cabeçalho do Copilot.

---

### 3. Arquitetura Dual-Engine: Nuvem vs. Modo Offline

O Copilot possui inteligência híbrida para garantir que a oficina nunca fique desassistida:

```
┌───────────────────────────────────────────────────────────────┐
│                    ARQUITETURA DUAL-ENGINE                    │
├───────────────────────────────┬───────────────────────────────┤
│         MOTOR NUVEM           │         MOTOR OFFLINE         │
│     (Google Gemini 2.0)       │       (Determinístico)        │
│                               │                               │
│ • Conectado à internet        │ • 100% autônomo sem internet  │
│ • Respostas abertas e fluidas │ • Tolerâncias exatas de fábrica│
│ • Raciocínio multi-etapas     │ • Zero latência               │
│ • Enriquecido com RAG local   │ • Diagnóstico direto por DTC  │
└───────────────────────────────┴───────────────────────────────┘
```

- **Badge de Status:** No topo do painel do Copilot, um selo indica qual motor está ativo no momento:
  - `Gemini Nuvem` (Verde / Ciano): Conexão ativa com a API do Google Gemini.
  - `Modo Offline (Determinístico)` (Âmbar): Operando com a base de conhecimento local embutida.

---

### 4. Configuração da Chave Google Gemini na Interface

Para ativar o poder total da IA generativa com o Google Gemini:

1. Acesse o menu **Configurações do Sistema** (ícone de engrenagem).
2. Clique na aba **Inteligência Artificial (Copilot)**.
3. Preencha os parâmetros:
   - **Habilitar Copilot de IA:** Marque a caixa para manter a IA ativa.
   - **Chave de API do Google Gemini:** Cole sua chave privada. Caso não possua, você pode obter gratuitamente no [Google AI Studio](https://aistudio.google.com/app/apikey).
   - **Modelo de IA:**
     - `gemini-2.0-flash` (Padrão Recomendado): Velocidade excepcional e excelente precisão para oficinas.
     - `gemini-1.5-flash`: Econômico e estável.
     - `gemini-1.5-pro`: Modelo maior para diagnósticos de altíssima complexidade de engenharia.
   - **Instruções Personalizadas da Oficina:** Insira regras específicas da sua empresa (ex: *"Priorizar baterias Moura e Heliar"*, *"Mencionar sempre nossa garantia de 90 dias nos orçamentos"*).
4. **Testar Conexão:** Clique no botão **Testar conexão com Gemini**.
   - O sistema fará um teste em tempo real medindo a latência (ex: *Conectado com sucesso em 340 ms*).
5. Clique em **Salvar configurações de IA**. As novas configurações entram em vigor instantaneamente sem necessidade de reiniciar o sistema!

---

### 5. Guia Rápido de Diagnósticos Elétricos (RAG Técnico Integrado)

O motor do Copilot (tanto na nuvem quanto offline) é alimentado por uma base de regras técnicas rigorosas da indústria automotiva:

#### A. Códigos de Falha DTC Mais Comuns
- **P0562 (Tensão do Sistema Baixa):** Alternador inoperante, correia frouxa, regulador danificado ou bateria com sulfatação severa.
- **P0563 (Tensão do Sistema Alta):** Falha no circuito de excitação ou diodo zener do regulador em curto. Risco iminente de queimar módulos (ECU/BCM). Tensão acima de 15.5V.
- **P0620 (Circuito de Controle do Alternador):** Rompimento da linha LIN/COM entre a central de injeção e o regulador inteligente.
- **P0335 (Sensor de Posição da Árvore de Manivelas - CKP):** Sinal ausente ou ruído. Testar resistência ôhmica em sensores indutivos (geralmente entre 400 Ω e 1.200 Ω) ou alimentação 5V/12V em sensores Hall.
- **P0300 (Falha Múltipla de Ignição / Misfire):** Queda de tensão primária nas bobinas de ignição, cabos de vela com alta resistência (> 15 kΩ) ou velas com gap excessivo.

#### B. Teste de Queda de Tensão (Voltage Drop)
O método definitivo para encontrar mau contato e cabos oxidados que multímetros comuns não detectam com o carro desligado:
1. Ligue os faróis altos e desembaçador traseiro (carga total no sistema).
2. **Queda no Cabo Positivo (Bateria + ao B+ do Alternador ou Motor de Partida):**
   - Tolerância Máxima: $\le 0.20\text{ V}$.
   - Se superior, indica cabo com fios rompidos ou terminal oxidado.
3. **Queda no Circuito de Terra (Carcaça do Motor ou Chassi ao Polo - da Bateria):**
   - Tolerância Máxima: $\le 0.10\text{ V}$.
   - Se superior, indica malha de aterramento (cordoalha) rompida ou parafuso com ferrugem.

#### C. Teste de Ripple AC de Alternador
Diagnostica diodos retificadores abertos ou em curto sem precisar desmontar o alternador do veículo:
1. Com o motor ligado a 2.000 RPM e carga ligada, coloque o multímetro na escala de **Tensão Alternada (VAC)** ou utilize um osciloscópio acoplado em AC.
2. Conecte as pontas diretamente nos polos da bateria.
3. **Limite Máximo Tolerado:** $\le 0.50\text{ VAC}$ ($500\text{ mV}$).
4. Valores acima de $500\text{ mV}$ causam travamento de módulos eletrônicos e ruído em sensores magnéticos.

#### D. Teste de Corrente de Fuga Parasita (Descarga da Bateria)
Para veículos que descarregam a bateria após alguns dias parados:
1. Ligue um alicate amperímetro DC ou multímetro em série com o polo negativo na escala de Amperes.
2. Tranque as portas, simule o fechamento do capô e aguarde o tempo de repouso das centrais (*Sleep Mode*, cerca de 15 a 30 minutos).
3. **Consumo Máximo Permitido:** $\le 50\text{ mA}$ ($0.05\text{ A}$).
4. Se o consumo for maior que $50\text{ mA}$, remova os fusíveis um a um até identificar o circuito culpado (frequentemente rastreadores, centrais multimídia instaladas incorretamente ou luz de porta-malas acesa).

#### E. Pinagem Padrão DIN 72552 para Relés Automotivos
Ao testar ou montar relés auxiliares de 4 e 5 pinos:
- **Pino 30:** Entrada de corrente contínua direta da bateria (linha 30, com fusível).
- **Pino 87:** Saída para a carga quando o relé é acionado (faróis, bomba, ventoinha).
- **Pino 87a:** Saída normalmente fechada (NF) em relés de 5 pinos.
- **Pino 86:** Positivo da bobina de comando (geralmente ligado ao pós-chave ou botão).
- **Pino 85:** Negativo da bobina de comando (geralmente acionado pela ECU ou botão de aterramento).

#### F. Teste Físico de Rede CAN Bus
1. Desligue a ignição do veículo e aguarde 1 minuto.
2. Na tomada de diagnóstico OBD-II (16 pinos):
   - Pino 6: **CAN High**
   - Pino 14: **CAN Low**
3. Meça a resistência ôhmica entre o pino 6 e o pino 14 com o multímetro:
   - **Valor Esperado:** Exatamente **$60\ \Omega$** (resultado de dois resistores terminadores de $120\ \Omega$ em paralelo localizados na ECU e no Painel/BCM).
   - Se marcar **$120\ \Omega$**, um dos módulos terminadores está com o barramento rompido.
   - Se marcar **$0\ \Omega$**, há curto-circuito entre as linhas CAN-H e CAN-L.

---

### 6. Chips Rápidos e Ações Automáticas no ERP

Na parte inferior do chat do Copilot, botões interativos permitem navegar com 1 clique para as telas relevantes citadas pela IA:
- `Ir para Estoque`
- `Ver Ferramentaria`
- `Ver Falta & Compras`
- `Módulo Técnico`
- `Buscar Ordem de Serviço`
