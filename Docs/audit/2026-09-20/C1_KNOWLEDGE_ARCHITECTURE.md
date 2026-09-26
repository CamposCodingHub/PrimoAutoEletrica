# PRIMOX WORKSHOP — CICLO C1
# ARQUITETURA DA BASE DE CONHECIMENTO TÉCNICO (C1.3)
**Data:** 2026-09-25  
**Módulo:** PRIMOX Knowledge — Base de Conhecimento Estruturado e Casos Reais  
**Status Arquitetural:** IMPLEMENTATION SPEC  

---

## 1. VISÃO GERAL & PRINCÍPIO DE DOMÍNIO

Uma oficina automotiva avançada não se destaca apenas por substituir peças, mas por **saber diagnosticar com precisão técnica comprovável**.
O maior ativo intangível de uma oficina de auto elétrica é o conhecimento acumulado de seus especialistas:
- O que aconteceu (Sintoma relatado e observado);
- O que foi medido (Grandezas elétricas: tensão, corrente, queda de tensão, oscilograma, ripple);
- O que foi diagnosticado (Causa raiz comprovada por teste);
- O que foi feito (Solução aplicada e peças substituídas);
- Qual foi o resultado (Validação do funcionamento pós-reparo).

Antes de qualquer inteligência artificial ser inserida na oficina, o software deve **estruturar os dados da realidade física**. Sem dados estruturados de qualidade, qualquer IA seria uma fonte de alucinação e prejuízo para a oficina.

---

## 2. MODELO DE DOMÍNIO & ENTIDADES

### 2.1 Entidade `TechnicalKnowledgeEntry` (Artigo da Base Técnica)
- `KnowledgeId` (`Guid`): Identificador único global.
- `Code` (`string`): Código de referência técnica (ex: `KB-ELET-001`, `KB-CAN-024`).
- `Title` (`string`): Título claro do boletim ou procedimento técnico.
- `System` (`string`): Subsistema automotivo (`Alimentação e Bateria`, `Partida e Arranque`, `Carga e Alternador`, `Rede CAN e Comunicação`, `Injeção Eletrônica`, `Iluminação e Sinalização`, `Climatização e HVAC`, `Arrefecimento Elétrico`, `Chassi e Conforto`).
- `VehicleCategory` (`string`): Aplicação principal (`Linha Leve`, `Linha Pesada`, `Utilitários`, `Agrícola`, `Universal`).
- `Voltage` (`string`): Tensão nominal de trabalho (`12V`, `24V`, `Bivolt`, `Alta Tensão/Híbrido`).
- `Symptom` (`string`): Descrição detalhada do sintoma apresentado.
- `PossibleCauses` (`string`): Hipóteses e causas prováveis conhecidas.
- `DiagnosticProcedure` (`string`): Roteiro de testes passo a passo.
- `RecommendedMeasurements` (`string`): Medições mandatórias esperadas (ex: *Queda máxima admissível no cabo positivo de partida: 0,5V sob carga de 250A*).
- `Solution` (`string`): Procedimento corretivo comprovado.
- `Warnings` (`string?`): Alertas de segurança operacional (ex: risco de curto, arco voltaico, danos à ECU).
- `Tags` (`string`): Palavras-chave para busca textual e indexação RAG (ex: `alternador,queda-tensao,d01,bateria,24v,scania`).
- `SourceType` (`KnowledgeSourceType`): `FIELD_EXPERIENCE`, `OEM_MANUAL`, `DIAGNOSTIC_CASE`, `AUTO_ELETRICA_TECNICA`.
- `CreatedByUserId` (`int`): Técnico/Engenheiro autor (`Funcionarios.Id`).
- `CreatedByUserName` (`string`): Nome do autor.
- `Status` (`KnowledgeStatus`): `DRAFT`, `PUBLISHED`, `ARCHIVED`.
- `CreatedAt` (`DateTime`): Timestamp de criação.
- `UpdatedAt` (`DateTime`): Timestamp de atualização.

