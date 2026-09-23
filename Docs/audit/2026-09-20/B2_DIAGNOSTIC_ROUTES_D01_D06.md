# PRIMOX WORKSHOP — ROTEIROS TÉCNICOS DE DIAGNÓSTICO (D01 A D06)
## ESPECIFICAÇÃO DE TESTES, MEDIÇÕES E LIMITES PERICIAIS

**Data:** 2026-09-23  
**Branch:** `audit/product-discovery-2026-09`  
**Objetivo:** Mapear em detalhe os roteiros fundamentais de autoeletricidade (D01 a D06) com grandezas físicas, referências nominais para sistemas 12V (leve) e 24V (pesado) e lógica de decisão técnica.

---

## 1. Roteiro D01 — Veículo Não Dá Partida

* **Código:** `D01`
* **Título:** Veículo não dá partida (Motor de partida inoperante ou mudo ao girar a chave / acionar start).
* **Objetivo:** Isolar se a causa raiz é bateria descarregada/danificada, queda de tensão nos cabos de alimentação, ausência de comando de ignição no automático (terminal 50) ou defeito interno no motor de partida.
* **Entradas:**
  - VeiculoId, Placa, Modelo, SistemaEletrico (12V ou 24V);
  - Sintoma relatado pelo motorista;
  - OrdemServicoId vinculada.
* **Testes & Sequência de Execução:**
  1. *Teste 1.1:* Tensão da bateria em repouso (borne a borne).
  2. *Teste 1.2:* Tensão da bateria durante a tentativa de partida.
  3. *Teste 1.3:* Sinal de comando de partida no terminal 50 do automático.
  4. *Teste 1.4:* Queda de tensão no circuito positivo principal (Bateria (+) até B+ Motor de Partida).
  5. *Teste 1.5:* Queda de tensão no circuito negativo (Carcaça Motor de Partida até Bateria (-)).
* **Medições & Valores Esperados:**
  - *1.1 Tensão Repouso:* >= 12.4V (12V) / >= 24.8V (24V). Unidade: `V`.
  - *1.2 Tensão em Partida:* >= 9.6V (12V) / >= 19.2V (24V). Unidade: `V`.
  - *1.3 Sinal Terminal 50:* >= 10.5V (12V) / >= 21.0V (24V) sob acionamento. Unidade: `V`.
  - *1.4 Queda Positivo:* <= 0.5V. Unidade: `V`.
  - *1.5 Queda Negativo (Aterramento):* <= 0.3V. Unidade: `V`.
* **Critérios de Resultado:**
  - Se Tensão Partida < 9.6V e Repouso baixo: `FORA_DO_ESPERADO` (Bateria sem carga ou sulfatada).
  - Se Sinal Terminal 50 = 0V: `FORA_DO_ESPERADO` (Falha comutador/relé partida/bloqueador).
  - Se Queda Cabo > 0.5V: `FORA_DO_ESPERADO` (Cabo oxidado ou terminal frouxo).
  - Se todas as medições forem normais e o motor não girar: `FORA_DO_ESPERADO` (Motor de partida travado/escovas gastas).
* **Conclusões & Correções Típicas:**
  - Substituição ou recarga de bateria;
  - Reparo/substituição do automático ou escovas do motor de partida;
  - Troca de terminal de bateria / reforço de aterramento.

---

## 2. Roteiro D02 — Bateria Descarregando (Fuga de Corrente)

* **Código:** `D02`
* **Título:** Bateria descarregando parada ou perdendo carga após período estacionada.
* **Objetivo:** Medir a corrente de consumo em repouso após o veículo entrar em modo sleep (standby dos módulos eletrônicos) e identificar circuitos ou acessórios ladrões de carga.
* **Entradas:**
  - Tipo de veículo, acessórios pós-venda instalados (rastreador, multimídia, alarme, amplificador).
* **Testes & Sequência de Execução:**
  1. *Teste 2.1:* Teste de condutância e CCA da bateria (para descartar auto-descarga interna).
  2. *Teste 2.2:* Medição de corrente de fuga em repouso (com alicate amperímetro DC ou multímetro em série no borne negativo, aguardando 15 a 30 min para sleep mode).
  3. *Teste 2.3:* Teste de fuga do alternador (desconectando cabo B+ do alternador para avaliar diodo em fuga).
  4. *Teste 2.4:* Isolamento de circuitos por remoção seletiva de fusíveis da caixa principal.
* **Medições & Valores Esperados:**
  - *2.1 CCA Real da Bateria:* >= 85% do valor nominal de etiqueta. Unidade: `CCA (A)`.
  - *2.2 Corrente de Fuga (Sleep):* <= 50 mA (0.05A) em linha leve; <= 80 mA (0.08A) em linha pesada com tacógrafo/rastreador. Unidade: `mA`.
  - *2.3 Retorno do Alternador:* 0.0 mA. Unidade: `mA`.
