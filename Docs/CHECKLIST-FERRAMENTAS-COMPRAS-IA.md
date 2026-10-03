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
- [x] Criar enum `StatusFerramenta` (`Disponivel`, `EmUso`, `EmManutencao`, `Avariada`, `Extraviada`) em `PrimoAutoEletrica/Models/Ferramenta.cs`.
- [x] Criar enum `CategoriaFerramenta` (`DiagnosticoEletronico`, `MedicaoEletrica`, `BateriasECarga`, `EletricaEMontagem`, `MecanicaGeral`).
- [x] Criar modelo `Ferramenta.cs`: `Id`, `CodigoPatrimonio`, `Nome`, `Categoria`, `MarcaModelo`, `NumeroSerie`, `LocalizacaoArmario`, `Status`, `ValorAquisicao`, `DataAquisicao`, `RequerCalibracaoPeriodica`, `IntervaloCalibracaoDias`, `UltimaCalibracao`, `ProximaCalibracao`, `FuncionarioPosseAtualId`, `FuncionarioPosseAtualNome`, `OrdemServicoAtualId`, `NumeroOSAtual`, `Observacoes`, `Ativo`.
- [x] Criar modelo `MovimentacaoFerramenta.cs`: `Id`, `FerramentaId`, `FuncionarioId`, `OrdemServicoId`, `DataRetirada`, `PrevisaoDevolucao`, `DataDevolucao`, `EstadoConservacaoRetirada`, `EstadoConservacaoDevolucao`, `ObservacaoDevolucao`, `RegistradoPor`.
- [x] Adicionar migração de banco de dados em `DatabaseService.Migrations.cs` para tabelas `Ferramentas` e `MovimentacoesFerramentas` com índices adequados.

> **O que se espera no final da Etapa 2.1:**  
> Estrutura completa de banco de dados e entidades prontas para rastrear patrimônio e histórico de retiradas.

---

### Etapa 2.2: Serviço de Negócio de Ferramentas e Calibração
- [x] Criar `PrimoAutoEletrica/Services/FerramentaService.cs`:
  - [x] `Task<List<Ferramenta>> ListarFerramentasAsync(FiltroFerramenta filtro)`
  - [x] `Task RegistrarRetiradaAsync(Guid ferramentaId, Guid funcionarioId, Guid? ordemServicoId, string estadoConservacao)`:
    - Validar se a ferramenta está `Disponivel`.
    - Atualizar status para `EmUso` e registrar vínculo de posse e OS.
    - Criar registro em `MovimentacoesFerramentas`.
  - [x] `Task RegistrarDevolucaoAsync(Guid ferramentaId, string estadoConservacao, string observacoes)`:
    - Atualizar status para `Disponivel` (ou `Avariada` se reportado defeito).
    - Desvincular funcionário e OS.
    - Fechar registro em `MovimentacoesFerramentas` com data/hora de devolução.
  - [x] `Task<List<Ferramenta>> ObterFerramentasComCalibracaoVencendoAsync(int diasAlerta = 7)`
  - [x] `Task<List<Ferramenta>> ObterFerramentasPendentesOSAsync(Guid ordemServicoId)`
  - [x] `Task<List<MovimentacaoFerramenta>> ObterHistoricoMovimentacaoAsync(Guid ferramentaId)`
- [x] Escrever testes unitários em `Tests/PrimoAutoEletrica.Tests/FerramentasAICopilotTests.cs`.

> **O que se espera no final da Etapa 2.2:**  
> Regras de negócio estritas que não permitem duas pessoas pegarem a mesma ferramenta, mantêm histórico para auditoria e controlam datas de calibração.

---

### Etapa 2.3: Interface de Usuário — Painel Visual de Ferramentas
- [x] Criar `PrimoAutoEletrica/UserControls/FerramentasControl.xaml` e `.xaml.cs`:
  - [x] Barra superior com contadores visuais: Total de Ferramentas, Disponíveis (Verde), Em Uso (Amarelo), Em Manutenção/Calibração (Azul), Avariadas/Atenção (Vermelho).
  - [x] Abas de visualização:
    - **Visão em Grade de Armários / Cartões**: Cards visuais com ícone da categoria, código de patrimônio, nome do equipamento, badge de status e foto/localização (ex: "Armário A - Gaveta 2").
    - **Visão em Lista / Tabela Detalhada**: `PremiumDataGrid` com ordenação por coluna, filtros por categoria, status e busca textual.
  - [x] Indicador visual nos cards em uso: Nome e foto do técnico que retirou + link clicável da OS em atendimento.
  - [x] Botão de Ação Rápida no topo: **"Registrar Retirada / Devolução (F2)"** e **"Cadastrar Nova Ferramenta"**.
