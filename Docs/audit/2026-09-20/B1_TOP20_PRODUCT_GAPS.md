# PRIMOX WORKSHOP — TOP 20 LACUNAS DO PRODUTO (FASE B1)
**Data de Auditoria:** 2026-09-20 / 2026-09-23  
**Branch de Auditoria:** `audit/product-discovery-2026-09`  
**Critério de Ordenação:** Rigorosamente ordenado por Impacto Funcional, Grau de Dependência e Risco Técnico. Não é um ranking de popularidade, e sim o mapa objetivo de trabalho para as fases subsequentes da Trilha B.

---

## Tabela Resumo das Top 20 Lacunas

| # | Lacuna / Oportunidade Técnica | Módulo | Classificação | Impacto Funcional | Risco Técnico |
| :---: | :--- | :--- | :---: | :---: | :---: |
| **01** | Emissão Fiscal NF-e Produção SEFAZ Bloqueada | Fiscal | `EXTERNAL_DEPENDENCY` | CRÍTICO | ALTO |
| **02** | Licenciamento Comercial / Servidor SaaS Ausente | Licenciamento | `MOCK` | CRÍTICO | ALTO |
| **03** | Roteiros de Diagnóstico sem Associação a OS/Veículo | Autoelétrica | `PARTIAL` | ALTO | MÉDIO |
| **04** | Catálogo de Procedimentos de Diagnóstico Hardcoded | Autoelétrica | `PARTIAL` | ALTO | BAIXO |
| **05** | DVI Persistido em JSON Local sem Assinatura Remota | DVI | `PARTIAL` | ALTO | MÉDIO |
| **06** | Contas a Pagar Sem Chave `FornecedorId` | Financeiro | `CORE` (Risco ID) | MÉDIO | MÉDIO |
| **07** | Duplicidade de Cadastro de Perfil RBAC (`Mecanico`) | Segurança / RBAC | `CORE` (Dado) | MÉDIO | BAIXO |
| **08** | Endpoints REST Ausentes para OS, Clientes e Veículos | API REST | `PARTIAL` | MÉDIO | BAIXO |
| **09** | Integração WhatsApp Limitada a Link Local (`wa.me`) | Comunicação | `PARTIAL` | MÉDIO | BAIXO |
| **10** | Ausência de Confirmação Automática de Pix (Webhook) | Financeiro / PDV | `NOT_IMPLEMENTED` | MÉDIO | MÉDIO |
| **11** | Ausência de TEF Dedicado para Cartões de Crédito/Débito | Caixa / PDV | `NOT_IMPLEMENTED` | MÉDIO | MÉDIO |
| **12** | Botões Sem Ação em `OrcamentosView.xaml` (`Email`, `Venda`) | Orçamentos | `UI_ONLY` | BAIXO | BAIXO |
| **13** | Janela de Transferência de Estoque entre Filiais Inoperante | Estoque | `UI_ONLY` | BAIXO | BAIXO |
| **14** | Exportação SPED Fiscal Incompleta (`ContabilExportService`) | Relatórios | `PARTIAL` | MÉDIO | MÉDIO |
| **15** | Suíte `UiTests` Apontando para Executável `net6.0` Antigo | Qualidade / CI | `PARTIAL` | BAIXO | BAIXO |
| **16** | Ausência de Envio de Notificações por Email / SMS | Comunicação | `NOT_IMPLEMENTED` | BAIXO | BAIXO |
| **17** | Cores Hardcoded Pontuais em Janelas do Design System | UI / Design | `CORE` (Polimento) | BAIXO | BAIXO |
| **18** | Tabela `Auditoria` Órfã no Banco de Dados | Banco de Dados | `CORE` (Resíduo) | BAIXO | MUITO BAIXO |
| **19** | Tabelas de Bloqueio Concorrente Duplicadas | Banco de Dados | `CORE` (Resíduo) | BAIXO | MUITO BAIXO |
| **20** | Ausência de Sincronização em Nuvem (Cloud Sync) | Infraestrutura | `NOT_IMPLEMENTED` | ALTO (Longo Prazo)| ALTO |

