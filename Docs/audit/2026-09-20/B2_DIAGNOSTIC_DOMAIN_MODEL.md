# PRIMOX WORKSHOP — MODELO DE DOMÍNIO DIAGNÓSTICO (FASE B2)
## AUTOELÉTRICA TECH/HEAVY + DIAGNÓSTICO TÉCNICO ESTRUTURADO

**Data:** 2026-09-23  
**Branch:** `audit/product-discovery-2026-09`  
**Escopo:** Especificação Formal do Modelo Conceitual e Técnico de Diagnóstico Elétrico Automotivo.

---

## 1. Diagrama Conceitual de Domínio

```text
+-------------------------------------------------------------------------------+
|                                    VEÍCULO                                    |
|  - Id (Guid)                                                                  |
|  - Placa / Modelo / Ano / ClienteId                                           |
|  - Telemetria Atual (Sistema 12V/24V, BateriaInstalada, Alternador, etc.)    |
+---------------------------------------+---------------------------------------+
                                        | 1
                                        |
                                        | N
+---------------------------------------v---------------------------------------+
|                               ORDEM DE SERVIÇO                                |
|  - Id (Guid)                                                                  |
|  - Numero / DataAbertura / Status                                             |
|  - VeiculoId (Guid) / ClienteId (Guid)                                        |
+---------------------------------------+---------------------------------------+
                                        | 1
                                        |
                                        | 1..N
+---------------------------------------v---------------------------------------+
|                             DIAGNÓSTICO TÉCNICO                               |
|  - Id (Guid) [PK]                                                             |
|  - OrdemServicoId (Guid) [FK Obrigatória]                                     |
|  - VeiculoId (Guid) [FK Obrigatória]                                          |
|  - TecnicoId / FuncionarioId (Guid/String)                                    |
|  - DataHora (DateTime)                                                        |
|  - Status (EmAndamento, Concluido, Cancelado)                                 |
|  - RoteiroCodigo (ex: "D01", "D02", "D03")                                    |
|  - SintomaRelatado (String)                                                   |
|  - SintomaCategoria (Partida, Carga, Iluminacao, Acessorios, Conforto, Rede)  |
|  - DiagnosticoLaudo (String)                                                  |
|  - CausaStatus (Confirmada, Provavel, NaoDeterminada)                         |
|  - CausaDescricao (String)                                                    |
|  - CorrecaoExecutada (String)                                                 |
|  - PecaUtilizadaId (Guid? -> FK Produtos)                                     |
|  - ServicoUtilizadoId (Guid? -> FK Servicos)                                  |
|  - ObservacoesGerais (String)                                                 |
+---------------------------------------+---------------------------------------+
                                        | 1
                                        |
                                        | 1..N
+---------------------------------------v---------------------------------------+
|                             DIAGNÓSTICO MEDIÇÃO                               |
|  - Id (Guid) [PK]                                                             |
|  - DiagnosticoId (Guid) [FK Obrigatória]                                      |
|  - NomeTeste (ex: "Tensao da bateria durante a partida")                      |
|  - TipoGrandeza (Tensao, Corrente, Resistencia, QuedaTensao, Fuga, CCA)       |
|  - Instrumento (Multimetro, AlicateAmperimetroDC, TestadorBateria, etc.)      |
|  - Unidade (V, mV, A, mA, Ohm, kOhm, rpm, °C, Hz, %, CCA)                    |
|  - ValorReferenciaMin (Decimal?)                                              |
|  - ValorReferenciaMax (Decimal?)                                              |
|  - TextoReferencia (String - ex: ">= 9.6V (12V) / >= 19.2V (24V)")            |
|  - ValorInicial (Decimal - Medição de Entrada)                                |
|  - ValorPosReparo (Decimal? - Medição Pós-Reparo / Validação Final)           |
|  - Resultado (NORMAL, FORA_DO_ESPERADO, INCONCLUSIVO, NAO_REALIZADO)          |
|  - ObservacaoTecnica (String)                                                 |
+-------------------------------------------------------------------------------+
```

---

## 2. Separação Rigorosa dos Conceitos Técnicos

### 2.1 Sintoma
O que o motorista ou cliente relata. Exemplo: *"Ao virar a chave, o motor não gira"* ou *"Luz de bateria acendeu na rodovia"*. Não é um diagnóstico.

### 2.2 Teste vs. Medição
* **Teste:** Procedimento operacional prescrito pelo roteiro (ex: *"Medição de queda de tensão no cabo positivo entre o borne da bateria e o terminal B+ do motor de partida"*).
* **Medição:** Grandeza física quantitativa obtida com instrumento aferido, expressa com valor numérico e unidade explícita (ex: `0.85 V`).

