# Manual de Operação — Central Fiscal (NFS-e de Serviços & NFC-e de Peças)

**Módulo:** Central Fiscal & Gateway Eletrônico  
**Versão:** 2.1 Enterprise  
**Objetivo:** Emissão, transmissão, cancelamento e consulta de documentos fiscais eletrônicos integrados.

---

## 1. Visão Geral

O PRIMOX Workshop conta com arquitetura de Gateway Fiscal plugável que permite emitir:
* **NFS-e (Nota Fiscal de Serviços Eletrônica):** Tributação municipal sobre a mão de obra aplicada nas Ordens de Serviço;
* **NFC-e (Nota Fiscal de Consumidor Eletrônica):** Tributação estadual (ICMS) sobre a venda de peças e acessórios em balcão ou aplicados na OS;
* **Importação de NF-e:** Entrada automática de XMLs de notas emitidas por fornecedores.

---

## 2. Configuração de Certificado Digital

1. Acesse `Configurações → Parâmetros Fiscais`;
2. Carregue o arquivo do **Certificado Digital A1 (.pfx)**;
3. Digite a senha do certificado e clique em **"Validar Certificado"**;
4. Preencha os parâmetros tributários:
   * Regime Tributário (Simples Nacional, Lucro Presumido ou Lucro Real);
   * Código de Tributação Municipal de Serviços (ISSQN);
   * CSC (Código de Segurança do Contribuinte) e IdToken para NFC-e;
   * Ambiente (Homologação para testes ou Produção com validade jurídica).

---

## 3. Emissão de NFS-e (Mão de Obra da Oficina)

1. Na conclusão da Ordem de Serviço ou na `Central Fiscal`:
2. Selecione a OS com mão de obra apontada;
3. Clique em **"Emitir NFS-e"**;
4. O sistema gera o XML no layout padrão ABRASF / Provedor Municipal, assina digitalmente com o certificado A1 e transmite para a Prefeitura;
5. Ao receber a autorização, o sistema imprime o RPS/DANFSE ou envia o link para o e-mail/WhatsApp do cliente.

---

## 4. Emissão de NFC-e (Venda de Peças no Balcão)

1. Ao finalizar uma venda de peças no PDV ou fechar uma OS com autopeças:
2. Selecione a opção **"Emitir Cupom Fiscal (NFC-e)"**;
3. O sistema valida os códigos NCM e alíquotas tributárias das peças cadastradas;
4. A nota é transmitida diretamente à SEFAZ estadual;
5. O comprovante com o **QR Code** de consulta é impresso na impressora térmica não-fiscal de balcão (58mm ou 80mm).

---

## 5. Contingência e Tratamento de Erros

* Caso o webservice da SEFAZ ou da Prefeitura fique temporariamente indisponível, a nota é gravada em **Contingência Offline**;
* O cliente recebe o cupom com indicação de contingência;
* Assim que a comunicação for restabelecida, a Central Fiscal retransmite as notas pendentes automaticamente.