---

## Detalhamento Técnico das Top 20 Lacunas

### GAP 01: Emissão Fiscal NF-e em Produção SEFAZ Bloqueada
* **Módulo:** Fiscal
* **Classificação:** `EXTERNAL_DEPENDENCY`
* **Descrição:** A fundação fiscal possui arquitetura moderna com provedores Focus NFe e PlugNotas, gerador de DANFE informativo, importador de XML e homologação operacional. No entanto, a emissão em produção está hard-blocked pela classe `FiscalProductionGuard.cs` (`ProductionEmissionAllowed = false`).
* **Impacto Funcional:** Impossibilita o cliente final emitir notas com valor jurídico contra a SEFAZ de produção sem configuração contratual de credenciais e liberação controlada da trava de guarda.
* **Dependência:** Contratação formal de gateway fiscal, certificado digital A1 válido do cliente e desativação programada do guard.
* **Ação Recomendada na Trilha B:** Manter homologação ativa e implementar a esteira de parametrização segura de certificado A1.

---

### GAP 02: Licenciamento Comercial / Servidor SaaS Ausente
* **Módulo:** Licenciamento
* **Classificação:** `MOCK` / `NOT_IMPLEMENTED`
* **Descrição:** O arquivo `LicenseService.cs` documenta explicitamente: `IsCommercialScaffoldOnly = true`. A ativação atual baseia-se em arquivo JSON local e hash SHA-256 de identificadores da máquina.
* **Impacto Funcional:** Não há proteção criptográfica assimétrica real (chaves públicas/privadas RSA ou ECDSA), nem validação de renovação/inadimplência contra servidor em nuvem.
* **Dependência:** Construção de microserviço de licenciamento com emissão de tokens assinados e rota de heartbeat.
* **Ação Recomendada na Trilha B:** Projetar o módulo comercial de licenciamento desvinculado do código operacional da oficina.

---

### GAP 03: Resultados de Diagnóstico Elétrico Sem Vínculo a OS/Veículo
* **Módulo:** Autoelétrica Técnica
* **Classificação:** `PARTIAL`
* **Descrição:** O serviço `AutoEletricaRoteiroPersistService.cs` salva a conclusão dos roteiros guiados em um arquivo JSON único (`AppData/AutoEletrica/roteiros-resultados.json`) indexado apenas por `Codigo` do roteiro (ex: "D01"), sem vincular a chave primária da OS em execução nem a placa do veículo testado.
* **Impacto Funcional:** Se dois veículos passarem pelo mesmo roteiro de bateria, o segundo sobrescreve o resultado do primeiro no arquivo global, inviabilizando auditoria pericial por OS.
* **Dependência:** Criação de estrutura de persistência para execuções técnicas associada a `OrdemServicoId` e `VeiculoId`.
* **Ação Recomendada na Trilha B:** Unificar os resultados de diagnósticos dentro da entidade `OrdemServico` ou tabela filha.

---

### GAP 04: Catálogo de Procedimentos de Diagnóstico Hardcoded em Código
* **Módulo:** Autoelétrica Técnica
* **Classificação:** `PARTIAL`
* **Descrição:** O método `AutoEletricaTecnicaService.ObterRoteirosDiagnostico()` possui os procedimentos D01 a D06 codificados de forma estática em C# (bateria descarregando, motor de partida, alternador, etc.).
* **Impacto Funcional:** O eletricista ou chefe de oficina não pode customizar procedimentos, incluir novos testes ou adaptar o catálogo às particularidades da sua oficina.
* **Dependência:** Criação de tabela de catálogo de procedimentos de diagnóstico no banco de dados.
* **Ação Recomendada na Trilha B:** Migrar a lista estática para tabela SQLite editável via interface administrativa.

---