### 2.2 Entidade `DiagnosticCase` (Caso Real de Diagnóstico em Operação)
O registro de caso real captura a verdade do pátio quando um serviço é concluído com sucesso:
- `CaseId` (`Guid`): Identificador único do caso.
- `Code` (`string`): Código legível do caso (ex: `CASO-20260925-001`).
- `Title` (`string`): Resumo executivo do caso (ex: `Actros 2651 falha intermitente de carga em 24V`).
- `VehicleId` (`Guid?`): Vínculo com o cadastro do veículo (`Veiculos.Id`).
- `VehicleModel` (`string`): Modelo e motorização do veículo.
- `VehiclePlate` (`string`): Placa do veículo.
- `WorkOrderId` (`Guid?`): Vínculo com a Ordem de Serviço concluída (`OrdensServico.Id`).
- `WorkOrderNumber` (`string?`): Número da OS.
- `TechnicianId` (`int?`): Técnico responsável pela solução (`Funcionarios.Id`).
- `TechnicianName` (`string`): Nome do técnico.
- `System` (`string`): Sistema elétrico analisado.
- `Voltage` (`string`): Tensão do sistema (`12V` ou `24V`).
- `DtcCodes` (`string?`): Códigos de falha registrados pelo scanner (ex: `P0562, U0100`).
- `Symptom` (`string`): Sintoma reportado pelo cliente / constatado.
- `Measurements` (`string`): Medições reais efetuadas (ex: *Tensão de repouso: 24,2V; Partida: 18,1V; Tensão no alternador B+: 28,4V; Tensão na bateria: 26,1V -> Queda de 2,3V no chicote positivo*).
- `InitialHypotheses` (`string?`): Hipóteses iniciais formuladas.
- `ConfirmedCause` (`string`): Causa raiz identificada conclusivamente (ex: *Terminal do cabo B+ oxidado e frouxo no distribuidor de carga*).
- `Solution` (`string`): Ação corretiva realizada (ex: *Substituição do terminal olha 50mm² e limpeza da base de contato*).
- `PartsUsed` (`string?`): Peças e insumos aplicados.
- `TestResult` (`string`): Medição de confirmação pós-reparo (ex: *Queda de tensão reduzida para 0,18V sob carga máxima de faróis e ar-condicionado*).
- `FinalResult` (`DiagnosticCaseResult`): `RESOLVED`, `PARTIAL`, `UNRESOLVED`.
- `KnowledgeEntryId` (`Guid?`): Vínculo com o artigo oficial na base técnica (quando promovido a boletim).
- `CreatedAt` (`DateTime`): Timestamp de auditoria.
- `UpdatedAt` (`DateTime`): Timestamp de auditoria.

---

## 3. INTEGRAÇÃO COM OS DEMAIS MÓDULOS

### 3.1 Preservação e Vínculo com Auto Elétrica Técnica (D01-D06 e D07-D17)
- O módulo existente de **Auto Elétrica Técnica** (`AutoEletricaTecnicaService`, `AutoEletricaTecnicaControl`) possui a matriz de roteiros D01 a D06 e o roadmap D07 a D17.
- **NÃO DUPLICAR:** O conhecimento técnico faz referência direta aos roteiros D01 a D06 (ex: *D01 - Balanço Energético*, *D02 - Teste de Queda de Tensão*, etc.).
- Os casos operacionais podem ser gerados diretamente a partir dos formulários de medição técnica.

### 3.2 Integração com Ordem de Serviço (OS)
- Ao finalizar uma OS com serviços e apontamentos técnicos elétricos, o PRIMOX disponibiliza a ação:
  `"Salvar como caso técnico"`
- O sistema pré-preenche automaticamente: Veículo, Placa, Cliente, Técnico, Peças Utilizadas e Diagnóstico, eliminando redigitação e enriquecendo o patrimônio cognitivo da oficina.

### 3.3 Integração com Vehicle360 & Client360
- O `Veiculo360Snapshot` já possui a coleção estruturada `Diagnosticos`.
- O histórico de casos técnicos do veículo é exibido na aba de diagnósticos do **Vehicle 360**, permitindo ao mecânico identificar falhas crônicas, recorrências sazonais e o histórico completo de intervenções elétricas daquela placa.

---

## 4. DESIGN SYSTEM & INTERFACE

- **BaseConhecimentoControl:**
  - Busca textual com filtro por Sistema, Categoria de Veículo, Tensão (12V/24V) e Tags.
  - Abas limpas: *Boletins e Artigos Técnicos* e *Casos Reais de Diagnóstico*.
  - Visualização de artigo técnico em layout moderno de ficha técnica (semelhante a manuais de fábrica).
  - Modal de registro e edição de caso técnico alinhado ao padrão `Modal.xaml` e temas Light/Dark.
