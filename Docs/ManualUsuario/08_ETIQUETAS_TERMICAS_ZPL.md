# Manual de Operação — Impressão Térmica de Etiquetas ZPL II

**Módulo:** Etiquetas Térmicas ZPL II  
**Versão:** 2.1 Enterprise  
**Objetivo:** Geração e impressão direta em impressoras térmicas industriais (Zebra, Argox, Elgin) para identificação de chaves, prateleiras e ferramentas.

---

## 1. Visão Geral

O PRIMOX Workshop possui um motor de geração nativa de linguagem **ZPL II (Zebra Programming Language)**. Ao invés de usar drivers lentos do Windows que geram imagens pesadas, o sistema envia comandos ZPL puros via rede ou USB, garantindo:
* Impressão ultra-rápida (menos de 1 segundo);
* Código de barras Code 128 e QR Codes com 100% de precisão de leitura;
* Resistência a graxa, óleo automotivo e calor.

---

## 2. Configuração da Impressora Térmica

1. Acesse `Configurações → Impressoras Térmicas`;
2. Selecione o modelo conectado:
   * **Zebra:** ZD220, GC420t, GK420t, ZD421;
   * **Argox:** OS-214 Plus / OS-2140;
   * **Elgin:** L42 Pro / L42;
3. Escolha o tipo de conexão: **USB Direto (Raw Spooler)** ou **Rede TCP/IP** (informando IP e porta, ex.: `192.168.1.150:9100`);
4. Clique em **"Testar Impressão de Calibração"**.

---

## 3. Modelos de Etiquetas Disponíveis

### A. Etiqueta de Chave do Carro (Recepção)
* **Finalidade:** Identificar a chave do veículo no quadro de chaves da oficina assim que o cliente chega;
* **Conteúdo:** Placa do veículo, Número da Ordem de Serviço, Nome do Cliente, Data de Entrada e Código de Barras;
* **Como Imprimir:** Ao salvar o orçamento ou abrir a OS, clique no ícone de **"Imprimir Etiqueta de Chave"**.

### B. Etiqueta de Prateleira (Almoxarifado)
* **Finalidade:** Endereçamento físico de peças no estoque (Rua, Prateleira, Gaveta);
* **Conteúdo:** Código interno do produto, descrição da peça, localização física e código de barras Code 128;
* **Como Imprimir:** Acesse `Estoque → Selecione a Peça → Imprimir Etiqueta de Gôndola`.

### C. Etiqueta de Ferramenta Especial (Ferramentaria 4.0)
* **Finalidade:** Identificação de instrumentos de alto valor (scanners, osciloscópios, alicates);
* **Conteúdo:** Nome do equipamento, número de patrimônio, data de validade da calibração e QR Code;
* **Como Imprimir:** Acesse `Ferramentaria 4.0 → Selecione o Aparelho → Imprimir Etiqueta de Ativo`.