### GAP 05: DVI Persistido em JSON Local sem Assinatura Remota do Cliente
* **Módulo:** DVI (Digital Vehicle Inspection)
* **Classificação:** `PARTIAL`
* **Descrição:** O checklist de entrada/saída, conformidade e fotos de avarias funciona perfeitamente, mas é persistido como arquivo JSON em disco (`AppData/Dvi/`). Não há link web gerado para envio ao smartphone do cliente para aprovação remota com assinatura na tela.
* **Impacto Funcional:** A conferência de entrada exige que o cliente esteja fisicamente na recepção da oficina ou aprove por mensagem de texto informal.
* **Dependência:** Portal web de aprovação ou upload de documento em nuvem.
* **Ação Recomendada na Trilha B:** Estruturar tabela SQLite para o DVI e gerar página web/PDF com link direto para aceite.

---

### GAP 06: `ContasPagar` Sem Chave Estrangeira `FornecedorId`
* **Módulo:** Financeiro
* **Classificação:** `CORE` com `RISK — IDENTITY ASSOCIATION`
* **Descrição:** A tabela `ContasPagar` armazena o campo `Fornecedor` como string de texto puro (nome), ao passo que a tabela `Fornecedores` possui chave primária numérica própria.
* **Impacto Funcional:** Risco de inconsistência analítica se a razão social ou nome fantasia do fornecedor for alterado; contas a pagar antigas ficam desvinculadas da ficha cadastral do fornecedor.
* **Dependência:** Migração de schema com adição da coluna `FornecedorId` e rotina de reconciliação de dados.
* **Ação Recomendada na Trilha B:** Adicionar `FornecedorId` na tabela `ContasPagar` em fase de expansão de schema.

---

### GAP 07: Duplicidade de Cadastro de Perfil RBAC por Falha de Encoding
* **Módulo:** Segurança & RBAC
* **Classificação:** `CORE` (Dado com anomalia)
* **Descrição:** A tabela `PerfisAcesso` possui o registro ID 3 (`Mecanico`) e o registro ID 4 (`Mecânico` com diacrítico). Ambos apontam para descrições similares, dividindo as permissões associadas em `PerfilPermissoes`.
* **Impacto Funcional:** Confusão administrativa na atribuição de cargos aos funcionários da oficina.
* **Dependência:** Script de higienização de banco para unificar os dois perfis sob o ID canônico.
* **Ação Recomendada na Trilha B:** Saneamento cadastral na Trilha B.

---

### GAP 08: Endpoints REST Incompletos no Projeto API
* **Módulo:** API REST
* **Classificação:** `PARTIAL`
* **Descrição:** A API Minimal ASP.NET Core em `PrimoAutoEletrica.Api` cobre autenticação JWT, health check, orçamentos, produtos de estoque e resumo financeiro. Não foram construídos endpoints para Ordens de Serviço, Clientes, Veículos e Operações Fiscais.
* **Impacto Funcional:** Impossibilita o desenvolvimento de aplicativo móvel para o mecânico no box da oficina ou integração com ERP externo sem que a API seja complementada.
* **Dependência:** Criação de rotas adicionais em `Program.cs` da API.
* **Ação Recomendada na Trilha B:** Implementar endpoints REST para OS e Clientes.

---

### GAP 09: Integração WhatsApp Limitada a Protocolo Local (`wa.me`)
* **Módulo:** Comunicação com Cliente
* **Classificação:** `PARTIAL`
* **Descrição:** O sistema abre o navegador ou app WhatsApp Desktop pré-preenchendo mensagem e telefone via URL `https://wa.me/{numero}?text={msg}`.
* **Impacto Funcional:** O operador precisa clicar manualmente em "Enviar" na janela do WhatsApp, impedindo automações em lote (ex: lembretes de revisão para 50 clientes).
* **Dependência:** Integração com API oficial do WhatsApp Business ou broker terceirizado.
* **Ação Recomendada na Trilha B:** Manter deep-link como fallback e avaliar conector de gateway para mensagens em lote.

---

