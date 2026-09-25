# PRIMOX WORKSHOP — FASE B7: GATE 11
# FISCAL OFFICIAL SOURCES & REGULATORY MAPPING
**Data da Auditoria:** 2026-09-25  
**Fase:** B7 — Production Money Migration + Fiscal/SEFAZ Homologation  
**Responsável Técnico:** Antigravity Autonomous Audit Engine  
**Status do Gate:** PASS  

---

## 1. OBJETIVO DO GATE 11

Mapear exaustivamente todas as fontes regulatórias oficiais brasileiras, manuais técnicos, notas técnicas (NT), schemas XML e endpoints governamentais (SEFAZ SP e Ambiente Nacional) que regem a emissão de Nota Fiscal Eletrônica (NF-e modelo 55) e Nota Fiscal de Consumidor Eletrônica (NFC-e modelo 65) no âmbito do PRIMOX Workshop.

O sistema PRIMOX opera predominantemente no regime do **Simples Nacional (CRT 1)** para oficinas mecânicas e autoelétricas (CNAE 4520-0/01 e 4520-0/07), emitindo operações de venda no balcão e saídas de peças e serviços aplicados em Ordens de Serviço.

---

## 2. NORMAS E MANUAIS REGULATÓRIOS OFICIAIS

| Código / Norma | Título / Descrição | Órgão Emissor | Relevância para o PRIMOX |
|---|---|---|---|
| **MOC 7.0** | Manual de Orientação do Contribuinte (Visão Geral, Anexo I - Leiaute e Regras de Validação) | ENCAT / CONFAZ / RFB | Define o leiaute 4.00, campos, tamanhos, regras de negócio e semântica de todos os nós XML (`<ide>`, `<emit>`, `<dest>`, `<det>`, `<total>`, `<pag>`). |
| **Ajuste SINIEF 07/05** | Institui a Nota Fiscal Eletrônica (NF-e modelo 55) e o Documento Auxiliar da Nota Fiscal Eletrônica (DANFE) | CONFAZ | Norma legal base para emissão de NF-e mercantil e garantia de autenticidade jurídica. |
| **Ajuste SINIEF 19/16** | Institui a Nota Fiscal de Consumidor Eletrônica (NFC-e modelo 65) e o Documento Auxiliar da NFC-e (DANFE-NFC-e) | CONFAZ | Regulamentação do varejo/balcão para emissão ao consumidor final com QRCode. |
| **Resolução CGSN nº 140/2018** | Regulamento do Simples Nacional | Comitê Gestor do Simples Nacional | Determina regras de tributação por CSOSN (Código de Situação da Operação do Simples Nacional: 102, 103, 300, 400, 500). |
| **NT 2020.006** | Intermediador da Operação e Detalhamento de Pagamentos | ENCAT | Torna obrigatório o preenchimento do grupo `<pag>` (`<detPag>`), indicando meio de pagamento (Dinheiro, PIX, Cartão Débito/Crédito) e intermediador (`indicadorIntermediador`). |
| **NT 2023.001** | Tributação Monofásica sobre Combustíveis e Regras de Validação Gerais | ENCAT | Atualizações gerais de RVs e alíquotas ad rem/ad valorem. |
| **NT 2021.004** | Validação de GTIN/EAN | ENCAT | Regras de validação do código de barras comercial (EAN-13) x Cadastro Centralizado de GTIN (CCG). |
| **NT 2019.001** | Regras de Validação Gerais e Segurança | ENCAT | Rejeições por QR Code inválido, divergência de chave, timeout síncrono/assíncrono. |

---

## 3. ESQUEMAS XML OFICIAIS (XSD)

O PRIMOX valida internamente e através de provider os documentos contra o pacote de esquemas oficial **PL_009k** (NF-e Layout 4.00):

| Esquema XSD | Função | Validação PRIMOX |
|---|---|---|
| `nfe_v4.00.xsd` | Schema mestre da NF-e / NFC-e | Validação de nós, tipos de dados, tamanhos de string e formatos numéricos (`TDec_1302`, `TDec_1104v`). |
| `leiauteNFe_v4.00.xsd` | Definição das estruturas completas de tags e elementos filhos | Validação hierárquica (`<infNFe>`, `<ide>`, `<emit>`, `<dest>`, `<det>`, `<imposto>`, `<total>`, `<pag>`). |
| `tiposBasico_v1.03.xsd` | Tipos primitivos (CNPJ, CPF, UF, Chave de Acesso de 44 dígitos) | Validação rigorosa de máscaras e dígitos verificadores. |
| `consStatServ_v4.00.xsd` | Consulta de status de serviço SEFAZ | Utilizado para verificar liveness da SEFAZ antes de emissões em lote. |
| `envEventoCancNFe_v1.00.xsd` | Pedido de cancelamento de NF-e | Evento homologado para cancelamento em até 24 horas. |
| `inutNFe_v4.00.xsd` | Pedido de inutilização de numeração | Evento para quebra de sequência numérica. |

