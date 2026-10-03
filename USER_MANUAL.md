# Manual Completo do Usuário — PRIMOX Workshop Enterprise v2.1

**Sistema Integrado de Gestão para Auto Elétricas, Centros Automotivos e Frotas**  
**Versão:** 2.1.0 Enterprise  
**Copyright (c) 2026 CamposCodingHub.** Todos os direitos reservados.  
**Aviso:** Software proprietário — Proibida a comercialização por terceiros.

---

## Sumário
1. [Visão Geral e Filosofia Operacional](#1-visão-geral-e-filosofia-operacional)
2. [A Regra de Ouro da Oficina (Fluxo Mestre)](#2-a-regra-de-ouro-da-oficina-fluxo-mestre)
3. [Guia Módulo a Módulo](#3-guia-módulo-a-módulo)
   - 3.1. [Dashboard Executivo](#31-dashboard-executivo)
   - 3.2. [Cadastros Base (Clientes e Veículos)](#32-cadastros-base-clientes-e-veículos)
   - 3.3. [Orçamentos Comerciais](#33-orçamentos-comerciais)
   - 3.4. [Ordens de Serviço e Laudo Técnico](#34-ordens-de-serviço-e-laudo-técnico)
   - 3.5. [Kanban da Oficina](#35-kanban-da-oficina)
   - 3.6. [PDV / Frente de Caixa](#36-pdv--frente-de-caixa)
   - 3.7. [Auto Elétrica Técnica (Medições e Ripple)](#37-auto-elétrica-técnica-medições-e-ripple)
   - 3.8. [Copilot de IA Dual-Engine](#38-copilot-de-ia-dual-engine)
   - 3.9. [Controle de Ferramentaria 4.0](#39-controle-de-ferramentaria-40)
   - 3.10. [Compras Anti-Ruptura e Ponto de Pedido (ROP)](#310-compras-anti-ruptura-e-ponto-de-pedido-rop)
   - 3.11. [Estoque e Curva ABC](#311-estoque-e-curva-abc)
   - 3.12. [Transferências Inter-Filiais ACID](#312-transferências-inter-filiais-acid)
   - 3.13. [Gestão de Frotas B2B e Faturamento Periódico](#313-gestão-de-frotas-b2b-e-faturamento-periódico)
   - 3.14. [Central Fiscal (NFS-e e NFC-e)](#314-central-fiscal-nfs-e-e-nfc-e)
   - 3.15. [Impressão Térmica de Etiquetas ZPL II](#315-impressão-térmica-de-etiquetas-zpl-ii)
   - 3.16. [Financeiro e Fluxo de Caixa Projetado](#316-financeiro-e-fluxo-de-caixa-projetado)
   - 3.17. [Relatórios e Business Intelligence](#317-relatórios-e-business-intelligence)
   - 3.18. [Multi-Filiais Corporativo](#318-multi-filiais-corporativo)
   - 3.19. [Licença HMAC e Segurança](#319-licença-hmac-e-segurança)
   - 3.20. [Configurações e Backup Automático](#320-configurações-e-backup-automático)
4. [Guia de Rotinas por Cargo (Do Caixa ao CEO)](#4-guia-de-rotinas-por-cargo-do-caixa-ao-ceo)
5. [Atalhos de Teclado](#5-atalhos-de-teclado)
6. [Erros Comuns e Como Evitar](#6-erros-comuns-e-como-evitar)

---

## 1. Visão Geral e Filosofia Operacional

O **PRIMOX Workshop** foi projetado com uma arquitetura **Offline-First Soberana**. Diferente de sistemas web comuns que travam ou ficam lentos quando a internet da oficina oscila, o PRIMOX executa localmente com velocidade instantânea, garantindo:
* **Zero Fila no Balcão:** Vendas no PDV e recebimentos de OS ocorrem em milissegundos;
* **Segurança de Dados:** O banco de dados fica sob custódia da própria empresa;
* **Conectividade Corporativa:** Permite sincronização segura entre Matriz e Filiais via rede local (LAN) ou VPN;
* **Precisão de Auto Elétrica:** Recursos especializados de medição de alternador, bateria, queda de tensão e suporte de IA para leitura de falhas DTC.

---

## 2. A Regra de Ouro da Oficina (Fluxo Mestre)

Para garantir que o estoque não fure, o financeiro não minta e os clientes fiquem satisfeitos, toda a equipe deve seguir rigorosamente o seguinte ciclo:

```
[1. Cliente] ──► [2. Veículo] ──► [3. Orçamento] ──► [4. OS Autorizada]
                                                              │
                                                              ▼
[7. Dashboard] ◄── [6. Fechamento] ◄── [5. Pagamento] ◄── [Execução Técnica]
```

1. **Cadastrar o Cliente:** Sempre pesquisar antes de cadastrar para evitar duplicidade;
2. **Cadastrar o Veículo:** Vincular a placa correta ao cliente com quilometragem e ano/modelo;
3. **Gerar o Orçamento:** Listar peças e serviços com preços transparentes e enviar ao cliente;
4. **Converter em Ordem de Serviço (OS):** Somente após o cliente autorizar formalmente o serviço;
5. **Execução Técnica:** Eletricista aponta peças utilizadas, medições elétricas e avança o Kanban;
6. **Recebimento e Baixa:** Receber o valor no PDV ou Financeiro (PIX, Cartão ou Faturado Frota);
7. **Fechamento e Auditoria:** Conferir o caixa no final do dia e acompanhar o Dashboard.

> **Importante:** Nunca atenda "de cabeça", nunca anote peças em papel de pão e nunca venda peças no balcão sem passar pelo PDV.

---

## 3. Guia Módulo a Módulo

### 3.1. Dashboard Executivo
* **Objetivo:** Visão panorâmica da oficina em tempo real logo após o login.
* **O que você vê:** Faturamento acumulado do mês, total de OS abertas no pátio, orçamentos aguardando aprovação e alertas de peças críticas.
* **Como operar:** Olhe todo dia de manhã para planejar o faturamento e conferir o volume de trabalho pendente. Se algum número parecer desatualizado, clique no botão **"Atualizar"**.

### 3.2. Cadastros Base (Clientes e Veículos)
* **Objetivo:** Manter a base de dados de proprietários e histórico técnico dos automóveis.
* **Passo a Passo Cliente:** Acesse `Clientes → Novo Cliente → Preencha Nome, CPF/CNPJ, Telefone/WhatsApp e Endereço → Salvar`.
* **Passo a Passo Veículo:** Acesse `Veículos → Novo Veículo → Selecione o Cliente → Digite a Placa (Mercosul ou antiga) → Informe Modelo, Ano e KM → Salvar`.
* **Dica de Ouro:** A pesquisa no topo da tela localiza qualquer cadastro em menos de 1 segundo por nome, documento ou placa.

### 3.3. Orçamentos Comerciais
* **Objetivo:** Apresentar a proposta de preço ao cliente antes de iniciar a desmontagem.
* **Operação:** Acesse `Orçamentos → Novo`. Escolha o cliente e veículo. Adicione as peças necessárias (com preço do estoque) e os serviços de mão de obra.
* **Envio Rápido:** Clique em **"Exportar PDF"** ou no botão de **"WhatsApp"** para disparar a proposta formatada para o celular do cliente.
* **Aprovação:** Assim que o cliente autorizar, clique em **"Converter para OS"**. O sistema cria a Ordem de Serviço automaticamente com todos os itens já vinculados.

### 3.4. Ordens de Serviço e Laudo Técnico
* **Objetivo:** Controlar formalmente a execução mecânica e elétrica do veículo no pátio.
* **Campos Principais:**
  * **Responsável Técnico:** Eletricista encarregado do serviço;
  * **Status:** Aberta, Em Diagnóstico, Aguardando Peça, Em Andamento, Concluída, Entregue;
  * **Peças Aplicadas:** Adicione exatamente as peças retiradas do almoxarifado;
  * **Laudo Técnico:** Parecer do eletricista sobre o defeito encontrado e o reparo executado.

### 3.5. Kanban da Oficina
* **Objetivo:** Gestão visual do fluxo de trabalho no chão de fábrica.
* **Como usar:** Os veículos aparecem como cartões coloridos organizados por colunas. O gerente ou técnico simplesmente arrasta o cartão conforme o serviço evolui.
* **Prioridade Crítica:** Dê atenção imediata aos cartões na coluna **"Aguardando Peça"** para não deixar boxes e elevadores travados.

### 3.6. PDV / Frente de Caixa
* **Objetivo:** Atendimento ágil de balcão e recebimento de ordens de serviço finalizadas.
* **Abertura de Caixa (Manhã):** Clique em **"Abrir Caixa"**, conte o dinheiro físico do fundo de troco (suprimento) e confirme.
* **Venda de Peças:** Digite o nome ou passe o leitor de código de barras na peça (relés, fusíveis, lâmpadas, baterias). Selecione a forma de pagamento (PIX, Dinheiro, Cartão) e finalize.
* **Sangria:** Quando houver excesso de dinheiro na gaveta, registre a **"Sangria"** e envie ao cofre.
* **Fechamento Cego (Fim do Turno):** Clique em **"Fechar Caixa"**, conte as notas e moedas sem olhar o sistema e digite o valor real da gaveta. O sistema emitirá o extrato de conferência.

### 3.7. Auto Elétrica Técnica (Medições e Ripple)
* **Objetivo:** Registro detalhado de grandezas elétricas automotivas.
* **Funcionalidades:**
  * **Teste de Bateria:** Tensão em repouso (V), corrente de partida (CCA) e teste de carga sob demanda;
  * **Alternador & Ripple:** Medição da tensão de carga (13,8V a 14,4V) e componente AC (ripple) para identificar diodos retificadores com fuga;
  * **Queda de Tensão:** Teste de aterramento do motor e polo positivo de alimentação da partida;
  * **Checklist de Iluminação e Chicotes:** Registro de lâmpadas queimadas e curtos-circuitos.

### 3.8. Copilot de IA Dual-Engine
* **Objetivo:** Inteligência Artificial especializada para guiar o diagnóstico elétrico complexo.
* **Operação:** Abra o Copilot de IA a partir da OS ou menu lateral. Digite o código de falha lido no scanner (ex.: `P0335 - Sensor de Rotação`) e o veículo.
* **Resultado:** O motor RAG determinístico apresenta:
  1. Descrição funcional do circuito;
  2. Pinagem da ECU e cores dos condutores;
  3. Tensões e formas de onda esperadas com multímetro e osciloscópio;
  4. Roteiro de testes eliminatório passo a passo.
* **Exportação:** Clique em **"Copiar para Laudo da OS"** para incluir o parecer técnico no histórico oficial do automóvel.

### 3.9. Controle de Ferramentaria 4.0
* **Objetivo:** Custódia de instrumentos de alto valor (scanners, osciloscópios, torquímetros) e trava anti-extravio.
* **Check-Out (Empréstimo):** O ferramenteiro bipe a ferramenta (etiqueta ZPL) e seleciona o técnico responsável. A responsabilidade é transferida formalmente.
* **Check-In (Devolução):** Na devolução, confere-se cabos e pontas de prova e conclui-se a devolução.
* **Trava Anti-Extravio:** O sistema bloqueia a finalização da OS se alguma ferramenta estiver vinculada àquele veículo, impedindo que ferramentas fiquem esquecidas sob o capô do cliente.
* **Calibração Periódica:** Alertas visuais indicam instrumentos com prazo de aferição vencido.

### 3.10. Compras Anti-Ruptura e Ponto de Pedido (ROP)
* **Objetivo:** Garantir que nunca falte peça crítica para os boxes da oficina.
* **Cálculo de ROP:** O sistema monitora a velocidade de consumo e o tempo de entrega do fornecedor, acionando o alerta assim que o estoque atinge o limite seguro.
* **Cotação Multi-Fornecedor:** Crie um lote de compra e compare propostas de até 3 distribuidores com cálculo automático da melhor oferta (preço e prazo).
* **Ordem de Compra (OC):** Formalize a compra e gere o documento oficial para envio ao distribuidor.

### 3.11. Estoque e Curva ABC
* **Objetivo:** Gestão acurada de peças com valorização patrimonial.
* **Endereçamento:** Localize qualquer peça informando Rua, Prateleira e Gaveta.
* **Curva ABC:** Identifique os itens de alto giro (Classe A: lâmpadas H4, baterias 60Ah, fusíveis mini) e evite capital parado em peças de baixa procura (Classe C).
* **Importação XML:** Ao receber mercadorias do distribuidor, importe o XML da NF-e para alimentar os saldos e cadastrar o contas a pagar automaticamente.

### 3.12. Transferências Inter-Filiais ACID
* **Objetivo:** Movimentação segura de peças entre diferentes lojas da mesma rede.
* **Operação:** A filial de origem abre a remessa e despacha as peças. O saldo sai da disponibilidade imediata e entra em status **"Em Trânsito"**.
* **Recebimento:** A filial de destino confere os volumes físicos e clica em **"Confirmar Recebimento"**. A transação é gravada com segurança transacional atômica (ACID).

### 3.13. Gestão de Frotas B2B e Faturamento Periódico
* **Objetivo:** Atendimento a empresas, transportadoras e frotistas conveniados.
* **Cadastro de Contrato:** Defina a tabela de preços com descontos negociados e limites de crédito.
* **Telemetria de KM:** Na abertura de cada OS, registre a quilometragem do veículo e o condutor.
* **Faturamento Agrupado:** No fim da quinzena ou mês, selecione o frotista e clique em **"Gerar Faturamento Consolidado"**. O sistema emite a fatura unificada acompanhada do romaneio analítico por placa e KM.

### 3.14. Central Fiscal (NFS-e e NFC-e)
* **Objetivo:** Emissão direta de documentos fiscais eletrônicos via Gateway Fiscal plugável.
* **NFS-e (Mão de Obra):** Transmissão eletrônica do valor dos serviços prestados para a Prefeitura municipal.
* **NFC-e (Balcão / Peças):** Emissão de cupom fiscal eletrônico para o consumidor final em vendas de peças.
* **Contingência:** Em caso de oscilação na SEFAZ, as notas são gravadas com segurança e reprocessadas automaticamente assim que o serviço normalizar.

### 3.15. Impressão Térmica de Etiquetas ZPL II
* **Objetivo:** Identificação profissional imediata na oficina em impressoras industriais (Zebra, Argox, Elgin).
* **Modelos Disponíveis:**
  1. **Etiqueta de Chave do Carro:** Placa, número da OS, cliente e data (resistente a óleo e graxa);
  2. **Etiqueta de Prateleira:** Código do produto, descrição e código de barras Code 128;
  3. **Etiqueta de Ferramenta Especial:** QR Code para check-out rápido na ferramentaria.

### 3.16. Financeiro e Fluxo de Caixa Projetado
* **Objetivo:** Controle total de entradas, saídas e previsibilidade de caixa.
* **Contas a Receber:** Acompanhe recebíveis de cartões, PIX, boletos e convênios de frotas faturadas.
* **Contas a Pagar:** Programe duplicatas de fornecedores de autopeças e despesas fixas da oficina.
* **Fluxo de Caixa D+30:** Gráfico interativo que projeta o saldo bancário para os próximos 30 dias com base nas contas agendadas.

### 3.17. Relatórios e Business Intelligence
* **Objetivo:** Relatórios gerenciais detalhados para tomada de decisão.
* **Filtros e Exportação:** Filtre por período, filial ou técnico e exporte relatórios consolidados em formato **PDF** profissional ou planilhas **Excel**.
* **Relatórios Principais:** DRE Gerencial, Produtividade por Eletricista, Faturamento por Categoria de Serviço, e Curva ABC de Vendas.

### 3.18. Multi-Filiais Corporativo
* **Objetivo:** Gestão centralizada de redes de auto elétrica e franquias.
* **Alternância de Loja:** O operador pode alternar entre unidades de trabalho com 1 clique (conforme suas permissões).
* **Consolidação:** A diretoria visualiza indicadores consolidados de toda a rede ou isolados por filial.

### 3.19. Licença HMAC e Segurança
* **Objetivo:** Proteção de autenticidade, auditoria e amarração de hardware.
* **Hardware ID:** Identificador exclusivo da estação de trabalho gerado criptograficamente via SHA-256.
* **Assinatura HMAC:** Chave criptográfica que atesta a legitimidade da versão Enterprise e impede violações de segurança.

### 3.20. Configurações e Backup Automático
* **Objetivo:** Parametrização geral da oficina e blindagem contra perda de dados.
* **Backup Automático:** Configure a rotina para gerar cópias diárias compactadas do banco de dados em pasta segura ou disco externo.
* **Restauração:** Em caso de troca de máquina ou emergência, a restauração é feita com 1 clique diretamente na tela de Backup.

---

## 4. Guia de Rotinas por Cargo (Do Caixa ao CEO)

### CEO / Diretor Executivo
1. Analisar o DRE Consolidado de todas as unidades da rede;
2. Monitorar a produtividade média de boxes e o ticket médio global;
3. Auditar a Curva ABC e o volume de capital imobilizado em estoques;
4. Validar os contratos corporativos com grandes frotas e cumprimento de SLAs.

### Proprietário / Sócio-Fundador
1. Abrir o Dashboard de manhã e conferir o faturamento acumulado do mês;
2. Auditar os fechamentos cegos de caixa e conferir as sangrias de segurança;
3. Aprovar descontos comerciais especiais em orçamentos de grande porte;
4. Confirmar a execução diária do backup do banco de dados.

### Gerente de Oficina
1. Conduzir a Daily matinal de 5 minutos alinhando o Kanban com os eletricistas;
2. Distribuir veículos por competência técnica e alocação de boxes/elevadores;
3. Eliminar gargalos cobrando prioridade em veículos "Aguardando Peça";
4. Conferir a devolução de ferramentas antes de liberar qualquer veículo.

### Gestor de Frotas B2B
1. Cadastrar contratos corporativos e tabelas de preços com frotistas;
2. Garantir o apontamento da quilometragem (KM) e motorista em cada OS;
3. Realizar o fechamento periódico e emitir o romaneio analítico por placa.

### Comprador
1. Monitorar os alertas de Ponto de Pedido (ROP) no módulo Compras;
2. Gerar cotações com múltiplos distribuidores buscando a melhor oferta de preço e prazo;
3. Emitir Ordens de Compra formais e acompanhar os prazos de entrega.

### Almoxarife / Estoquista
1. Conferir fisicamente as mercadorias recebidas e importar o XML da nota fiscal;
2. Guardar peças nas posições corretas (Rua/Prateleira/Gaveta) e imprimir etiquetas ZPL;
3. Fornecer peças aos eletricistas mediante baixa estrita na Ordem de Serviço;
4. Conduzir contagens semanais do inventário rotativo.

### Ferramenteiro 4.0
1. Efetuar o check-out de scanners e osciloscópios com bipagem de etiqueta;
2. Conferir integridade física dos cabos e acessórios no check-in de devolução;
3. Monitorar prazos de calibração periódica de torquímetros e manômetros.

### Consultor de Atendimento / Recepção
1. Acolher o cliente e pesquisar o cadastro no sistema antes de criar novo;
2. Preencher o checklist de entrada registrando avarias, pertences e KM;
3. Elaborar o orçamento e enviar a proposta ao cliente via WhatsApp ou impresso;
4. Converter o orçamento aprovado em OS e direcionar ao pátio.

### Eletricista Automotivo / Técnico Diagnosticador
1. Assumir a OS no Kanban e mover para "Em Andamento";
2. Consultar o Copilot de IA para códigos DTC e diagramas de chicote;
3. Realizar testes elétricos (ripple de alternador, queda de tensão e bateria);
4. Apontar na OS todas as peças instaladas e fazer o check-in das ferramentas.

### Operador de Caixa / Balcão PDV
1. Abrir o caixa pela manhã informando o valor exato do troco na gaveta;
2. Realizar vendas rápidas de balcão e recebimentos de OS concluídas;
3. Efetuar sangrias periódicas sempre que a gaveta atingir o teto de segurança;
4. Realizar o fechamento cego de caixa ao término do expediente.

### Analista Financeiro & Fiscal
1. Efetuar baixas bancárias de contas a receber e conferir faturas de frotistas;
2. Programar pagamentos a fornecedores de autopeças e despesas operacionais;
3. Transmitir notas fiscais NFS-e e NFC-e na Central Fiscal;
4. Acompanhar a projeção de fluxo de caixa para os próximos 30 dias.

---

## 5. Atalhos de Teclado

| Tecla de Atalho | Ação Executada |
|---|---|
| **F1** | Abrir a Central de Ajuda em qualquer tela |
| **Ctrl + N** | Novo registro (Cliente, Veículo, Orçamento, OS) |
| **Ctrl + S** | Salvar dados da tela atual |
| **F5** | Atualizar lista / recarregar dados |
| **Ctrl + F** | Focar no campo de busca/pesquisa da tela |
| **Esc** | Fechar janela modal ou cancelar ação |
| **Delete** | Excluir item selecionado (com confirmação de segurança) |

---

## 6. Erros Comuns e Como Evitar

1. **Cliente Duplicado:** Sempre busque por CPF/CNPJ ou telefone antes de clicar em "Novo". Duplicar clientes quebra o histórico do carro e confunde cobranças.
2. **Placa Errada:** Confira a placa digitada no documento do veículo. Cadastrar placa errada cria um "veículo fantasma" no sistema.
3. **Vender Fora do PDV:** Toda peça que sai do balcão deve passar pelo PDV. Entregar peças sem registrar gera furo de estoque e perda de dinheiro.
4. **Finalizar OS sem Lançar Peças:** Se a peça foi colocada no carro mas não foi lançada na OS, seu estoque estará errado e a oficina terá prejuízo contábil.
5. **Dormir com Caixa Aberto:** Nunca deixe o caixa aberto de um dia para o outro. Faça o fechamento cego todo fim de tarde para garantir a integridade financeira da empresa.