* **Critérios de Resultado:**
  - Se Corrente de Fuga > 50 mA: `FORA_DO_ESPERADO` (Dreno parasitário no circuito).
  - Se CCA < 70%: `FORA_DO_ESPERADO` (Bateria degradada com alta resistência interna).
* **Conclusões & Correções Típicas:**
  - Placa de diodos do alternador substituída;
  - Reconfiguração ou substituição de módulo/acessório com defeito de sleep;
  - Troca da bateria por fim de vida útil.

---

## 3. Roteiro D03 — Alternador Não Carrega (Sistema de Carga)

* **Código:** `D03`
* **Título:** Alternador não carrega / Luz de bateria acesa no painel / Tensão não sobe com motor ligado.
* **Objetivo:** Avaliar a capacidade do alternador de gerar energia e regular a tensão do sistema elétrico sob marcha lenta e sob plena carga de consumidores.
* **Entradas:**
  - Tensão nominal (12V ou 24V), capacidade do alternador (ex: 90A, 120A, 150A), rotação do motor.
* **Testes & Sequência de Execução:**
  1. *Teste 3.1:* Estado e tensão da correia de acionamento do alternador.
  2. *Teste 3.2:* Tensão nos bornes da bateria em marcha lenta sem consumidores.
  3. *Teste 3.3:* Tensão nos bornes da bateria com carga total ligada (farol alto, ar-condicionado, desembaçador).
  4. *Teste 3.4:* Tensão direta na saída B+ do alternador (comparação com a bateria para detectar perda em cabos).
  5. *Teste 3.5:* Teste de corrente gerada (alicate amperímetro na saída B+ sob carga).
  6. *Teste 3.6:* Ripple AC (tensão alternada residual na bateria para diagnosticar diodo retificador aberto).
* **Medições & Valores Esperados:**
  - *3.2 Tensão Marcha Lenta (Sem Carga):* 13.8V a 14.6V (12V) / 27.6V a 28.8V (24V). Unidade: `V`.
  - *3.3 Tensão Com Carga Plena:* >= 13.5V (12V) / >= 27.0V (24V). Unidade: `V`.
  - *3.4 Diferencial B+ vs Bateria:* <= 0.3V de perda no cabo. Unidade: `V`.
  - *3.6 Ripple AC:* < 0.25V AC (indica retificação perfeita). Unidade: `V`.
* **Critérios de Resultado:**
  - Tensão < 13.5V ou > 15.0V: `FORA_DO_ESPERADO` (Defeito de regulagem ou geração).
  - Tensão AC > 0.5V: `FORA_DO_ESPERADO` (Diodo retificador aberto/danificado).
* **Conclusões & Correções Típicas:**
  - Substituição do regulador de voltagem;
  - Substituição da placa retificadora / estator;
  - Troca da correia ou polia roda-livre do alternador.

---

## 4. Roteiro D04 — Motor de Partida Pesado (Arranque Arrasto)

* **Código:** `D04`
* **Título:** Motor de partida pesado (arranque arrastado, gira lento mesmo com bateria carregada).
* **Objetivo:** Discriminar se o arraste decorre de corrente excessiva por atrito mecânico/curto no induzido, queda de potencial nos cabos de força ou limitação de entrega de corrente pela bateria.
* **Entradas:**
  - Cilindrada do motor, combustível, temperatura do motor no teste.
* **Testes & Sequência de Execução:**
  1. *Teste 4.1:* Teste de condutância e CCA da bateria.
  2. *Teste 4.2:* Medição de corrente de pico e corrente contínua de partida (alicate amperímetro DC com função Inrush/Peak).
  3. *Teste 4.3:* Queda de tensão dinâmica no cabo positivo durante o giro.
  4. *Teste 4.4:* Queda de tensão dinâmica no aterramento do motor durante o giro.
  5. *Teste 4.5:* Inspeção de temperatura nos cabos e terminais após tentativa de partida.
* **Medições & Valores Esperados:**
  - *4.2 Corrente de Arranque (Motor Leve 1.0 - 2.0L):* Pico 150A a 250A; contínua 80A a 120A. Unidade: `A`.
  - *4.2 Corrente de Arranque (Diesel Pesado 24V):* Pico 400A a 700A; contínua 200A a 350A. Unidade: `A`.
  - *4.3 Queda Positivo:* <= 0.5V. Unidade: `V`.
  - *4.4 Queda Negativo:* <= 0.3V. Unidade: `V`.