- [x] Integrar nova opção de navegação no menu principal / Sidebar do PRIMOX com ícone apropriado (`Wrench` / `Geo.Tech`).
- [x] Garantir 100% de conformidade com o Dark Mode e Light Mode.

> **O que se espera no final da Etapa 2.3:**  
> Um painel moderno estilo "oficina 4.0", onde qualquer colaborador ou gerente bate o olho na tela e sabe exatamente onde cada scanner ou osciloscópio está guardado ou quem está usando.

---

### Etapa 2.4: Modal de Empréstimo Rápido e Leitura por Código de Barras
- [x] Criar `PrimoAutoEletrica/Views/EmprestarDevolverFerramentaDialog.xaml` e `.xaml.cs`:
  - [x] Caixa de texto em foco imediato com suporte a leitor de código de barras ou bip de QR Code.
  - [x] Ao bipar o código:
    - Se a ferramenta estiver **Disponível**: Preenche automaticamente os dados do equipamento, solicita selecionar o Técnico (com atalhos rápidos) e a OS (opcional), e confirma a retirada com `Enter`.
    - Se a ferramenta estiver **Em Uso**: Entende que é um ato de devolução; exibe quem retirou, há quanto tempo está fora, pergunta o estado de conservação (`OK`, `Necessita Limpeza`, `Avariada`) e confirma com `Enter`.
  - [x] Tempo total de operação projetado: **Menos de 4 segundos**.

> **O que se espera no final da Etapa 2.4:**  
> Retirada e devolução ultra-ágeis no balcão de ferramentas que não geram atrito nem atrasam o fluxo da oficina mecânica.

---

### Etapa 2.5: Trava de Segurança Anti-Extravio no Fechamento de OS
- [x] No `PrimoAutoEletrica/Views/OrdemServicoWindow.xaml.cs`:
  - [x] Interceptar o evento de mudança de status para `Concluída`, `Faturada`, `Pronta para entrega` ou `Entregue`.
  - [x] Invocar `FerramentaService.ObterFerramentasPendentesOSAsync(os.Id)`.
  - [x] Se retornar um ou mais itens:
    - Bloquear o encerramento da OS ou alertar o operador de forma mandatória.
    - Exibir diálogo de alerta de alta prioridade (Modal com estilo Warning/Danger do PRIMOX).

> **O que se espera no final da Etapa 2.5:**  
> Fim definitivo do pesadelo de deixar ferramentas caras dentro do cofre do motor ou painel do carro do cliente.

---

### Etapa 2.6: Integração com Auto Elétrica Técnica e Notificação de Fim de Turno
- [x] Criar `PrimoAutoEletrica/Views/CadastroFerramentaDialog.xaml` e `.xaml.cs` para inclusão/edição ágil.
- [x] Criar `PrimoAutoEletrica/Views/HistoricoFerramentasDialog.xaml` e `.xaml.cs` para auditoria total de empréstimos.
- [x] Configurar checagem em segundo plano no `MainWindow_Loaded` para emitir notificação caso ferramentas estejam pendentes de devolução.

> **O que se espera no final da Etapa 2.6:**  
> Sinergia total entre a área técnica e a organização física da oficina.

---

## 4. PILAR 3: Copilot de IA Dual-Engine (Operacional + Especialista Elétrico)

### Etapa 3.1: Arquitetura de Provedores de IA (Gemini + Local Offline + Fallback)
- [x] Criar interface `IAIService` em `PrimoAutoEletrica/Services/AI/IAIService.cs`:
  - `Task<AIChatResponse> ProcessarMensagemAsync(AIChatRequest request, CancellationToken cancellationToken = default);`
  - `bool IsOnlineAvailable { get; }`
  - `string ProviderName { get; }`