### 2.3 Resultado
Classificação técnica objetiva comparando a medição com a referência aceitável:
* `NORMAL`: Medição dentro dos limites nominais do fabricante para a tensão do sistema (12V ou 24V).
* `FORA_DO_ESPERADO`: Medição indica anomalia física (queda excessiva, fuga anormal, tensão insuficiente).
* `INCONCLUSIVO`: Leitura instável ou condição de teste ruidosa.
* `NAO_REALIZADO`: Teste ignorado ou não aplicável ao cenário.

### 2.4 Diagnóstico vs. Causa
* **Diagnóstico:** Constatação técnica da falha funcional no sistema (ex: *"Queda de tensão excessiva no circuito principal de partida"*).
* **Causa:** O motivo físico/mecânico da falha, categorizado formalmente:
  - `CONFIRMADA`: Evidência pericial comprovada (ex: *"Terminal olhal positivo oxidado e com 3 filamentos partidos"*).
  - `PROVAVEL`: Hipótese com alta probabilidade técnica ainda não desmontada.
  - `NAO_DETERMINADA`: Defeito intermitente ainda não isolado.

### 2.5 Correção
A ação executada pela oficina para sanar a causa raiz:
- Substituição de peça (vinculada ao `ProdutoId` do estoque real);
- Mão de obra técnica de reparo de chicote ou reaperto;
- Registro do técnico responsável pelo serviço.

### 2.6 Teste Pós-Reparo (Antes vs. Depois)
Diferencial pericial do PRIMOX Workshop:
```text
[TESTE INICIAL]              [CORREÇÃO]              [TESTE FINAL PÓS-REPARO]
  Queda: 0.85 V      ──>    Troca de Terminal   ──>      Queda: 0.12 V
(FORA DO ESPERADO)               e Cabo B+                  (NORMAL / APROVADO)
```

---

## 3. Unidades Físicas e Tipos Padronizados

| Grandeza | Unidades Permitidas | Instrumento Típico | Exemplo de Aplicação |
| :--- | :---: | :--- | :--- |
| **Tensão Contínua** | `V`, `mV` | Multímetro Digital | Tensão em repouso da bateria (12.6V / 25.2V) |
| **Queda de Tensão** | `V`, `mV` | Multímetro (escala mV/V) | Queda de potencial em cabos sob partida (< 0.5V) |
| **Corrente Contínua** | `A`, `mA` | Alicate Amperímetro DC | Corrente de fuga (< 50mA) / Partida (150A a 600A) |
| **Resistência** | `Ω`, `kΩ` | Ohmímetro / Multímetro | Bobina de relé (60Ω a 120Ω), continuidade chicote |
| **Capacidade de Partida**| `CCA` (A) | Testador Condutância CCA | Capacidade real de arranque da bateria vs rótulo |
| **Frequência** | `Hz` | Multímetro / Osciloscópio | Sinal do alternador (terminal W) / PWM |
| **Rotação** | `rpm` | Scanner / Tacômetro | Marcha lenta para teste de carga do alternador |
| **Temperatura** | `°C` | Termômetro Infravermelho | Aquecimento anormal em conectores e terminais |

---

## 4. Suporte a Linha Leve (12V) e Linha Pesada (24V) — Autoelétrica Heavy

O modelo suporta frotas mistas sem necessidade de criar subsistemas paralelos. A chave `SistemaEletrico` orienta os parâmetros de referência:

| Teste | Referência Linha Leve (12V) | Referência Linha Pesada (24V) |
| :--- | :---: | :---: |
| **Bateria em Repouso (100% carregada)** | `12.6V a 12.8V` | `25.2V a 25.6V` (2 baterias em série) |
| **Bateria Limite Crítico de Descarga** | `< 12.2V` | `< 24.4V` |
| **Tensão Durante Partida** | `>= 9.6V` | `>= 19.2V` |
| **Carga do Alternador em Marcha Lenta** | `13.8V a 14.6V` | `27.6V a 28.8V` |
| **Corrente de Fuga em Repouso (Sleep)** | `< 50 mA (0.05A)` | `< 80 mA (0.08A)` (módulos tacógrafo/rastreador) |
| **Queda Máxima em Cabo Positivo Partida**| `< 0.5V` | `< 0.8V` (maior comprimento de cabo em caminhões) |
| **Queda Máxima no Circuito de Terra** | `< 0.2V` (chassi) | `< 0.4V` (longarina / motor chassi) |

---

## 5. Estratégia de Identidade e Integridade Referencial

* **Chave Primária do Diagnóstico:** `DiagnosticoId` (`Guid` não nulo).
* **Vínculo com a OS:** `OrdemServicoId` (`Guid` não nulo).
* **Vínculo com o Veículo:** `VeiculoId` (`Guid` não nulo).
* **Vínculo com o Cliente:** Obtido via `OrdemServico.ClienteId` ou espelhado como `ClienteId`.
* **Proibição Absoluta:** É terminantemente proibido associar diagnósticos por placa avulsa, nome do cliente, apelido do veículo ou substring textual.

Com este modelo de domínio formalizado, a rastreabilidade técnica atende aos mais exigentes padrões periciais automotivos e de garantia.