* **Critérios de Resultado:**
  - Se Corrente > 350A em motor leve: `FORA_DO_ESPERADO` (Buchas desgastadas fazendo induzido roçar na sapata polar).
  - Se Queda Cabo > 0.6V: `FORA_DO_ESPERADO` (Resistência de contato no cabo positivo ou massa).
* **Conclusões & Correções Típicas:**
  - Revisão do motor de partida com troca de buchas, porta-escovas e lubrificação do bendix;
  - Substituição do cabo principal de bateria ou malha de aterramento do motor.

---

## 5. Roteiro D05 — Fusível Queimando (Curto-Circuito / Sobrecarga)

* **Código:** `D05`
* **Título:** Fusível queimando repetidamente ao acionar determinado circuito.
* **Objetivo:** Rastrear curtos-circuitos contra a carcaça (massa) ou sobrecorrente causada por componente elétrico travado sem queimar fusíveis sucessivos durante a medição.
* **Entradas:**
  - Identificação do fusível (posição na central, amperagem nominal especificada), circuito alimentado.
* **Testes & Sequência de Execução:**
  1. *Teste 5.1:* Verificação da amperagem nominal do fusível conforme manual técnico da montadora.
  2. *Teste 5.2:* Medição de resistência entre o lado de carga do fusível e o terra (massa) com circuito desenergizado.
  3. *Teste 5.3:* Instalação de lâmpada série (ou disjuntor rearmável de teste) no soquete do fusível para teste visual com carga.
  4. *Teste 5.4:* Desconexão sequencial dos consumidores do ramal (lâmpadas, motores, módulos) para isolar o trecho em curto.
  5. *Teste 5.5:* Teste de vibração e movimentação mecânica de chicotes em passagens de lataria.
* **Medições & Valores Esperados:**
  - *5.1 Amperagem:* Especificação idêntica ao manual (ex: 10A, 15A, 20A). Unidade: `A`.
  - *5.2 Resistência para Massa:* Deve ser proporcional às cargas normais (R = V/I). Para circuito de 10A, R > 1.2 Ω. Leitura próxima a 0.0 Ω indica curto franco. Unidade: `Ω`.
* **Critérios de Resultado:**
  - Resistência < 0.2 Ω no soquete sem consumidores: `FORA_DO_ESPERADO` (Curto-circuito no chicote contra a lataria).
  - Consumo com lâmpada série acesa no brilho máximo: `FORA_DO_ESPERADO`.
* **Conclusões & Correções Típicas:**
  - Reparo e isolação de chicote esmagado em coluna/porta/passa-muro;
  - Substituição do consumidor em curto (ex: motor de trava, bomba de lavador, relé colado).

---

## 6. Roteiro D06 — Farol Fraco / Iluminação Oscilante

* **Código:** `D06`
* **Título:** Farol acende com baixa intensidade luminosa, lâmpada oscilando ou aquecendo conector.
* **Objetivo:** Identificar perda de rendimento óptico por queda de potencial nos chicotes, conectores derretidos ou deficiência de aterramento frontal.
* **Entradas:**
  - Lado afetado (esquerdo / direito), tipo de lâmpada instalada (H4, H7, LED, halógena padrão).
* **Testes & Sequência de Execução:**
  1. *Teste 6.1:* Tensão real no conector da lâmpada com farol ligado sob carga.
  2. *Teste 6.2:* Queda de tensão no circuito positivo do farol (Bateria (+) até terminal do farol).
  3. *Teste 6.3:* Queda de tensão no circuito negativo do farol (Terminal terra da lâmpada até Bateria (-)).
  4. *Teste 6.4:* Inspeção visual de derretimento térmico e oxidação nos terminais do soquete.
* **Medições & Valores Esperados:**
  - *6.1 Tensão no Soquete da Lâmpada:* >= 12.8V com motor ligado (12V) / >= 26.0V (24V). Unidade: `V`.
  - *6.2 Queda no Positivo:* <= 0.4V. Unidade: `V`.
  - *6.3 Queda no Aterramento:* <= 0.2V. Unidade: `V`.
* **Critérios de Resultado:**
  - Se Tensão no Soquete < 11.5V: `FORA_DO_ESPERADO` (Queda acentuada gera perda de até 35% do fluxo luminoso).
  - Se Queda no Terra > 0.4V: `FORA_DO_ESPERADO` (Aterramento dianteiro deficiente).
* **Conclusões & Correções Típicas:**
  - Substituição do soquete cerâmico e conector;
  - Instalação de relé auxiliar para alimentação direta com cabo dimensionado;
  - Limpeza e restauração do parafuso de aterramento na lataria dianteira.