- [x] Criar modelos em `PrimoAutoEletrica/Models/AI/`:
  - `AIChatMessage.cs` (Role: User, Assistant, System, Tool; Content; ToolCalls; SuggestedActions; Timestamp).
  - `AIChatRequest.cs` (Histórico de mensagens, contexto ativo do PRIMOX).
  - `AIChatResponse.cs` (Texto da resposta, Ações executáveis/Function Calls, Sugestões de botões).
- [x] Implementar `GeminiAIService.cs`:
  - Comunicação HTTPS com a API do Google Gemini (`gemini-2.0-flash`).
  - Suporte a System Instructions ricas especializadas em auto elétrica e ERP.
  - Fallback resiliente automático para o motor offline se a nuvem oscilar.
- [x] Implementar `DeterministicFallbackAIService.cs`:
  - Motor determinístico local respondendo mesmo sem internet ou sem chave de API configurada.

> **O que se espera no final da Etapa 3.1:**  
> Infraestrutura de IA robusta que nunca trava o sistema: usa nuvem de alta velocidade se disponível, e fallback offline inteligente caso a internet caia.

---

### Etapa 3.2: Interface Retrátil — Copilot Flyout Panel (Ctrl + I)
- [x] Criar `PrimoAutoEletrica/UserControls/CopilotFlyoutPanel.xaml` e `.xaml.cs`:
  - [x] Painel lateral retrátil posicionado na borda direita de `MainWindow.xaml` (largura padrão: 420px).
  - [x] Botão de abertura com ícone de IA no cabeçalho superior do PRIMOX ao lado do Ctrl+K.
  - [x] Atalho de teclado global: `Ctrl + I` para abrir e fechar a qualquer momento.
  - [x] Cabeçalho do Copilot:
    - Indicador visual de modo (`Gemini Nuvem` ou `Offline Técnico`).
    - Botão de limpar histórico e botão de fechar.
  - [x] Área de conversa com rolagem:
    - Balões de mensagens com distinção visual clara (usuário vs Copilot).
    - Chips/Badges de prompts rápidos no topo (DTC P0562, Fuga de Corrente, Relé, Peças em Falta, Ferramentas).
  - [x] Caixa de entrada de texto inferior:
    - `TextBox` expansível com atalho `Enter` (com `Shift+Enter` para nova linha).
- [x] 100% estilizado com `Colors.Dark.xaml` e `Colors.Light.xaml` para harmonia total com o resto do aplicativo.

> **O que se espera no final da Etapa 3.2:**  
> Uma experiência fluida e nativa de assistente embutido, moderna e bonita, acessível a qualquer momento sem trocar de janela.

---

### Etapa 3.3: Motor 1 — Automação ERP via Function Calling
- [x] Criar `PrimoAutoEletrica/Services/AI/AIToolRegistry.cs`:
  - Definição do catálogo de ferramentas expostas para a IA:
    1. `NavegarParaModulo(string modulo)` — Abre Financeiro, Estoque, Ordens de Serviço, Ferramentaria, Compras, etc.
    2. `ConsultarEstoque(string termo)` — Retorna quantidade em estoque, prateleira, preço de venda.
    3. `ConsultarProdutosEmFalta()` — Retorna lista de peças em ponto de pedido/ruptura.
    4. `ConsultarFerramentasEmUso()` — Retorna quais ferramentas estão livres ou com qual técnico estão.
    5. `BuscarClienteVeiculo(string termo)` — Busca rápida por placa, modelo, nome ou CPF.
    6. `ConsultarDiagnosticoEletrico(string codigoOuSintoma)` — Procedimento técnico guiado.

> **O que se espera no final da Etapa 3.3:**  
> O operador pode controlar o sistema por linguagem natural e receber respostas consolidadas em segundos.

---

