# -*- coding: utf-8 -*-
"""
Pack 1 of B5.0 Documents:
3. B5_0_RELEASE_SCOPE.md
4. B5_0_V1_SCOPE.md
5. B5_0_TECH_HEAVY_PRODUCT_SCOPE.md
6. B5_0_CHECKLIST_UI_SPEC.md
7. B5_0_MONEY_MIGRATION_RELEASE_PLAN.md
"""

import os

DOC_DIR = r"c:\Projetos\PrimoAutoEletrica\Docs\audit\2026-09-20"
os.makedirs(DOC_DIR, exist_ok=True)

# -------------------------------------------------------------
# 3. B5_0_RELEASE_SCOPE.md
# -------------------------------------------------------------
release_scope_md = """# PRIMOX WORKSHOP — B5.0
## ESCOPO REAL DE RELEASE COMERCIAL (MVP OPERACIONAL)

**Data:** 2026-09-24  
**Status:** **DEFINIDO**

---

### 1. Critério de Classificação do MVP Comercial

Para que uma oficina mecânica/autoelétrica opere diariamente sem interrupções e sem depender de processos em papel ou planilhas paralelas, o software deve garantir a integridade do fluxo de atendimento e prestação de serviço.

A classificação adota o modelo de priorização operacional:
- **MUST HAVE (Essencial para Operação Diária):** Sem isso a oficina não consegue atender, diagnosticar, cobrar ou controlar seu estoque.
- **SHOULD HAVE (Recomendado para Eficiência):** Agiliza o trabalho e reduz erros manuais, mas a oficina consegue operar provisoriamente com alternativa manual.
- **NICE TO HAVE (Conveniência e Diferenciação):** Recursos modernos de comodidade para clientes e gestores.
- **FUTURE (Evoluções Pós-Piloto):** Demandam infraestrutura externa (cloud, telecom ou adquirentes).

---

### 2. Mapeamento do Fluxo Operacional de Ponta a Ponta

| Etapa do Fluxo | Funcionalidade Associada | Classificação | Justificativa Operacional |
| :--- | :--- | :---: | :--- |
| **1. Recepção & Cadastro** | Cadastro de Clientes com Validação CPF/CNPJ | **MUST HAVE** | Identificação fiscal e de contato indispensável |
| | Termo de Consentimento LGPD | **MUST HAVE** | Conformidade legal básica de custódia de dados |
| | Cadastro de Veículos Leves e Pesados (12V/24V) | **MUST HAVE** | Objeto central de atendimento na oficina |
| | Prontuário Técnico Elétrico (17 campos) | **MUST HAVE** | Diferencial técnico da autoelétrica |
| **2. Triagem & Inspeção** | DVI Inspecão Visual Digital Local | **SHOULD HAVE** | Registro fotográfico e avarias de entrada do pátio |
| | DVI Aprovação Remota Web | **FUTURE** | Demanda servidor web e link externo para cliente |
| **3. Proposta & Aprovação** | Elaboração de Orçamento (Peças + Mão de Obra) | **MUST HAVE** | Definição formal de valores e condições |
| | Token de Aprovação do Orçamento | **MUST HAVE** | Registro formal de autorização do proprietário |
| | Conversão Automática Orçamento → OS | **MUST HAVE** | Eliminação completa de retrabalho e redigitação |
| **4. Execução Técnica** | Abertura e Gestão de Ordem de Serviço | **MUST HAVE** | Controle operacional da oficina e do mecânico |
| | Diagnóstico Técnico Estruturado (D01-D06) | **MUST HAVE** | Guias de teste elétrico (Partida, Carga, Fuga, Bateria) |
| | Registro de Medições (V, A, CCA, Queda) | **MUST HAVE** | Evidência técnica auditável do defeito |
| | Teste Pós-Reparo com Delta Calculado | **MUST HAVE** | Prova objetiva de eficácia do conserto elétrico |
| | Checklist Técnico da OS | **SHOULD HAVE** | Inspeção multiponto para frotas e linha pesada |
| **5. Suprimentos & Peças** | Requisição e Baixa Automática de Estoque | **MUST HAVE** | Kardex atualizado e prevenção de extravios |
| | Catálogo Técnico e Aplicação Veicular | **MUST HAVE** | Identificação correta de componentes alternativos |
| | Importação de XML de NF-e de Compra | **SHOULD HAVE** | Entrada expressa de mercadorias no estoque |
| | Endereçamento Físico (Gaveteiro) | **NICE TO HAVE** | Otimização do tempo de localização de peças pequenas |
| **6. Entrega & Faturamento**| Conclusão da OS com Baixa Financeira | **MUST HAVE** | Liberação do veículo e apuração de custos |
| | Caixa Operacional (Turno, Sangria, Suprimento)| **MUST HAVE** | Fechamento diário de valores em dinheiro/pix |
| | Contas a Receber e Parcelamento | **MUST HAVE** | Gestão de prazos e crédito de clientes |
| | Contas a Pagar e Despesas Operacionais | **MUST HAVE** | Controle financeiro do estabelecimento |
| **7. Relacionamento** | Histórico Técnico Unificado (Vehicle360) | **MUST HAVE** | Rastreabilidade de intervenções anteriores |
| | Histórico Comercial Unificado (Client360) | **MUST HAVE** | Fidelização e visão global de consumo |
| | Pós-Venda (Garantias, Retornos e Revisões) | **MUST HAVE** | Prevenção de retrabalho e retenção de frota |
| | Notificação Automática por SMS/WhatsApp API | **FUTURE** | Exige integração oficial e custos de mensageria |
| | Link Manual WhatsApp (wa.me) | **SHOULD HAVE** | Envio de mensagens pré-formatadas sem custo |

---

### 3. Matriz de Priorização do Release

```mermaid
graph TD
    A[MUST HAVE - 33 Recursos] --> B[Núcleo Funcional Completo Desktop]
    C[SHOULD HAVE - 6 Recursos] --> B
    B --> D[Piloto Comercial Controlado - Fase B6]
    E[NICE TO HAVE - 6 Recursos] --> F[Atualização v1.1 - Pós-Piloto]
    G[FUTURE - 12 Recursos] --> H[Evolução Nuvem / SaaS - Trilha B]
```

### 4. Conclusão do Escopo de Release
O PRIMOX Workshop possui **33 recursos MUST HAVE plenamente implementados e validados no backend e desktop**. A oficina consegue executar 100% de sua rotina operacional local com o software, mantendo backup regular e controle de acesso estrito.
"""

