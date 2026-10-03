# PRIMOX Workshop — Checklist Mestre de Implementação
## Expansão Estratégica: Ferramental, Compras Anti-Ruptura & Copilot de IA Especialista

> **Documento:** Checklist Executivo e Técnico Passo a Passo  
> **Caminho:** `Docs/CHECKLIST-FERRAMENTAS-COMPRAS-IA.md`  
> **Referência Técnica:** [`PRIMOX-FERRAMENTAS-COMPRAS-IA-COPILOT.md`](file:///home/campos/Projetos/CamposCodingHub/PrimoAutoEletrica/Docs/architecture/PRIMOX-FERRAMENTAS-COMPRAS-IA-COPILOT.md)  
> **Stack:** .NET 6.0-windows · C# 10 · WPF · SQLite / SQL Server · PRIMOX Design System (Dark/Light)  
> **Data de Início:** 2026-10-03  
> **Status:** Pronto para Execução  

---

## Índice

1. [Visão Geral dos 3 Pilares e Metas Finais](#1-visão-geral-dos-3-pilares-e-metas-finais)
2. [PILAR 1: Produtos em Falta & Gestão de Compras (Anti-Ruptura)](#2-pilar-1-produtos-em-falta--gestão-de-compras-anti-ruptura)
   - [Etapa 1.1: Modelos e Migração do Banco de Dados](#etapa-11-modelos-e-migração-do-banco-de-dados)
   - [Etapa 1.2: Motor de Negócio e Algoritmo ROP (Ponto de Pedido)](#etapa-12-motor-de-negócio-e-algoritmo-rop-ponto-de-pedido)
   - [Etapa 1.3: Interface de Usuário — Necessidade de Compras](#etapa-13-interface-de-usuário--necessidade-de-compras)
   - [Etapa 1.4: Cotação Rápida WhatsApp e Emissão de Pedido PDF](#etapa-14-cotação-rápida-whatsapp-e-emissão-de-pedido-pdf)
   - [Etapa 1.5: Baixa Automatizada por Importação de NF-e](#etapa-15-baixa-automatizada-por-importação-de-nf-e)
3. [PILAR 2: Controle e Gestão de Ferramental Especializado](#3-pilar-2-controle-e-gestão-de-ferramental-especializado)
   - [Etapa 2.1: Modelagem de Dados e Esquema de Ferramentaria](#etapa-21-modelagem-de-dados-e-esquema-de-ferramentaria)
   - [Etapa 2.2: Serviço de Negócio de Ferramentas e Calibração](#etapa-22-serviço-de-negócio-de-ferramentas-e-calibração)
   - [Etapa 2.3: Interface de Usuário — Painel Visual de Ferramentas](#etapa-23-interface-de-usuário--painel-visual-de-ferramentas)
   - [Etapa 2.4: Modal de Empréstimo Rápido e Leitura por Código de Barras](#etapa-24-modal-de-empréstimo-rápido-e-leitura-por-código-de-barras)
   - [Etapa 2.5: Trava de Segurança Anti-Extravio no Fechamento de OS](#etapa-25-trava-de-segurança-anti-extravio-no-fechamento-de-os)
   - [Etapa 2.6: Integração com Auto Elétrica Técnica e Notificação de Fim de Turno](#etapa-26-integração-com-auto-elétrica-técnica-e-notificação-de-fim-de-turno)
4. [PILAR 3: Copilot de IA Dual-Engine (Operacional + Especialista Elétrico)](#4-pilar-3-copilot-de-ia-dual-engine-operacional--especialista-elétrico)
   - [Etapa 3.1: Arquitetura de Provedores de IA (Gemini + Local Offline + Fallback)](#etapa-31-arquitetura-de-provedores-de-ia-gemini--local-offline--fallback)
   - [Etapa 3.2: Interface Retrátil — Copilot Flyout Panel (Ctrl + I)](#etapa-32-interface-retrátil--copilot-flyout-panel-ctrl--i)
   - [Etapa 3.3: Motor 1 — Automação ERP via Function Calling](#etapa-33-motor-1--automação-erp-via-function-calling)
   - [Etapa 3.4: Motor 2 — RAG Técnico e Diagnóstico Elétrico Automotivo](#etapa-34-motor-2--rag-técnico-e-diagnóstico-elétrico-automotivo)
   - [Etapa 3.5: Ações Rápidas (One-Click) e Inserção em Orçamentos/OS](#etapa-35-ações-rápidas-one-click-e-inserção-em-orçamentosos)
5. [PILAR 4: Notificações Shell e Alertas Globais](#5-pilar-4-notificações-shell-e-alertas-globais)
6. [PILAR 5: Testes, Homologação e Definição de Pronto (DoD)](#6-pilar-5-testes-homologação-e-definição-de-pronto-dod)
7. [Quadro Resumo de Progresso](#7-quadro-resumo-de-progresso)

---

## 1. Visão Geral dos 3 Pilares e Metas Finais

```
┌───────────────────────────────────────────────────────────────────────────┐
│                           PRIMOX WORKSHOP 2.0                             │
├─────────────────────┬─────────────────────────────┬───────────────────────┤
│   GESTAO DE COMPRAS │    CONTROLE DE FERRAMENTAL  │   COPILOT DE IA DUAL  │
│     ANTI-RUPTURA    │       ANTI-EXTRAVIO         │   OPERACIONAL/TECNICO │
├─────────────────────┼─────────────────────────────┼───────────────────────┤
│ • Ponto de Pedido   │ • Painel visual de armários │ • Atalho Global Ctrl+I│
│ • Reservas ativas OS│ • Empréstimo em 5s (Barcode)│ • Function Calling ERP│
│ • Cotação WhatsApp  │ • Trava de liberação de OS  │ • RAG Elétrico c/ DTC │
│ • Ordem Compra PDF  │ • Alerta calibração         │ • Modo Nuvem + Offline│
│ • Baixa via NFe XML │ • Notificação fim de turno  │ • Ações em 1 clique   │
└─────────────────────┴─────────────────────────────┴───────────────────────┘
```

---

## 2. PILAR 1: Produtos em Falta & Gestão de Compras (Anti-Ruptura)

### Etapa 1.1: Modelos e Migração do Banco de Dados
- [x] Criar enum `NivelUrgenciaFalta` (`Critica`, `Alta`, `Media`, `Preventiva`) em `PrimoAutoEletrica/Models/ItemFaltaEstoque.cs`.
- [x] Criar enum `StatusPedidoCompra` (`Rascunho`, `CotacaoEnviada`, `AprovadoAguardandoEntrega`, `RecebidoParcial`, `RecebidoTotal`, `Cancelado`) em `PrimoAutoEletrica/Models/PedidoCompra.cs`.
- [x] Criar classe de modelo `ItemFaltaEstoque.cs` contendo propriedades: `ProdutoId`, `Codigo`, `Nome`, `Categoria`, `QuantidadeEstoque`, `QuantidadeMinima`, `QuantidadeReservadaOS`, `SaldoRealDisponivel`, `QuantidadeSugeridaCompra`, `UltimoCustoCompra`, `ValorTotalEstimado`, `Urgencia`, `CurvaAbc`, `FornecedorPreferencialId`, `FornecedorPreferencialNome`.
- [x] Criar classe `PedidoCompra.cs` e `PedidoCompraItem.cs` com propriedades de cabeçalho, vínculo com fornecedor e itens detalhados.
- [x] Adicionar migração no `PrimoAutoEletrica/Services/DatabaseService.Migrations.cs`:
  - [x] Criar tabela `PedidosCompra` com índices em `Status` e `FornecedorId`.
  - [x] Criar tabela `PedidosCompraItens` com chave estrangeira para `PedidosCompra(Id)` e índice em `ProdutoId`.
  - [x] Adicionar colunas em `Produtos`: `LeadTimeDias INTEGER DEFAULT 3`, `EstoqueSeguranca INTEGER DEFAULT 2`.
- [x] Validar script SQLite e compatibilidade com SQL Server no `DatabaseService`.

> **O que se espera no final da Etapa 1.1:**  
> Tabelas criadas de forma idempotente sem perda de dados existentes; models C# fortemente tipados e prontos para injeção no repositório.

---

### Etapa 1.2: Motor de Negócio e Algoritmo ROP (Ponto de Pedido)
- [x] Criar interface `IGestaoComprasService` e implementação `GestaoComprasService` em `PrimoAutoEletrica/Services/GestaoComprasService.cs`.
- [x] Implementar método `Task<List<ItemFaltaEstoque>> ObterNecessidadesReposicaoAsync()`:
  - [x] Cruzar estoque atual com `EstoqueOperationalService` para calcular quantidade reservada em Ordens de Serviço em andamento.
  - [x] Calcular saldo real: `SaldoReal = QuantidadeEstoque - QuantidadeReservadaOS`.
  - [x] Calcular Consumo Médio Diário ($CMD$) com base no histórico dos últimos 60 dias de saídas.
  - [x] Calcular Ponto de Pedido: $ROP = (CMD \times LeadTime) + EstoqueSeguranca$.
  - [x] Classificar Urgência:
    - `Critica`: $SaldoReal \le 0$ E há OS aberta aguardando o item.
    - `Alta`: $SaldoReal \le QuantidadeMinima$.
    - `Media`: $SaldoReal \le ROP$.
    - `Preventiva`: Reposição de giro Curva A/B.
  - [x] Calcular Quantidade Sugerida de Compra baseada no lote econômico para manter 30 dias (Curva A), 45 dias (Curva B) ou 15 dias (Curva C).
- [x] Implementar testes unitários para cálculos de ROP, pendências e cotações em `Tests/PrimoAutoEletrica.Tests/GestaoComprasTests.cs`.

> **O que se espera no final da Etapa 1.2:**  
> O serviço retorna em milissegundos uma lista priorizada de peças que precisam ser compradas, distinguindo emergências imediatas de reposições rotineiras.

---

### Etapa 1.3: Interface de Usuário — Necessidade de Compras
- [x] Criar `PrimoAutoEletrica/UserControls/ComprasNecessidadeControl.xaml` e `.xaml.cs`:
  - [x] Cabeçalho com cards de resumo estatístico (Total de Itens em Falta, Rupturas Críticas, Custo Total Estimado de Reposição, Fornecedores Envolvidos).
  - [x] Barra de ferramentas com filtros: Busca rápida (código/descrição), ComboBox de Urgência (`Todas`, `Crítica`, `Alta`, etc.), ComboBox de Fornecedor e Seletor de Curva ABC.
  - [x] `DataGrid` estilizado via `PremiumDataGrid` (compatível com Dark e Light Mode) exibindo colunas:
    - Badge de Urgência colorido (Vermelho pulsante para Crítica, Âmbar para Alta, Azul para Média).
    - Código e Descrição do Produto.
    - Estoque Atual / Reservado em OS / Saldo Real.
    - Estoque Mínimo / Ponto de Pedido.
    - Quantidade Sugerida (editável pelo usuário caso queira ajustar).
    - Último Custo Unitário e Custo Total Estimado.
    - Fornecedor Preferencial.
  - [x] Checkbox de seleção em lote para gerar pedido conjunto.
- [x] Adicionar aba ou botão de acesso no módulo de Estoque ou na Sidebar principal.
- [x] Testar renderização de cores no Modo Escuro (`Colors.Dark.xaml`) e Modo Claro (`Colors.Light.xaml`).

> **O que se espera no final da Etapa 1.3:**  
> Uma tela clara, limpa e funcional onde o comprador da oficina abre e imediatamente vê quais peças estão travando serviços, com valores totais e filtros responsivos.

---

### Etapa 1.4: Cotação Rápida WhatsApp e Emissão de Pedido PDF
- [x] Implementar gerador de texto de cotação para WhatsApp no `GestaoComprasService`:
  - Formato padronizado contendo: Nome da Oficina, Data, Lista de Códigos/Descrições/Quantidades e solicitação de orçamento/prazo.
  - Botão na UI: **"Cotar via WhatsApp"** -> Copia texto para o Clipboard e abre `Process.Start("https://wa.me/55{TelefoneFornecedor}?text=...")`.
- [x] Implementar emissão de Ordem de Compra formal no `DocumentoPdfService`:
  - Layout A4 profissional com dados da oficina, dados do fornecedor, número do pedido (`PC-2026-XXXX`), tabela de itens com quantidade, campos de aprovação e prazos acordados.
  - Botão na UI: **"Gerar Pedido de Compra (PDF)"** -> Abre visualizador de PDF ou salva direto em arquivo.
- [x] Gravação do pedido gerado no banco de dados com status `CotacaoEnviada` ou `AprovadoAguardandoEntrega`.

> **O que se espera no final da Etapa 1.4:**  
> O encarregado de compras seleciona 5 peças, clica em "WhatsApp" e a mensagem já vai montada direto para o vendedor do distribuidor de autopeças em 2 segundos.

---

### Etapa 1.5: Baixa Automatizada por Importação de NF-e
- [x] Estender `ImportarNotaWindow.xaml.cs` e `GestaoComprasService`:
  - [x] Ao ler o XML da NF-e recebida, verificar se o CNPJ do emitente coincide com algum `PedidoCompra` com status `AprovadoAguardandoEntrega`.
  - [x] Se houver vínculo, cruzar os itens do XML com os itens do pedido de compra.
  - [x] Realizar baixa automática de pendências no pedido de compra:
    - Atualizar `QuantidadeRecebida` de cada item do pedido.
    - Marcar o pedido como `RecebidoTotal` ou `RecebidoParcial`.
    - Incrementar o estoque físico e liberar eventuais reservas de OS bloqueadas.
    - Registrar log de auditoria com a chave da NF-e vinculada.

> **O que se espera no final da Etapa 1.5:**  
> Quando a mercadoria chega e a nota é importada, o pedido de compra em aberto é baixado sozinho sem necessidade de digitação dupla.

---

## 3. PILAR 2: Controle e Gestão de Ferramental Especializado

### Etapa 2.1: Modelagem de Dados e Esquema de Ferramentaria
- [ ] Criar enum `StatusFerramenta` (`Disponivel`, `EmUso`, `EmManutencao`, `Avariada`, `Extraviada`) em `PrimoAutoEletrica/Models/Ferramenta.cs`.
- [ ] Criar enum `CategoriaFerramenta` (`DiagnosticoEletronico`, `MedicaoEletrica`, `BateriasECarga`, `EletricaEMontagem`, `MecanicaGeral`).
- [ ] Criar modelo `Ferramenta.cs`: `Id`, `CodigoPatrimonio`, `Nome`, `Categoria`, `MarcaModelo`, `NumeroSerie`, `LocalizacaoArmario`, `Status`, `ValorAquisicao`, `DataAquisicao`, `RequerCalibracaoPeriodica`, `IntervaloCalibracaoDias`, `UltimaCalibracao`, `ProximaCalibracao`, `FuncionarioPosseAtualId`, `FuncionarioPosseAtualNome`, `OrdemServicoAtualId`, `NumeroOSAtual`, `Observacoes`, `Ativo`.
- [ ] Criar modelo `MovimentacaoFerramenta.cs`: `Id`, `FerramentaId`, `FuncionarioId`, `OrdemServicoId`, `DataRetirada`, `PrevisaoDevolucao`, `DataDevolucao`, `EstadoConservacaoRetirada`, `EstadoConservacaoDevolucao`, `ObservacaoDevolucao`, `RegistradoPor`.
- [ ] Adicionar migração de banco de dados em `DatabaseService.Migrations.cs` para tabelas `Ferramentas` e `MovimentacoesFerramentas` com índices adequados.

> **O que se espera no final da Etapa 2.1:**  
> Estrutura completa de banco de dados e entidades prontas para rastrear patrimônio e histórico de retiradas.

---

### Etapa 2.2: Serviço de Negócio de Ferramentas e Calibração
- [ ] Criar `PrimoAutoEletrica/Services/FerramentaService.cs`:
  - [ ] `Task<List<Ferramenta>> ListarFerramentasAsync(FiltroFerramenta filtro)`
  - [ ] `Task RegistrarRetiradaAsync(Guid ferramentaId, Guid funcionarioId, Guid? ordemServicoId, string estadoConservacao)`:
    - Validar se a ferramenta está `Disponivel`.
    - Atualizar status para `EmUso` e registrar vínculo de posse e OS.
    - Criar registro em `MovimentacoesFerramentas`.
  - [ ] `Task RegistrarDevolucaoAsync(Guid ferramentaId, string estadoConservacao, string observacoes)`:
    - Atualizar status para `Disponivel` (ou `Avariada` se reportado defeito).
    - Desvincular funcionário e OS.
    - Fechar registro em `MovimentacoesFerramentas` com data/hora de devolução.
  - [ ] `Task<List<Ferramenta>> ObterFerramentasComCalibracaoVencendoAsync(int diasAlerta = 7)`
  - [ ] `Task<List<Ferramenta>> ObterFerramentasPendentesOSAsync(Guid ordemServicoId)`
  - [ ] `Task<List<MovimentacaoFerramenta>> ObterHistoricoMovimentacaoAsync(Guid ferramentaId)`
- [ ] Escrever testes unitários em `Tests/PrimoAutoEletrica.Tests/FerramentaServiceTests.cs`.

> **O que se espera no final da Etapa 2.2:**  
> Regras de negócio estritas que não permitem duas pessoas pegarem a mesma ferramenta, mantêm histórico para auditoria e controlam datas de calibração.

---

### Etapa 2.3: Interface de Usuário — Painel Visual de Ferramentas
- [ ] Criar `PrimoAutoEletrica/UserControls/FerramentasControl.xaml` e `.xaml.cs`:
  - [ ] Barra superior com contadores visuais: Total de Ferramentas, Disponíveis (Verde), Em Uso (Amarelo), Em Manutenção/Calibração (Azul), Avariadas/Atenção (Vermelho).
  - [ ] Abas de visualização:
    - **Visão em Grade de Armários / Cartões**: Cards visuais com ícone da categoria, código de patrimônio, nome do equipamento, badge de status e foto/localização (ex: "Armário A - Gaveta 2").
    - **Visão em Lista / Tabela Detalhada**: `PremiumDataGrid` com ordenação por coluna, filtros por categoria, status e busca textual.
  - [ ] Indicador visual nos cards em uso: Nome e foto do técnico que retirou + link clicável da OS em atendimento.
  - [ ] Botão de Ação Rápida no topo: **"Registrar Retirada / Devolução (F2)"** e **"Cadastrar Nova Ferramenta"**.
- [ ] Integrar nova opção de navegação no menu principal / Sidebar do PRIMOX com ícone apropriado (`Wrench` / `Tools`).
- [ ] Garantir 100% de conformidade com o Dark Mode e Light Mode.

> **O que se espera no final da Etapa 2.3:**  
> Um painel moderno estilo "oficina 4.0", onde qualquer colaborador ou gerente bate o olho na tela e sabe exatamente onde cada scanner ou osciloscópio está guardado ou quem está usando.

---

### Etapa 2.4: Modal de Empréstimo Rápido e Leitura por Código de Barras
- [ ] Criar `PrimoAutoEletrica/Views/EmprestarDevolverFerramentaDialog.xaml` e `.xaml.cs`:
  - [ ] Caixa de texto em foco imediato com suporte a leitor de código de barras ou bip de QR Code.
  - [ ] Ao bipar o código:
    - Se a ferramenta estiver **Disponível**: Preenche automaticamente os dados do equipamento, solicita selecionar o Técnico (com atalhos rápidos) e a OS (opcional), e confirma a retirada com `Enter`.
    - Se a ferramenta estiver **Em Uso**: Entende que é um ato de devolução; exibe quem retirou, há quanto tempo está fora, pergunta o estado de conservação (`OK`, `Necessita Limpeza`, `Avariada`) e confirma com `Enter`.
  - [ ] Tempo total de operação projetado: **Menos de 4 segundos**.

> **O que se espera no final da Etapa 2.4:**  
> Retirada e devolução ultra-ágeis no balcão de ferramentas que não geram atrito nem atrasam o fluxo da oficina mecânica.

---

### Etapa 2.5: Trava de Segurança Anti-Extravio no Fechamento de OS
- [ ] No `PrimoAutoEletrica/Views/OrdemServicoWindow.xaml.cs` (ou controle de edição de OS):
  - [ ] Interceptar o evento de mudança de status para `Concluída`, `Faturada` ou `Veículo Entregue`.
  - [ ] Invocar `FerramentaService.ObterFerramentasPendentesOSAsync(os.Id)`.
  - [ ] Se retornar um ou mais itens:
    - Bloquear o encerramento da OS.
    - Exibir diálogo de alerta de alta prioridade (Modal com estilo Danger/Warning do PRIMOX):
      > *"ATENÇÃO: Não é possível entregar o veículo ou concluir a OS nº [XXXX]!  
      > As seguintes ferramentas constam vinculadas a este serviço e podem ter sido esquecidas no veículo do cliente:  
      > • FER-0004 — Scanner Raven 3 (Posse: Técnico Carlos)  
      > • FER-0012 — Alicate Amperímetro Minipa  
      > Por favor, confirme o recolhimento das ferramentas no armário antes de liberar o carro."*
    - Disponibilizar botão direto no modal: **"Devolver Ferramentas Agora"** (com confirmação de senha do responsável se configurado).

> **O que se espera no final da Etapa 2.5:**  
> Fim definitivo do pesadelo de deixar ferramentas caras dentro do cofre do motor ou painel do carro do cliente.

---

### Etapa 2.6: Integração com Auto Elétrica Técnica e Notificação de Fim de Turno
- [ ] Integrar ao `AutoEletricaTecnicaControl`:
  - Na visualização dos testes de diagnóstico guiado, exibir tags de ferramentas necessárias (ex: `Osciloscópio`, `Caneta de Polaridade`), indicando se estão livres na oficina ou com quem estão no momento.
- [ ] Configurar job ou timer leve em segundo plano:
  - 30 minutos antes do término do expediente padrão da oficina, consultar se existem ferramentas com status `EmUso`.
  - Emitir notificação flutuante no `ShellNotificationService`: *"Fim de Turno: Há 4 ferramentas que ainda não foram guardadas no armário de ferramentaria."*.

> **O que se espera no final da Etapa 2.6:**  
> Sinergia total entre a área técnica e a organização física da oficina.

---

## 4. PILAR 3: Copilot de IA Dual-Engine (Operacional + Especialista Elétrico)

### Etapa 3.1: Arquitetura de Provedores de IA (Gemini + Local Offline + Fallback)
- [ ] Criar interface `IAIService` em `PrimoAutoEletrica/Services/AI/IAIService.cs`:
  - `Task<AIChatResponse> ProcessarMensagemAsync(AIChatRequest request, CancellationToken cancellationToken = default);`
  - `bool IsOnlineAvailable { get; }`
  - `AIProviderType ActiveProvider { get; }`
- [ ] Criar modelos em `PrimoAutoEletrica/Models/AI/`:
  - `AIChatMessage.cs` (Role: User, Assistant, System, Tool; Content; ToolCalls; Timestamp).
  - `AIChatRequest.cs` (Histórico de mensagens, contexto ativo do PRIMOX: tela atual, OS selecionada, cliente selecionado).
  - `AIChatResponse.cs` (Texto da resposta, Ações executáveis/Function Calls, Sugestões de botões, Fontes técnicas citadas).
- [ ] Implementar `GeminiAIService.cs`:
  - Comunicação HTTPS com a API do Google Gemini (`gemini-1.5-flash` ou `gemini-pro`).
  - Suporte a System Instructions ricas especializadas em auto elétrica e ERP.
  - Suporte a chamadas de ferramentas (*Function Calling*).
- [ ] Implementar `LocalFallbackAIService.cs`:
  - Motor determinístico local baseado nas regras existentes em `AutoEletricaTecnicaService` e nas tabelas locais do banco, respondendo mesmo sem internet ou sem chave de API configurada.
- [ ] Adicionar tela de configuração em `ConfiguracoesSistemaWindow.xaml` para inserção de chave de API da IA, teste de conexão e escolha do provedor padrão.

> **O que se espera no final da Etapa 3.1:**  
> Infraestrutura de IA robusta que nunca trava o sistema: usa nuvem de alta velocidade se disponível, e fallback offline inteligente caso a internet caia.

---

### Etapa 3.2: Interface Retrátil — Copilot Flyout Panel (Ctrl + I)
- [ ] Criar `PrimoAutoEletrica/UserControls/CopilotFlyoutPanel.xaml` e `.xaml.cs`:
  - [ ] Painel lateral retrátil posicionado na borda direita de `MainWindow.xaml` com animação suave de entrada/saída (largura padrão: 420px).
  - [ ] Botão de abertura com ícone de IA / Faísca / Robô no cabeçalho superior do PRIMOX ao lado do relógio/status.
  - [ ] Atalho de teclado global: `Ctrl + I` para abrir e fechar a qualquer momento.
  - [ ] Cabeçalho do Copilot:
    - Indicador visual de modo (`Dual-Engine Ativo`, `Modo Nuvem / Gemini` ou `Modo Offline`).
    - Botão de limpar histórico e botão de fechar.
  - [ ] Área de conversa com rolagem:
    - Balões de mensagens com distinção visual clara (usuário vs Copilot).
    - Suporte a Markdown renderizado (negrito, listas, blocos de medições elétricas em código/tabela).
    - Chips/Badges com o contexto capturado (ex: *"Contexto: OS #1042 — VW Gol G6"*).
  - [ ] Caixa de entrada de texto inferior:
    - `TextBox` expansível com dica *"Pergunte sobre um defeito elétrico ou comande o sistema..."*.
    - Botão de envio e atalho `Enter` (com `Shift+Enter` para nova linha).
    - Botões de sugestão rápida (*"Defeitos comuns do alternador"*, *"Consultar peças em falta"*, *"Quem está com o osciloscópio?"*).
- [ ] 100% estilizado com `Colors.Dark.xaml` e `Colors.Light.xaml` para harmonia total com o resto do aplicativo.

> **O que se espera no final da Etapa 3.2:**  
> Uma experiência fluida e nativa de assistente embutido, moderna e bonita, acessível a qualquer momento sem trocar de janela.

---

### Etapa 3.3: Motor 1 — Automação ERP via Function Calling
- [ ] Criar `PrimoAutoEletrica/Services/AI/Tools/AIToolRegistry.cs`:
  - Definição do catálogo de ferramentas expostas para a IA:
    1. `NavegarParaModulo(string modulo)` — Abre Financeiro, Estoque, Ordens de Serviço, Ferramentaria, etc.
    2. `ConsultarPecaEstoque(string termo)` — Retorna quantidade em estoque, prateleira, preço de venda e peças similares.
    3. `BuscarClienteOuVeiculo(string termo)` — Busca rápida por placa, nome ou CPF.
    4. `VerificarStatusFerramentas(string? categoria)` — Retorna quais ferramentas estão livres ou com qual técnico estão.
    5. `ConsultarResumoFinanceiro(string periodo)` — Retorna faturamento, contas a pagar e receber do dia/semana.
    6. `CriarRascunhoOrcamento(string placa, List<string> pecas)` — Prepara orçamento na tela.
- [ ] Implementar execução segura de ferramentas:
  - Ferramentas somente leitura são executadas imediatamente e o resultado devolvido ao prompt.
  - Ferramentas de escrita/alteração geram um **Cartão de Confirmação Visual** na mensagem do chat antes de aplicar (ex: *"Deseja que eu crie o orçamento para o Corolla ABC-1234 com 1x Bateria Moura 60Ah? [Confirmar] [Cancelar]"*).

> **O que se espera no final da Etapa 3.3:**  
> O operador pode controlar o sistema por linguagem natural ("IA, vê se tem relé auxiliar de 4 pinos e onde ele tá guardado").

---

### Etapa 3.4: Motor 2 — RAG Técnico e Diagnóstico Elétrico Automotivo
- [ ] Criar `PrimoAutoEletrica/Services/AI/AutomotiveKnowledgeRAGService.cs`:
  - Indexar as tabelas locais de roteiros de diagnóstico guiado de `AutoEletricaTecnicaService`.
  - Indexar base de códigos DTC padrão OBD-II (Powertrain P, Chassis C, Body B, Network U) com foco em elétrica:
    - *P0560 a P0563* (Falhas de Tensão do Sistema).
    - *P0620 a P0626* (Controle de Campo do Alternador / Regulador).
    - *U0100 a U0140* (Falhas de Comunicação em Rede CAN automotiva).
    - *B1000+* (Falhas de BCM, travas, vidros elétricos e iluminação).
- [ ] Implementar montagem do System Prompt Especialista:
  - Instruções de engenharia elétrica: ensinar a IA a orientar testes com multímetro (tensão contínua, queda de tensão máxima de 0.2V em condutores sob carga, teste de ripple AC no alternador, medição de fuga de corrente parasita com alicate amperímetro ou em série com borne negativo).
  - Associação com os veículos cadastrados: caso o usuário pergunte sobre um carro com prontuário elétrico preenchido, injetar as medições do prontuário no contexto.

> **O que se espera no final da Etapa 3.4:**  
> O técnico digita: *"Fiat Toro 2.0 Diesel acusando P0562 e luz da bateria piscando"* e a IA responde um passo a passo técnico cirúrgico:
> 1. Medir queda de tensão no cabo positivo entre alternador e borne (+);
> 2. Medir cabo de massa motor-chassi com faróis acesos;
> 3. Medir sinal LIN no conector do regulador inteligente;
> 4. Ferramentas sugeridas da oficina (com link se estão no armário);
> 5. Peças correspondentes em estoque no PRIMOX.

---

### Etapa 3.5: Ações Rápidas (One-Click) e Inserção em Orçamentos/OS
- [ ] Adicionar suporte a componentes interativos dentro do `CopilotFlyoutPanel`:
  - [ ] **Botão "Adicionar Peças ao Orçamento Aberto"**: Ao sugerir a troca do regulador de voltagem ou terminal de bateria, um botão adiciona o item direto na OS ativa com 1 clique.
  - [ ] **Botão "Copiar Procedimento de Teste para o Laudo da OS"**: Cola o passo a passo com valores de referência no campo de Observações Técnicas do serviço para valorizar o laudo entregue ao cliente.
  - [ ] **Botão "Reservar Ferramenta"**: Abre o modal de empréstimo já com a ferramenta sugerida pré-selecionada.

> **O que se espera no final da Etapa 3.5:**  
> O diagnóstico da IA se traduz instantaneamente em faturamento e organização prática dentro do sistema.

---

## 5. PILAR 4: Notificações Shell e Alertas Globais

- [ ] Integrar os 3 novos módulos ao `PrimoAutoEletrica/Services/ShellNotificationService.cs`:
  - [ ] Notificação de **Ruptura Imediata**: Disparada quando uma OS é aberta e uma peça necessária não possui saldo real.
  - [ ] Notificação de **Retenção de Ferramenta**: Disparada quando uma ferramenta ultrapassa a previsão de devolução em mais de 2 horas.
  - [ ] Notificação de **Calibração Vencida**: Disparada na inicialização do sistema se houver instrumentos com calibração expirada.
- [ ] Adicionar Badges numéricos na barra de navegação/Sidebar do PRIMOX:
  - Badge vermelho no ícone de Compras se houver itens críticos.
  - Badge amarelo no ícone de Ferramentas se houver ferramentas em atraso.

> **O que se espera no final do Pilar 4:**  
> Sistema proativo que avisa problemas antes que virem prejuízos para o dono da oficina.

---

## 6. PILAR 5: Testes, Homologação e Definição de Pronto (DoD)

- [ ] **Testes de Integração e Regressão**:
  - [ ] Executar suíte de testes xUnit (`Tests/PrimoAutoEletrica.Tests`).
  - [ ] Executar smoke tests (`UiSmokeTestService`).
  - [ ] Validar integridade do `QaEngine` (43/43 verificações).
- [ ] **Auditoria Visual Dark & Light Mode**:
  - [ ] Todas as novas telas testadas alternando entre o tema Escuro e Claro.
  - [ ] Zero controles com fundo branco órfão ou texto invisível no modo escuro.
- [ ] **Build & Deploy**:
  - [ ] Compilação de release com `dotnet publish -c Release -r win-x64 --no-self-contained -p:EnableWindowsTargeting=true`.
  - [ ] Validação do executável publicado e commit limpo no repositório.

---

## 7. Quadro Resumo de Progresso

| Módulo / Fase | Itens | Status Atual | Próximo Passo |
| :--- | :---: | :---: | :--- |
| **1. Produtos em Falta & Compras** | 5 Etapas | ✅ **CONCLUÍDO (Entregue)** | Integrado ao Estoque, Sidebar, PDF e Baixa NF-e |
| **2. Gestão de Ferramental** | 6 Etapas | 🚀 **Pronto para iniciar** | Criar models `Ferramenta.cs` e tabela `MovimentacoesFerramentas` |
| **3. Copilot de IA Dual-Engine** | 5 Etapas | ⏳ Aguardando Fase 2 | Criar interface `IAIService`, cliente Gemini e painel `CopilotFlyoutPanel` |
| **4. Alertas & Notificações Shell** | 1 Etapa | ⏳ Planejado | Integrar eventos no `ShellNotificationService` |
| **5. QA, Dark/Light & Deploy** | 1 Etapa | ⏳ Planejado | Validação xUnit, compilação de release e push GitHub |

---
*Este documento deve ser atualizado marcando as caixas `[x]` a cada etapa concluída e commit realizado.*