### Etapa 3.4: Motor 2 — RAG Técnico e Diagnóstico Elétrico Automotivo
- [x] Criar `PrimoAutoEletrica/Services/AI/AutomotiveDiagnosticRAGService.cs`:
  - Indexar base de códigos DTC padrão OBD-II com foco em elétrica:
    - *P0562 e P0563* (Falhas de Tensão do Sistema Baixa / Alta).
    - *P0620* (Controle de Campo do Alternador / Regulador).
    - *P0335* (Sensor de Rotação CKP indutivo vs hall).
    - *P0300* (Falhas de Ignição / Centelha no osciloscópio).
    - *CONSUMO_PARASITA* (Procedimento completo de medição de fuga de corrente em repouso < 50mA com fusíveis e sleep mode).
    - *TESTE_RELE* (Pinagem DIN 72552: 30, 85, 86, 87, 87a e testes de bobina e resistência de contato).
    - *REDE_CAN* (Diagnóstico físico de barramento CAN Bus, terminação de 60 Ohms e tensões médias).

> **O que se espera no final da Etapa 3.4:**  
> O técnico obtém um passo a passo técnico com parâmetros nominais de tensão, corrente, resistência e peças prováveis de reposição.

---

### Etapa 3.5: Ações Rápidas (One-Click) e Inserção em Orçamentos/OS
- [x] Adicionar suporte a componentes interativos dentro do `CopilotFlyoutPanel`:
  - [x] Botões de ação rápida contextuais dentro dos balões da IA:
    - `[Ir para Módulo]`
    - `[Ver Peças no Estoque]`
    - `[Copiar Procedimento]`
    - `[Acessar Ferramentaria]`

> **O que se espera no final da Etapa 3.5:**  
> O diagnóstico da IA se traduz instantaneamente em navegação e ação prática dentro do sistema.

---

## 5. PILAR 4: Notificações Shell e Alertas Globais

- [x] Integrar os novos módulos ao `PrimoAutoEletrica/Services/ShellNotificationService.cs`:
  - [x] Notificação de **Ruptura Imediata**: Disparada em segundo plano caso produtos críticos estejam travando OSs.
  - [x] Notificação de **Retenção de Ferramenta**: Disparada em segundo plano se houver ferramentas com devolução vencida.
  - [x] Suporte a ações rápidas com navegação direta com 1 clique a partir do Toast.

> **O que se espera no final do Pilar 4:**  
> Sistema proativo que avisa problemas antes que virem prejuízos para o dono da oficina.

---

## 6. PILAR 5: Testes, Homologação e Definição de Pronto (DoD)

- [x] **Testes de Integração e Regressão**:
  - [x] Testes unitários para Gestão de Compras (`Tests/PrimoAutoEletrica.Tests/GestaoComprasTests.cs`).
  - [x] Testes unitários para Ferramentaria & AI Copilot (`Tests/PrimoAutoEletrica.Tests/FerramentasAICopilotTests.cs`).
- [x] **Auditoria Visual Dark & Light Mode**:
  - [x] Todas as novas telas criadas com brushes dinâmicos (`Colors.Dark.xaml` e `Colors.Light.xaml`).
  - [x] Zero controles com fundo branco órfão ou texto invisível no modo escuro.
- [x] **Build & Deploy**:
  - [x] Compilação com zero erros.
  - [x] Publicação de release para `win-x64` em `bin/Release/net6.0-windows/win-x64/publish/`.

---

## 7. Quadro Resumo de Progresso

| Módulo / Fase | Itens | Status Atual | Próximo Passo |
| :--- | :---: | :---: | :--- |
| **1. Produtos em Falta & Compras** | 5 Etapas | ✅ **CONCLUÍDO (100%)** | Operacional e testado |
| **2. Gestão de Ferramental** | 6 Etapas | ✅ **CONCLUÍDO (100%)** | Armários visuais, Barcode, Trava OS e Histórico |
| **3. Copilot de IA Dual-Engine** | 5 Etapas | ✅ **CONCLUÍDO (100%)** | Gemini REST, Motor RAG Offline e Ctrl+I Flyout |
| **4. Alertas & Notificações Shell** | 1 Etapa | ✅ **CONCLUÍDO (100%)** | Notificações ativas no Toast e background |
| **5. QA, Dark/Light & Deploy** | 1 Etapa | ✅ **CONCLUÍDO (100%)** | Publicado win-x64 com zero erros |

---
*Documento atualizado com 100% de conclusão de todos os 3 pilares estratégicos.*