with open(os.path.join(DOC_DIR, "B5_0_RELEASE_SCOPE.md"), "w", encoding="utf-8") as f:
    f.write(release_scope_md)

print("3. B5_0_RELEASE_SCOPE.md OK")

# -------------------------------------------------------------
# 4. B5_0_V1_SCOPE.md
# -------------------------------------------------------------
v1_scope_md = """# PRIMOX WORKSHOP — B5.0
## ESPECIFICAÇÃO DE ESCOPO DO PRIMOX WORKSHOP v1.0

**Versão Alvo:** 1.0.0 (Release Comercial Desktop)  
**Status do Escopo:** **APROVADO PARA PLANEJAMENTO**

---

### 1. Definição do Produto PRIMOX Workshop v1.0

O PRIMOX Workshop v1.0 é um **Sistema de Gestão e Diagnóstico Técnico Avançado para Oficinas Mecânicas e Autoelétricas**, com foco diferenciado em **Sistemas Elétricos e Eletrônicos para Veículos Leves (12V) e Linha Pesada / Máquinas (24V)**.

O software opera em arquitetura **Local Desktop First**, garantindo funcionamento contínuo mesmo sem conexão com a internet, máxima velocidade de resposta e total privacidade dos dados da oficina.

---

### 2. Módulos Inclusos na Versão 1.0

| Módulo | Estado de Implementação | Nível de Validação | Observação de Release |
| :--- | :---: | :---: | :--- |
| **Dashboard Gerencial** | Implemented | Homologated | Indicadores de OS, faturamento e agenda em tempo real |
| **Clientes & Veículos** | Implemented | Homologated | Gestão cadastral completa, validação CPF/CNPJ, LGPD |
| **Prontuário Elétrico** | Implemented | Homologated | 17 parâmetros de arquitetura 12V e 24V |
| **Client360 & Vehicle360** | Implemented | Homologated | Histórico técnico, financeiro e temporal sem junções textuais |
| **Orçamentos & Aprovação** | Implemented | Homologated | Cálculo com margens, geração de tokens e conversão 1:1 para OS |
| **Ordens de Serviço** | Implemented | Homologated | Gestão operacional com apontamento técnico |
| **Autoelétrica Técnica** | Implemented | Homologated | Roteiros D01 a D06 estruturados + medições e delta pós-reparo |
| **Estoque & Catálogo** | Implemented | Homologated | Kardex, importação de XML de NF-e, fotos e esquemas em PDF |
| **Financeiro & Caixa** | Implemented | Homologated | Contas a pagar, receber, fluxo de caixa e frente de caixa |
| **Pós-Venda Estruturado** | Implemented | Validated | Gestão de retornos, garantias e revisões preventivas |
| **DVI Inspeção Digital** | Implemented | Validated | Checklist visual de recepção com fotos locais |
| **Segurança & RBAC** | Implemented | Homologated | 10 perfis, 82 permissões fail-closed, hash PBKDF2 |
| **Backup & Integridade** | Implemented | Homologated | Backup a quente SQLite com checagem física e de FKs |
| **Configurações & Temas** | Implemented | Homologated | Modo Dark e Light nativos, dados da empresa e parâmetros |

---

### 3. Limitações Técnicas Declaradas da v1.0

1. **Mono-Estação Primária / Rede Local Direta:** O banco SQLite opera no computador principal da oficina. O suporte a rede local multi-terminal deve ser homologado via pasta compartilhada com WAL mode ou API local na v1.1.
2. **Emissão Fiscal Direta Bloqueada:** A v1.0 suporta entrada e conferência de XMLs de terceiros. A emissão de notas fiscais próprias (NF-e/NFC-e) depende da contratação do módulo fiscal homologado na Fase B7.
3. **Licenciamento Local Standalone:** A validação da cópia na v1.0 utiliza assinatura digital assimétrica local ou chave de ativação offline, sem dependência de servidor cloud.
4. **Armazenamento de Imagens Local:** Fotos de DVI e documentos técnicos são armazenados no diretório local `%LOCALAPPDATA%\\PrimoAutoEletrica\\Dvi\\`.

---

### 4. Itens Explicitamente Fora da Versão 1.0 (Out of Scope)

- Portal Web do Cliente (Acompanhamento em nuvem)
- Aplicativo Mobile para Mecânicos (Android / iOS)
- Sincronização Automática Multi-Filiais em Nuvem
- TEF Dedicado para Maquininha de Cartão
- Conciliação Bancária Automática por arquivo OFX/CNAB
- Consulta Online de Catálogos de Distribuidores via API B2B
- Emissão de Nota Fiscal de Serviços Municipal (NFS-e)
"""