### GAP 10: Ausência de Gateway de Pagamento Automático (Pix com Webhook)
* **Módulo:** Caixa / PDV / Financeiro
* **Classificação:** `NOT_IMPLEMENTED`
* **Descrição:** As vendas e pagamentos via Pix são lançados manualmente pelo operador no PDV após visualização do comprovante no celular da oficina.
* **Impacto Funcional:** Lentidão no atendimento de balcão e risco de golpe de falso comprovante.
* **Dependência:** Conexão com API Pix (Banco Central / Bacen / PSP) com exibição de QR Code dinâmico na tela e confirmação por webhook.
* **Ação Recomendada na Trilha B:** Desenvolver módulo Pix dinâmico no PDV.

---

### GAP 11: Ausência de Integração com TEF Dedicado
* **Módulo:** Caixa / PDV
* **Classificação:** `NOT_IMPLEMENTED`
* **Descrição:** Vendas no cartão de crédito/débito utilizam maquininhas independentes (POS). O operador digita o valor na maquininha e seleciona a forma de pagamento no sistema.
* **Impacto Funcional:** Risco de erro de digitação de valores no POS e necessidade de conferência cega no fechamento de caixa.
* **Dependência:** Conector com gerenciador padrão de TEF (ex: SiTef, Cappta, Getcard).
* **Ação Recomendada na Trilha B:** Avaliar demanda de TEF frente ao perfil das oficinas clientes.

---

### GAP 12: Botões Sem Ação em `OrcamentosView.xaml` (`UI_ONLY`)
* **Módulo:** Orçamentos
* **Classificação:** `UI_ONLY`
* **Descrição:** A janela `OrcamentosView.xaml` possui dois botões visíveis no topo: `<Button Content="Email".../>` e `<Button Content="Converter em venda".../>` que não possuem `Click` handler no codebehind nem `Command` no ViewModel.
* **Impacto Funcional:** Clicar nos botões não surte nenhum efeito visual ou operacional para o usuário.
* **Dependência:** Implementação da ação ou remoção dos botões para evitar frustração do operador.
* **Ação Recomendada na Trilha B:** Limpeza ou conexão ao fluxo de venda.

---

### GAP 13: Janela de Transferência entre Filiais Inoperante (`UI_ONLY`)
* **Módulo:** Estoque
* **Classificação:** `UI_ONLY`
* **Descrição:** A janela `Views/TransferirEstoqueWindow.xaml` possui apenas 39 linhas de XAML sem ViewModel, sem serviço de transferência e a tabela `Filiais` no banco possui 0 registros.
* **Impacto Funcional:** Funcionalidade órfã no menu de estoque.
* **Dependência:** Estruturação de arquitetura multi-loja.
* **Ação Recomendada na Trilha B:** Ocultar a opção até que a arquitetura multi-loja seja priorizada.

---

### GAP 14: Exportação SPED Fiscal Incompleta
* **Módulo:** Relatórios / Fiscal
* **Classificação:** `PARTIAL`
* **Descrição:** A classe `ContabilExportService.cs:120` possui o comentário explícito: `// TODO: Implementar formato SPED completo (SPED Fiscal, SPED Contábil, etc.)`. Atualmente exporta apenas dados sintéticos em CSV.
* **Impacto Funcional:** Escritórios contábeis de porte médio que exigem o arquivo EFD ICMS/IPI oficial precisam importar manualmente ou por outros meios.
* **Dependência:** Construtor do layout padrão Guia Prático da EFD-ICMS/IPI.
* **Ação Recomendada na Trilha B:** Implementar gerador de blocos EFD padrão.

---

### GAP 15: Suíte `UiTests` Desalinhada do Target .NET 10
* **Módulo:** Qualidade & Testes de Integração UI
* **Classificação:** `PARTIAL`
* **Descrição:** O projeto `PrimoAutoEletrica.UiTests` possui a classe `UiTestBase.cs` procurando o binário em `PrimoAutoEletrica/bin/Debug/net6.0-windows/PrimoAutoEletrica.exe`, ao passo que a solução foi modernizada para `net10.0-windows`. Os 7 testes de UI falham por `FileNotFoundException`.
* **Impacto Funcional:** Não impede o funcionamento do produto real, mas prejudica a execução do comando global `dotnet test`.
* **Dependência:** Correção pontual de caminho no arquivo `UiTestBase.cs`.
* **Ação Recomendada na Trilha B:** Atualizar `UiTestBase.cs` para referenciar o build `net10.0-windows`.

