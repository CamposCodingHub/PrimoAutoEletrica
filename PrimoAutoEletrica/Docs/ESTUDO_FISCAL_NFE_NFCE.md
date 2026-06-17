# Estudo Fiscal - NFe e NFCe - Primo Auto Elétrica

## Visão Geral

Este documento estuda os requisitos fiscais para emissão de Nota Fiscal Eletrônica (NFe) e Nota Fiscal do Consumidor Eletrônica (NFCe) no contexto do Primo Auto Elétrica.

## Contexto

O Primo Auto Elétrica é um sistema ERP para oficinas elétricas automotivas. As operações fiscais típicas incluem:

- Venda de peças e produtos
- Prestação de serviços
- Compra de insumos
- Importação de NF-e de fornecedores

## Classificação Fiscal

### Natureza da Operação

A oficina pode ser classificada como:
- **Comércio:** Venda de produtos
- **Serviço:** Prestação de serviços
- **Misto:** Ambos (mais comum)

### Regime Tributário

- **Simples Nacional:** Regime simplificado para pequenas empresas
- **Lucro Presumido:** Regime com base em presunção de lucro
- **Lucro Real:** Regime com base em lucro contábil real

## NFe - Nota Fiscal Eletrônica

### Quando Usar

- Venda de produtos para outras empresas (B2B)
- Venda de produtos com valor acima de R$ 10.000,00
- Exportação de produtos
- Remessa para conserto

### Requisitos Técnicos

- Certificado digital A1 ou A3
- Software emissor homologado
- Conexão com SEFAZ
- Schema XML da NFe

### Campos Obrigatórios

- Dados do emitente (oficina)
- Dados do destinatário (cliente)
- Itens da nota (produtos/serviços)
- Impostos (ICMS, IPI, PIS, COFINS)
- Totais
- Dados de transporte
- Assinatura digital

### Integração com Primo Auto Elétrica

**Fluxo:**
1. Usuário seleciona itens no PDV
2. Sistema gera XML da NFe
3. Sistema assina XML com certificado digital
4. Sistema envia para SEFAZ
5. Sistema recebe autorização
6. Sistema imprime DANFE
7. Sistema salva XML localmente

## NFCe - Nota Fiscal do Consumidor Eletrônica

### Quando Usar

- Venda de produtos para consumidor final (B2C)
- Venda de produtos com valor até R$ 10.000,00
- Venda de produtos no balcão

### Requisitos Técnicos

- Certificado digital A1 ou A3
- Software emissor homologado
- Conexão com SEFAZ
- Schema XML da NFCe

### Campos Obrigatórios

- Dados do emitente (oficina)
- Dados do consumidor (opcional para valores até R$ 200,00)
- Itens da nota
- Impostos
- Totais
- Forma de pagamento
- Assinatura digital

### Integração com Primo Auto Elétrica

**Fluxo:**
1. Usuário seleciona itens no PDV
2. Sistema gera XML da NFCe
3. Sistema assina XML
4. Sistema envia para SEFAZ
5. Sistema recebe autorização
6. Sistema imprime DANFCe
7. Sistema envia por e-mail (se solicitado)

## NFS-e - Nota Fiscal de Serviços Eletrônica

### Quando Usar

- Prestação de serviços
- Mão de obra
- Serviços de manutenção

### Requisitos

Depende do município:
- RPS (Recibo Provisório de Serviços)
- NFS-e municipal
- NFS-e estadual (alguns estados)

## Implementação no Primo Auto Elétrica

### Fase 1: Preparação

- [ ] Definir regime tributário da oficina
- [ ] Obter certificado digital
- [ ] Cadastrar dados fiscais da empresa
- [ ] Definir CFOPs (Código Fiscal de Operações)
- [ ] Definir NCMs (Nomenclatura Comum do Mercosul)

### Fase 2: Importação

- [ ] Implementar importador de XML
- [ ] Validar schema XML
- [ ] Extrair dados do XML
- [ ] Atualizar estoque automaticamente

### Fase 3: Emissão

- [ ] Implementar gerador de XML
- [ ] Implementar assinatura digital
- [ ] Implementar envio para SEFAZ
- [ ] Implementar consulta de status
- [ ] Implementar impressão de DANFE/DANFCe

### Fase 4: Cancelamento

- [ ] Implementar cancelamento de NFe/NFCe
- [ ] Implementar carta de correção
- [ ] Implementar inutilização de numeração

### Fase 5: Consulta

- [ ] Implementar consulta de chave de acesso
- [ ] Implementar download de XML
- [ ] Implementar histórico de notas

## Custos

### Certificado Digital

- A1 (cartão/token): R$ 100-200/ano
- A3 (software): R$ 200-400/ano

### Software Emissor

- Licença anual: R$ 500-2.000/ano
- Ou uso de serviço web: por nota emitida

### Homologação

- Ambiente de homologação SEFAZ: gratuito
- Testes obrigatórios

## Legislação

### Leis Relevantes

- Lei nº 12.741/2012 (NFCe)
- Lei nº 8.112/1990 (NFe)
- Convênio ICMS 142/2018
- Ajuste SINIEF 07/2016

### Normas Técnicas

- NT 2015.002 (NFe 4.0)
- NT 2016.002 (NFCe 4.0)
- Manual de Orientação do Contribuinte

## Riscos e Considerações

### Riscos

- Falha de conexão com SEFAZ
- Rejeição da nota
- Erro no cálculo de impostos
- Perda do certificado digital
- Multas por emissão incorreta

### Considerações

- Necessidade de treinamento da equipe
- Manutenção do certificado digital
- Backup dos XMLs emitidos
- Contingência em caso de falha

## Recomendações

### Para Implementação

1. Começar com importação de XML (menor complexidade)
2. Implementar emissão de NFCe (mais simples que NFe)
3. Implementar emissão de NFe após NFCe estar estável
4. Usar biblioteca homologada (ex: NFe.Classes)
5. Testar extensivamente em ambiente de homologação

### Para Operação

1. Manter certificado digital atualizado
2. Fazer backup regular dos XMLs
3. Treinar equipe em contingência
4. Monitorar status de notas emitidas
5. Manter documentação atualizada

## Próximos Passos

1. Consultar contador para definir regime tributário
2. Obter certificado digital
3. Escolher software emissor ou desenvolver próprio
4. Implementar importação de XML
5. Implementar emissão de NFCe
6. Implementar emissão de NFe
7. Testar em homologação
8. Treinar equipe
9. Iniciar operação em produção

## Referências

- Portal da NFe: http://www.nfe.fazenda.gov.br
- Portal da NFCe: http://www.nfce.fazenda.gov.br
- SEFAZ do seu estado
- Receita Federal

---

**Versão:** 1.0
**Última atualização:** 2026-06-17
**Status:** Estudo preliminar - Revisão por contador necessária