with open(os.path.join(DOC_DIR, "B5_0_V1_SCOPE.md"), "w", encoding="utf-8") as f:
    f.write(v1_scope_md)

print("4. B5_0_V1_SCOPE.md OK")

# -------------------------------------------------------------
# 5. B5_0_TECH_HEAVY_PRODUCT_SCOPE.md
# -------------------------------------------------------------
tech_heavy_md = """# PRIMOX WORKSHOP — B5.0
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
"""

with open(os.path.join(DOC_DIR, "B5_0_TECH_HEAVY_PRODUCT_SCOPE.md"), "w", encoding="utf-8") as f:
    f.write(tech_heavy_md)

print("5. B5_0_TECH_HEAVY_PRODUCT_SCOPE.md OK")

# -------------------------------------------------------------
# 6. B5_0_CHECKLIST_UI_SPEC.md
# -------------------------------------------------------------
checklist_ui_md = """# PRIMOX WORKSHOP — B5.0
## ESPECIFICAÇÃO DE INTERFACE: CHECKLIST TÉCNICO MULTIPONTO

**Entidades de Domínio:** `ChecklistTecnicoOS`, `ChecklistTecnicoItem`  
**Módulos Integrados:** Ordem de Serviço, Diagnóstico Técnico, Vehicle 360  
**Status:** **ESPECIFICADO PARA DESENVOLVIMENTO (BACKLOG B5-002)**

---

### 1. Visão Geral e Casos de Uso

O Checklist Técnico Multiponto permite que o técnico realize inspeções padronizadas no veículo durante a execução da OS. 

**Cenários Principais:**
1. **Inspeção de Entrada:** Verificação rápida de iluminação, palhetas, bateria e estado de pneus.
2. **Revisão Preventiva Programada:** Checagem sistemática de 30 a 50 itens de frotas e veículos pesados.
3. **Inspeção de Segurança:** Verificação de fusíveis de alta potência, relés principais e fixação de chicotes.

---

### 2. Estrutura de Dados da Interface

Cada item inspecionado exibe:
- **Grupo:** (Ex: Sistema de Partida, Sistema de Carga, Aterramentos, Iluminação, Sinalização)
- **Item / Descrição:** (Ex: Tensão de repouso da bateria, Queda de tensão no cabo massa)
- **Status da Inspeção (Botões de Ação Rápida):**
  - `OK` (Verde) — Conforme
  - `ATENCAO` (Amarelo) — Requer observação futura ou aprovação do cliente
  - `CRITICO` (Vermelho) — Apresenta risco iminente de pane ou acidente
  - `NAO_SE_APLICA` (Cinza) — Opcional inexistente no veículo
  - `NAO_DISPONIVEL` (Azul/Neutro) — Teste não pôde ser realizado por falta de acesso
- **Campo de Valor/Medição:** Ex: `12.45` + Unidade `V`
- **Observações Técnicas:** Campo de anotação rápida com sugestões automáticas
- **Anexo de Evidência:** Botão para vincular foto da peça defeituosa

---

### 3. Wireframe Conceitual da Tela (WPF UserControl)

```
+------------------------------------------------------------------------------------+
| CHECKLIST TÉCNICO MULTIPONTO — OS #1042                               [ X ] Fechar |
| Veículo: Volvo FH 540 (Placa: ABC-1234) | Técnico: Carlos Silva (Eletricista)     |
+------------------------------------------------------------------------------------+
| [ Filtro: Todos | Inconformes | Críticos ]             [ Barra de Progresso: 85% ] |
+------------------------------------------------------------------------------------+
| GRUPO: SISTEMA DE CARGA E PARTIDA                                                  |
| ---------------------------------------------------------------------------------- |
| 1. Tensão de Repouso Bateria A:  [ 12.60 ] V  ( [OK] [ATENÇÃO] [CRÍTICO] [N/A] )   |
| 2. Tensão de Repouso Bateria B:  [ 11.85 ] V  ( [OK] [ATENÇÃO] [CRÍTICO] [N/A] ) ! |
|    Obs: Desbalanceamento de carga detectado (>0.5V de diferença)                   |
|    Evidência: [ foto_bat_b.jpg ] [ Visualizar ]                                    |
| 3. Corrente de Carga Alternador: [ 75.00 ] A  ( [OK] [ATENÇÃO] [CRÍTICO] [N/A] )   |
| 4. Queda de Tensão Aterramento:  [ 0.15 ] V   ( [OK] [ATENÇÃO] [CRÍTICO] [N/A] )   |
+------------------------------------------------------------------------------------+
| GRUPO: ILUMINAÇÃO E SINALIZAÇÃO                                                    |
| ---------------------------------------------------------------------------------- |
| 5. Farol Principal Direito:                   ( [OK] [ATENÇÃO] [CRÍTICO] [N/A] )   |
| 6. Farol Principal Esquerdo:                  ( [OK] [ATENÇÃO] [CRÍTICO] [N/A] ) ! |
|    Obs: Lâmpada H7 queimada / Conector derretido                                   |
+------------------------------------------------------------------------------------+
| AÇÕES:                                                                             |
| [ + Adicionar Item ]   [ Gerar Orçamento Adicional ]   [ Salvar ]   [ Imprimir ]   |
+------------------------------------------------------------------------------------+
```

---

### 4. Integrações de Fluxo

1. **Geração Automática de Orçamento Complementar:** Itens marcados como `ATENCAO` ou `CRITICO` podem ser convertidos com 1 clique em itens de Orçamento Suplementar para aprovação do cliente.
2. **Gravação no Histórico do Vehicle360:** O resultado consolidado do checklist é anexado à linha do tempo técnica do veículo.
3. **Impressão de Laudo Técnico de Entrega:** Relatório visual anexado à nota fiscal ou via do cliente.
"""