---

## 4. ENDPOINTS SEFAZ E GATEWAY DE HOMOLOGAÇÃO / PRODUÇÃO

### 4.1. SEFAZ São Paulo (SP) — Endpoints Nativos SOAP 1.2
Estado de domicílio fiscal primário do cliente de referência:

| Serviço SEFAZ SP | Ambiente de Homologação | Ambiente de Produção |
|---|---|---|
| **NFeAutorizacao4** | `https://homologacao.nfe.fazenda.sp.gov.br/ws/nfeautorizacao4.asmx` | `https://nfe.fazenda.sp.gov.br/ws/nfeautorizacao4.asmx` |
| **NFeRetAutorizacao4** | `https://homologacao.nfe.fazenda.sp.gov.br/ws/nferetautorizacao4.asmx` | `https://nfe.fazenda.sp.gov.br/ws/nferetautorizacao4.asmx` |
| **NFeStatusServico4** | `https://homologacao.nfe.fazenda.sp.gov.br/ws/nfestatusservico4.asmx` | `https://nfe.fazenda.sp.gov.br/ws/nfestatusservico4.asmx` |
| **NFeRecepcaoEvento4** | `https://homologacao.nfe.fazenda.sp.gov.br/ws/nferecepcaoevento4.asmx` | `https://nfe.fazenda.sp.gov.br/ws/nferecepcaoevento4.asmx` |
| **NFeInutilizacao4** | `https://homologacao.nfe.fazenda.sp.gov.br/ws/nfeinutilizacao4.asmx` | `https://nfe.fazenda.sp.gov.br/ws/nfeinutilizacao4.asmx` |

### 4.2. Gateway REST Integrado (Focus NFe API v2)
O PRIMOX utiliza como provider homologado a API Focus NFe para abstração do protocolo SOAP e gestão de contingência:

| Componente | Homologação (Sandbox) | Produção (Live) |
|---|---|---|
| **Base URL** | `https://homologacao.focusnfe.com.br/v2/` | `https://api.focusnfe.com.br/v2/` |
| **Emissão NF-e** | `POST /v2/nfe?ref={idempotencyKey}` | `POST /v2/nfe?ref={idempotencyKey}` *(Bloqueado por Guard)* |
| **Consulta NF-e** | `GET /v2/nfe/{reference_or_id}` | `GET /v2/nfe/{reference_or_id}` |
| **Cancelamento** | `DELETE /v2/nfe/{reference_or_id}` | `DELETE /v2/nfe/{reference_or_id}` |
| **DANFE PDF** | `GET /v2/nfe/{id}.pdf` | `GET /v2/nfe/{id}.pdf` |
| **XML Autorizado** | `GET /v2/nfe/{id}.xml` | `GET /v2/nfe/{id}.xml` |

---

## 5. REGRAS DE VALIDAÇÃO (RV) CRÍTICAS AUDITADAS NO PRIMOX

| Regra SEFAZ | Descrição da Regra | Implementação / Mitigação no PRIMOX |
|---|---|---|
| **RV E01-10** | CNPJ do emitente deve constar no cadastro da SEFAZ do estado emissor. | O sistema valida o CNPJ cadastrado nas configurações fiscais e impede emissão com CNPJ inválido ou ausente. |
| **RV E03a-30** | Assinatura digital do XML (X.509 v3 / SHA-256) deve pertencer à autoridade certificadora válida da ICP-Brasil. | Assinatura gerenciada com garantia de compatibilidade com certificados A1 (.pfx). |
| **RV I08-10** | Código NCM deve conter exatamente 8 dígitos numéricos válidos na Tabela NCM vigente da RFB. | `FiscalDocumentValidator` bloqueia expressamente NCM em branco, nulo ou fictício (`00000000`), sem inventar códigos (`Validator_BloqueiaSemNcm_SemInventar`). |
| **RV W16-10** | Total da NF-e (`vNF`) deve ser igual ao somatório dos itens menos descontos mais acréscimos. | Matematicamente verificado em centavos com fórmula de distribuição de desconto centavo a centavo (`diff = 0.00`). |
| **RV YA01-10** | Obrigatório informar pelo menos uma forma de pagamento no grupo `detPag`. | Mapeador fiscal preenche obrigatoriamente `tPag` (01=Dinheiro, 03=Cartão Crédito, 04=Cartão Débito, 17=PIX, 99=Outros). |
| **RV B03-10** | Número e série da nota fiscal devem ser estritamente sequenciais por série. | Garantido por armazenamento seguro e trava de sequência atômica no banco de dados. |

---

## 6. CONCLUSÃO DO GATE 11

As fontes oficiais, notas técnicas e regras de validação estão formalmente mapeadas, estruturadas e integradas à arquitetura do PRIMOX Workshop.

**Resultado do Gate 11:** **PASS**