---

### GAP 16: Ausência de Envio de Notificações por Email / SMS
* **Módulo:** Comunicação
* **Classificação:** `NOT_IMPLEMENTED`
* **Descrição:** A oficina não possui disparo de aviso de "Veículo pronto para retirada" ou "Orçamento disponível" via email ou SMS transacional.
* **Impacto Funcional:** Dependência exclusiva do contato telefônico ou WhatsApp manual.
* **Dependência:** Serviço de envio SMTP ou API de mensageria.
* **Ação Recomendada na Trilha B:** Configurar envio de PDF de orçamento e OS por email corporativo.

---

### GAP 17: Cores Hardcoded Pontuais no Design System
* **Módulo:** Interface & Design System
* **Classificação:** `CORE` com pequenos pontos de polimento
* **Descrição:** 5 janelas secundárias possuem cores hexadecimais embutidas (ex: `LoginWindow.xaml`, `PrimeiraExecucaoWindow.xaml`, `ConfigurarPermissoesWindow.xaml`), totalizando 15 ocorrências em toda a aplicação.
* **Impacto Funcional:** Pequenas variações visuais imperceptíveis na grande maioria dos monitores, mas que desviam das diretrizes puras de tokens do tema Dark/Light.
* **Dependência:** Substituição pontual por chaves `DynamicResource`.
* **Ação Recomendada na Trilha B:** Polimento de CSS/XAML nessas janelas.

---

### GAP 18: Tabela `Auditoria` Órfã no Banco de Dados
* **Módulo:** Banco de Dados / Persistência
* **Classificação:** `CORE` (Resíduo de arquitetura)
* **Descrição:** O banco possui a tabela `Auditoria` (0 linhas) e a tabela `AuditLogs` (1.592 linhas). A aplicação moderna utiliza exclusivamente `AuditLogs` para trilha de auditoria crítica.
* **Impacto Funcional:** Nenhum impacto operacional.
* **Dependência:** Script futuro de limpeza de schema.
* **Ação Recomendada na Trilha B:** Documentar a tabela como legada e descontinuada.

---

### GAP 19: Tabelas de Bloqueio Concorrente Duplicadas
* **Módulo:** Banco de Dados / Concorrência
* **Classificação:** `CORE` (Resíduo de arquitetura)
* **Descrição:** O banco possui `RecordLocks` (8 linhas) e `RegistroBloqueios` (0 linhas). A classe `RecordLockService.cs` utiliza exclusivamente `RecordLocks`.
* **Impacto Funcional:** Nenhum impacto operacional.
* **Dependência:** Limpeza de schema.
* **Ação Recomendada na Trilha B:** Manter `RecordLocks` como padrão único.

---

### GAP 20: Falta de Mecanismo de Sincronização em Nuvem (Cloud Sync)
* **Módulo:** Arquitetura / Nuvem
* **Classificação:** `NOT_IMPLEMENTED`
* **Descrição:** O PRIMOX é uma aplicação estritamente desktop local rodando sobre SQLite local ou SQL Server em rede local cabeada/Wi-Fi. Não possui motor de sincronização assíncrona com nuvem (offline-first sync).
* **Impacto Funcional:** Para gerenciar a oficina à distância (ex: dono em casa acompanhando o faturamento no celular), é necessário acesso remoto (AnyDesk/RDP) ou conexão VPN na rede da oficina.
* **Dependência:** Arquitetura de mensageria em nuvem ou replicação de dados.
* **Ação Recomendada na Trilha B:** Avaliar na Trilha B avançada a sincronização de KPIs para painel web gerencial.