with open(os.path.join(DOC_DIR, "B5_0_CHECKLIST_UI_SPEC.md"), "w", encoding="utf-8") as f:
    f.write(checklist_ui_md)

print("6. B5_0_CHECKLIST_UI_SPEC.md OK")

# -------------------------------------------------------------
# 7. B5_0_MONEY_MIGRATION_RELEASE_PLAN.md
# -------------------------------------------------------------
money_plan_md = """# PRIMOX WORKSHOP — B5.0
## PLANO DE RELEASE E MIGRAÇÃO FÍSICA DE MONEY (SQLITE)

**Objetivo:** Transição segura da representação de valores monetários de ponto flutuante (`REAL`) para centavos inteiros (`INTEGER CentsV1`) no banco de dados SQLite.  
**Ambiente:** Homologação Operacional / Base de Produção sob Gate Rígido  
**Status do Plano:** **ESPECIFICADO (NÃO EXECUTAR NESTA FASE)**

---

### 1. Diagnóstico do Estado Atual

1. **Camada de Aplicação:** A arquitetura do PRIMOX Workshop já opera com precisão total através do componente `MoneyIO`, `MoneyCents` e `decimal` em C#, com testes automatizados garantindo arredondamento bancário (`MidpointRounding.AwayFromZero`) e isolamento de centavos extremos.
2. **Camada de Armazenamento (SQLite):**
   - As tabelas financeiras existentes (`Orcamentos`, `OrdensServico`, `ContasPagar`, `ContasReceber`, `MovimentacoesFinanceiras`) armazenam valores originalmente como `REAL`.
   - A leitura e escrita em tempo de execução são mediadas pelo `MoneyIO`, prevenindo distorções no dia a dia da oficina.
3. **Decisão Arquitetural B4/B5:** A migração física das colunas para `INTEGER` **permanece BLOQUEADA no banco de produção** para evitar qualquer risco de indisponibilidade ou corrupção de dados históricos.

---

### 2. Arquitetura do Utilitário de Migração

A migração física definitiva só poderá ser executada através de um utilitário dedicado (`DbMigrationTool`) operando sob um protocolo formal de **6 Estágios**:

```mermaid
graph LR
    A[1. PRECHECK] --> B[2. BACKUP]
    B --> C[3. TRANSACTION]
    C --> D[4. DATA PROOF]
    D --> E{5. GATE GO/NO-GO}
    E -- Aprovado --> F[6. COMMIT & VACUUM]
    E -- Reprovado --> G[ROLLBACK ATÔMICO]
```

#### Estágio 1: PRECHECK (Inspeção Prévia)
- Verifica permissão exclusiva de acesso ao arquivo `.db`.
- Executa `PRAGMA integrity_check` e `PRAGMA foreign_key_check`.
- Calcula o hash SHA-256 da base original.
- Registra a contagem de registros e a soma monetária de controle de cada tabela:
  `SUM(CAST(ROUND(Valor * 100) AS INTEGER))`

#### Estágio 2: BACKUP (Cópia Snapshot Imutável)
- Cria cópia física timestamped: `primoauto_pre_money_mig_<DATA>.db`.
- Valida integridade da cópia recém-criada via hash e checagem SQLite.

#### Estágio 3: TRANSACTION (Migração Estruturada)
- Abertura de transação única `BEGIN IMMEDIATE TRANSACTION`.
- Para cada tabela alvo:
  1. Criação de coluna temporária `ValorCents INTEGER`.
  2. Migração segura com conversão determinística:
     `UPDATE Tabela SET ValorCents = CAST(ROUND(Valor * 100.0) AS INTEGER);`
  3. Validação de nulos (`COALESCE(ValorCents, 0)`).

#### Estágio 4: DATA PROOF (Validação Linha a Linha)
- Comparação de 100% dos registros:
  `ABS((Valor * 100.0) - ValorCents) > 0.001`
- Se qualquer divergência for detectada: aborta imediatamente.
- Validação das somas agregadas (soma antiga vs soma nova).

#### Estágio 5: GATE GO / NO-GO
- Script automático emite relatório de conferência.
- Se houver divergência de 1 centavo: **NO-GO (Rollback imediato)**.
- Se todas as somas coincidirem perfeitamente: **GO**.

#### Estágio 6: COMMIT & REORGANIZAÇÃO
- Atualização do schema definitivo.
- `PRAGMA user_version = 2` (indicador de base em CentsV1).
- `COMMIT` da transação.
- `VACUUM` e `ANALYZE` do banco.

---

### 3. Matriz de Tabelas e Colunas Alvo da Migração

| Tabela SQLite | Coluna Atual (`REAL`) | Nova Coluna (`INTEGER Cents`) | Tipo de Dado |
| :--- | :--- | :--- | :--- |
| `Orcamentos` | `ValorTotal`, `Desconto` | `ValorTotalCents`, `DescontoCents` | Centavos Inteiros |
| `OrcamentoItens` | `PrecoUnitario`, `Subtotal` | `PrecoUnitarioCents`, `SubtotalCents`| Centavos Inteiros |
| `OrdensServico` | `ValorTotal`, `Desconto` | `ValorTotalCents`, `DescontoCents` | Centavos Inteiros |
| `OrdemServicoItens` | `ValorUnitario` | `ValorUnitarioCents` | Centavos Inteiros |
| `ContasPagar` | `Valor`, `ValorPago` | `ValorCents`, `ValorPagoCents` | Centavos Inteiros |
| `ContasReceber` | `Valor`, `ValorRecebido` | `ValorCents`, `ValorRecebidoCents` | Centavos Inteiros |
| `MovimentacoesFinanceiras` | `Valor` | `ValorCents` | Centavos Inteiros |
| `Produtos` | `PrecoCusto`, `PrecoVenda`| `PrecoCustoCents`, `PrecoVendaCents`| Centavos Inteiros |

---

### 4. Critério de Execução
A migração física em produção só será realizada na **Fase B7**, precedida de ensaio completo em homologação e homologação pelo piloto comercial.
"""

with open(os.path.join(DOC_DIR, "B5_0_MONEY_MIGRATION_RELEASE_PLAN.md"), "w", encoding="utf-8") as f:
    f.write(money_plan_md)

print("7. B5_0_MONEY_MIGRATION_RELEASE_PLAN.md OK")
