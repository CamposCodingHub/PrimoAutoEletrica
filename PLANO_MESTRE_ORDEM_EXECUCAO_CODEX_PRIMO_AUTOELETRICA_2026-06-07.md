# PLANO MESTRE DE EXECUÇÃO PARA O CODEX
## Projeto: Primo Auto Elétrica — Sistema Desktop WPF para Oficina Auto Elétrica Automotiva

**Data:** 07/06/2026  
**Objetivo deste documento:** servir como uma ordem direta, detalhada e sequencial para o Codex executar melhorias no projeto, sem pular etapas, priorizando estabilidade, testes, correções de erros atuais, qualidade visual, funcionalidades profissionais e diferenciais específicos de auto elétrica automotiva.

---

# 0. ORDEM GERAL OBRIGATÓRIA AO CODEX

Codex, leia este documento inteiro antes de modificar qualquer arquivo.

Você deve trabalhar por fases. Não pule fases. Não misture muitas mudanças ao mesmo tempo. Não implemente funcionalidades novas enquanto o projeto estiver com erro de build, erro de tema, erro de navegação, erro de banco ou falhas graves nos testes.

A ordem obrigatória é:

1. Melhorar o processo de testes, simulações e validação geral.
2. Corrigir os erros atuais detectados no projeto, principalmente erro de tema/Style/Effect.
3. Fazer os testes voltarem a 100%.
4. Criar checkpoint estável.
5. Limpar e organizar o projeto.
6. Refatorar arquivos grandes e frágeis.
7. Melhorar banco de dados, backup e persistência.
8. Melhorar módulos existentes.
9. Melhorar visual e experiência de uso.
10. Implementar recursos profissionais que sistemas semelhantes têm.
11. Implementar diferenciais específicos de auto elétrica automotiva.
12. Preparar o sistema para uso real em oficina.
13. Preparar documentação, instalador e versão comercial futura.

## 0.1 Regras de segurança antes de cada alteração

Antes de alterar qualquer arquivo, execute obrigatoriamente:

- Identifique exatamente quais arquivos serão alterados.
- Explique por que cada arquivo será alterado.
- Faça backup/checkpoint antes de mudanças grandes.
- Altere poucos arquivos por vez.
- Rode build e testes depois de cada bloco de alteração.
- Nunca remova funcionalidades existentes sem justificar.
- Nunca troque nomes de classes, namespaces, controles ou arquivos XAML sem verificar todas as referências.
- Nunca alterar tema global sem rodar teste de todas as telas.
- Nunca alterar DatabaseService sem rodar teste de persistência.
- Nunca alterar Login/Permissões sem rodar teste por perfil.
- Nunca alterar PDV/Financeiro/Estoque sem rodar fluxo completo.

## 0.2 Regra de aceite obrigatório

Uma etapa só pode ser considerada concluída se cumprir:

- `dotnet restore` executado sem erro.
- `dotnet build` executado sem erro.
- Teste de UI executado.
- Teste de workflow executado.
- Teste de tema executado quando houver alteração visual.
- Teste de banco executado quando houver alteração de dados.
- Relatório final gerado com: arquivos alterados, motivo, testes executados, resultado e próximos riscos.

## 0.3 Erros atuais conhecidos que devem ser tratados com prioridade

O projeto apresentou falhas graves no UI Smoke Test, com erro relacionado a tema/estilo/efeito:

```text
'{DependencyProperty.UnsetValue}' não é um valor válido para a propriedade 'Effect'.
```

Arquivos suspeitos/prioritários para investigação:

```text
Themes/Cards.xaml
Themes/Shadows.xaml
Themes/GlobalStyles.xaml
Themes/Colors.Light.xaml
Themes/Colors.Dark.xaml
Themes/Buttons.xaml
Themes/Inputs.xaml
Themes/DataGrid.xaml
App.xaml
Services/ThemeService.cs
```

Objetivo mínimo antes de avançar:

```text
UI Smoke Test = 100% aprovado
Workflow Test = 100% aprovado
Build = 100% aprovado
Tema claro/escuro = 100% aprovado
```

---

# FASE 1 — MELHORAR TESTES, SIMULAÇÕES E PROCESSO DE VALIDAÇÃO

Esta é a primeira fase obrigatória. Não crie novas funcionalidades antes de concluir esta fase.

## 1.1 Criar script único de validação completa

Criar um script em:

```text
Scripts/Run-FullValidation.ps1
```

Esse script deve executar, em ordem:

1. Verificar se está na pasta correta do projeto.
2. Registrar data/hora da execução.
3. Criar pasta de resultado em `TestResults/FullValidation/<data-hora>/`.
4. Executar `dotnet clean`.
5. Executar `dotnet restore`.
6. Executar `dotnet build`.
7. Executar testes unitários existentes.
8. Executar UI Smoke Test.
9. Executar Workflow Test.
10. Executar teste de tema claro/escuro.
11. Executar teste de permissões por perfil.
12. Executar teste de banco/persistência.
13. Coletar logs de erro.
14. Gerar relatório final em `.md` e `.txt`.
15. Retornar código de saída com sucesso somente se tudo passar.

Resultado esperado:

```text
Com um único comando, o dono do projeto deve saber se o sistema está aprovado ou quebrado.
```

Critério de aceite:

- Script criado.
- Script documentado.
- Script executa sem depender de comandos manuais soltos.
- Relatório final mostra aprovado/reprovado por categoria.

## 1.2 Melhorar o UI Smoke Test

Localize o serviço atual de UI Smoke Test. Ele provavelmente está muito grande e precisa ser melhorado, mas primeiro garanta que funcione.

O UI Smoke deve testar todas as telas principais:

- Login.
- Dashboard.
- Clientes.
- Veículos.
- Orçamentos.
- Ordens de Serviço.
- PDV.
- Estoque.
- Importar NF-e.
- Financeiro.
- Fornecedores.
- Funcionários.
- Agendamentos.
- Relatórios.
- Configurações, se existir.
- Tela de troca de tema, se existir.
- Modais principais.
- Janelas de cadastro, edição, visualização e histórico.

Para cada tela, o teste deve verificar:

- A tela abre sem exceção.
- O construtor não lança erro.
- `InitializeComponent` funciona.
- Não existe `StaticResource` ausente.
- Não existe `Style` inválido.
- Não existe `Effect` inválido.
- Não existe Binding crítico quebrado.
- Os botões principais existem.
- Os campos obrigatórios existem.
- O DataGrid aparece com colunas.
- O ScrollViewer não corta botões importantes.
- A tela se comporta em janela maximizada.
- A tela se comporta em tamanho mínimo aceitável.
- Textos não ficam com a mesma cor do fundo.
- Botões não ficam invisíveis.
- Headers seguem o mesmo padrão visual.

Critério de aceite:

- UI Smoke cobre todas as telas.
- Relatório aponta exatamente qual tela quebrou.
- Relatório aponta o controle/arquivo provável.
- Relatório separa erro visual de erro funcional.

## 1.3 Criar teste específico de tema e ResourceDictionary

Criar teste novo para validar somente temas, estilos e recursos.

Nome sugerido:

```text
ThemeResourceValidationService.cs
```

Ou, se já existir estrutura de testes:

```text
Tests/ThemeResourceValidationTests.cs
```

Esse teste deve validar:

- Todos os arquivos `Themes/*.xaml` carregam sem exceção.
- Todas as chaves usadas em `StaticResource` existem.
- Todos os `DynamicResource` críticos existem.
- Nenhum `BasedOn` aponta para estilo inexistente.
- Nenhum `Effect` recebe `UnsetValue`.
- Nenhum `DropShadowEffect` está quebrado.
- Nenhum brush principal está nulo.
- Nenhum estilo global sobrescreve estilo de forma perigosa.
- Tema claro carrega.
- Tema escuro carrega.
- Alternar claro → escuro → claro não quebra.
- Cards, botões, inputs, DataGrid e modais usam estilos válidos.

Arquivos que devem ser testados:

```text
Themes/Cards.xaml
Themes/Shadows.xaml
Themes/GlobalStyles.xaml
Themes/Colors.Light.xaml
Themes/Colors.Dark.xaml
Themes/Buttons.xaml
Themes/Inputs.xaml
Themes/DataGrid.xaml
Themes/Modern.xaml
Themes/ScrollBars.xaml
App.xaml
```

Critério de aceite:

- O erro de `DependencyProperty.UnsetValue` precisa ser detectado por esse teste antes de chegar no UI Smoke.
- Se uma chave de tema estiver faltando, o teste deve dizer qual chave está faltando.

## 1.4 Melhorar Workflow Test para simular oficina real

O Workflow Test atual não deve apenas clicar em botões. Ele deve provar que o sistema funciona como uma oficina.

Criar ou melhorar teste de fluxo completo:

```text
Fluxo: Cliente → Veículo → Orçamento → OS → Estoque → PDV/Caixa → Financeiro → Histórico
```

Passos obrigatórios do teste:

1. Abrir sistema.
2. Fazer login como Administrador.
3. Cadastrar cliente de teste.
4. Validar se cliente foi salvo no banco.
5. Cadastrar veículo vinculado ao cliente.
6. Validar se veículo aparece dentro do cliente.
7. Cadastrar fornecedor.
8. Cadastrar produto/peça.
9. Dar entrada de estoque.
10. Validar quantidade em estoque.
11. Criar orçamento para o cliente e veículo.
12. Adicionar peça ao orçamento.
13. Adicionar mão de obra ao orçamento.
14. Aplicar desconto.
15. Validar cálculo de subtotal, desconto e total.
16. Alterar status para enviado.
17. Alterar status para aprovado.
18. Converter orçamento em OS.
19. Validar se OS herda dados do orçamento.
20. Colocar OS em execução.
21. Reservar peça do estoque.
22. Validar reserva.
23. Finalizar OS.
24. Baixar estoque definitivamente.
25. Abrir caixa.
26. Receber pagamento em dinheiro.
27. Receber pagamento PIX.
28. Testar pagamento misto.
29. Validar lançamento no financeiro.
30. Validar histórico do cliente.
31. Validar histórico do veículo.
32. Gerar comprovante ou relatório simples.
33. Fechar caixa.
34. Reabrir sistema.
35. Validar persistência dos dados.
36. Limpar dados de teste ou marcar como ambiente de teste.

Critério de aceite:

- O teste precisa verificar dados reais no banco, não apenas tela aberta.
- Se o estoque não baixar, reprovar.
- Se financeiro não lançar, reprovar.
- Se histórico não aparecer, reprovar.
- Se orçamento convertido perder dados, reprovar.

## 1.5 Criar teste de permissões por perfil

Perfis mínimos:

- Administrador.
- Gerente.
- Mecânico/Técnico.
- Vendedor/Atendente.
- Caixa.
- Almoxarife/Estoque.

O teste deve validar:

- Administrador acessa tudo.
- Caixa acessa PDV e Caixa, mas não acessa Funcionários, Configurações críticas e permissões.
- Mecânico acessa OS, veículos, checklist e diagnóstico, mas não acessa financeiro completo.
- Almoxarife acessa estoque e fornecedores, mas não altera caixa/financeiro sensível.
- Vendedor acessa clientes, veículos, orçamentos e agenda.
- Gerente acessa relatórios e financeiro, mas pode ter restrições de configuração se necessário.

Critério de aceite:

- Cada menu deve ser testado por perfil.
- Cada botão crítico deve ser testado por perfil.
- Acesso negado deve ser amigável e registrado em log.

## 1.6 Criar teste de banco de dados e persistência

Criar testes para:

- Criar banco limpo em ambiente temporário.
- Criar tabelas.
- Inserir cliente.
- Editar cliente.
- Excluir/desativar cliente.
- Inserir veículo.
- Vincular veículo ao cliente.
- Inserir produto.
- Movimentar estoque.
- Criar fornecedor.
- Criar orçamento.
- Criar OS.
- Criar venda.
- Criar lançamento financeiro.
- Fechar conexão.
- Abrir conexão novamente.
- Conferir se tudo persistiu.

Critério de aceite:

- Não pode depender do banco real do usuário.
- Deve usar banco temporário de teste.
- Deve testar chaves estrangeiras.
- Deve testar dados inválidos.
- Deve testar valores nulos.

## 1.7 Criar captura automática de telas

Criar processo de screenshots automáticos das telas principais.

Salvar em:

```text
TestResults/Screenshots/<data-hora>/
```

Capturar:

- Login.
- Dashboard.
- Clientes.
- Cadastro de cliente.
- Veículos.
- Cadastro de veículo.
- Orçamentos.
- Nova OS.
- PDV.
- Estoque.
- Financeiro.
- Agenda.
- Relatórios.
- Tela em tema claro.
- Tela em tema escuro.

Critério de aceite:

- Screenshots gerados automaticamente.
- Relatório aponta se algum print falhou.
- As imagens servem para auditoria visual posterior.

## 1.8 Criar relatório automático de qualidade

Ao final dos testes, gerar:

```text
TestResults/FullValidation/<data-hora>/RELATORIO_QUALIDADE.md
```

O relatório deve conter:

- Data/hora.
- Branch/pasta analisada.
- Build aprovado/reprovado.
- Testes aprovados/reprovados.
- Telas testadas.
- Fluxos testados.
- Falhas encontradas.
- Arquivos prováveis relacionados às falhas.
- Severidade: crítica, alta, média, baixa.
- Próxima ação recomendada.

---

# FASE 2 — CORRIGIR ERROS ATUAIS DE TEMA, STYLE E EFFECT

Não avance para novas funcionalidades enquanto esta fase não estiver concluída.

## 2.1 Investigar erro `DependencyProperty.UnsetValue` em `Effect`

Pesquisar em todos os arquivos XAML:

```text
Effect=
DropShadowEffect
StaticResource
DynamicResource
BasedOn
CardShadow
Shadow
```

Verificar principalmente:

```text
Themes/Cards.xaml
Themes/Shadows.xaml
Themes/GlobalStyles.xaml
App.xaml
```

O que o Codex deve fazer:

- Encontrar onde `Effect` recebe resource inexistente.
- Encontrar onde um estilo tenta aplicar `Effect` com chave inexistente.
- Corrigir criando o recurso faltante ou removendo a aplicação inválida.
- Evitar usar `Effect` com binding que pode retornar nulo.
- Garantir fallback seguro.

Critério de aceite:

- Erro de `UnsetValue` eliminado.
- Tema carrega sem exceção.
- Cards métricos abrem em todas as telas.
- UI Smoke não falha por tema.

## 2.2 Padronizar sombras de cards

Criar ou corrigir recursos de sombra com nomes estáveis.

Exemplo de chaves que devem existir, se forem usadas:

```text
CardShadowEffect
SoftShadowEffect
MediumShadowEffect
StrongShadowEffect
PanelShadowEffect
```

O Codex deve verificar se todos os lugares que chamam essas chaves realmente possuem definição.

Critério de aceite:

- Nenhum card usa sombra inexistente.
- Visual fica consistente.
- Tema claro e escuro aceitam a mesma estrutura.

## 2.3 Corrigir cards métricos quebrados

Telas afetadas provavelmente incluem:

- Dashboard.
- Estoque.
- Veículos.
- Clientes.
- Financeiro.
- Agendamentos.
- Orçamentos.
- OS.

O Codex deve:

- Abrir cada tela.
- Verificar cards superiores.
- Corrigir estilos quebrados.
- Garantir alinhamento.
- Garantir texto visível.
- Garantir ícone visível.

Critério de aceite:

- Todas as telas com cards abrem sem exceção.
- UI Smoke volta para aprovado nas telas afetadas.

## 2.4 Rodar validação depois da correção

Executar:

```text
Scripts/Run-FullValidation.ps1
```

Critério de aceite:

```text
Build = aprovado
Theme Test = aprovado
UI Smoke = aprovado
Workflow = aprovado
```

Se ainda falhar, não avance. Corrija até passar.

---

# FASE 3 — CRIAR CHECKPOINT ESTÁVEL E PACOTE LIMPO

## 3.1 Criar checkpoint estável

Quando todos os testes passarem, criar pasta ou marcação:

```text
CHECKPOINT_ESTAVEL_UI_WORKFLOW_100_2026-06-07
```

Conteúdo do checkpoint:

- Código fonte atual.
- Relatório dos testes.
- Screenshots principais.
- Observações do que foi corrigido.

## 3.2 Criar script de pacote limpo

Criar:

```text
Scripts/Create-CleanPackage.ps1
```

Esse script deve gerar um ZIP limpo sem:

```text
bin/
obj/
.vs/
.git/
TestResults antigos desnecessários
logs temporários
cache
arquivos .user
arquivos .suo
```

E deve manter:

```text
Source code
Themes
Views
ViewModels
Models
Services
Data
Scripts
Docs
Testes importantes
README
```

Critério de aceite:

- ZIP fica menor.
- Projeto abre no Visual Studio.
- Projeto compila após extrair.

## 3.3 Melhorar `.gitignore`

Adicionar regras para impedir envio de lixo:

```text
bin/
obj/
.vs/
*.user
*.suo
TestResults/temp/
*.db-shm
*.db-wal
logs/temp/
```

Critério de aceite:

- Repositório/pacote não carrega arquivos desnecessários.

---

# FASE 4 — ORGANIZAR E REFATORAR ARQUITETURA

## 4.1 Mapear estrutura atual

Criar documento:

```text
Docs/ARQUITETURA_ATUAL.md
```

Deve conter:

- Pastas existentes.
- Principais telas.
- Principais serviços.
- Fluxo de navegação.
- Fluxo de banco.
- Fluxo de login/permissão.
- Fluxo de tema.
- Testes existentes.
- Arquivos grandes demais.
- Arquivos perigosos de alterar.

## 4.2 Refatorar `UiSmokeTestService`

Se o arquivo estiver grande, dividir em arquivos parciais ou classes menores:

```text
UiSmokeTestService.Core.cs
UiSmokeTestService.Login.cs
UiSmokeTestService.Navigation.cs
UiSmokeTestService.Theme.cs
UiSmokeTestService.Clientes.cs
UiSmokeTestService.Veiculos.cs
UiSmokeTestService.Orcamentos.cs
UiSmokeTestService.OrdensServico.cs
UiSmokeTestService.PDV.cs
UiSmokeTestService.Estoque.cs
UiSmokeTestService.Financeiro.cs
UiSmokeTestService.Relatorios.cs
UiSmokeTestService.Helpers.cs
```

Critério de aceite:

- Mesmo comportamento anterior preservado.
- Testes continuam passando.
- Arquivo fica mais fácil de manter.

## 4.3 Refatorar `DatabaseService`

Se o `DatabaseService` estiver grande demais, separar por domínio:

```text
ClienteDatabaseService
VeiculoDatabaseService
ProdutoDatabaseService
FornecedorDatabaseService
FuncionarioDatabaseService
OrcamentoDatabaseService
OrdemServicoDatabaseService
EstoqueDatabaseService
PdvDatabaseService
FinanceiroDatabaseService
AgendamentoDatabaseService
RelatorioDatabaseService
ConfiguracaoDatabaseService
```

Regra:

- Não quebrar a API usada pelas telas sem atualizar todos os pontos.
- Fazer refatoração em etapas.
- Testar após cada domínio separado.

## 4.4 Padronizar Models/ViewModels/Views

Verificar se existe mistura de lógica pesada em code-behind.

Onde possível:

- Manter View apenas para UI.
- ViewModel para estado e comandos.
- Service para regra de negócio.
- Repository/DatabaseService para dados.

Critério de aceite:

- Reduzir code-behind gigante.
- Preservar funcionamento.
- Não fazer refatoração radical sem testes.

---

# FASE 5 — BANCO DE DADOS, INTEGRIDADE E BACKUP

## 5.1 Criar documentação do banco

Criar:

```text
Docs/BANCO_DE_DADOS.md
```

Documentar:

- Nome de cada tabela.
- Campos.
- Tipo de cada campo.
- Chave primária.
- Chave estrangeira.
- Índices.
- Relacionamentos.
- Uso no sistema.

## 5.2 Criar controle de versão do banco

Implementar tabela:

```text
SchemaVersion
```

Campos sugeridos:

```text
Id
Version
AppliedAt
Description
```

Objetivo:

- Saber qual versão do banco o cliente usa.
- Permitir migrações futuras sem perder dados.

## 5.3 Criar migrações controladas

Criar estrutura para atualizar banco:

```text
Migrations/001_Initial.sql
Migrations/002_AddPermissions.sql
Migrations/003_AddWorkshopKanban.sql
...
```

Critério de aceite:

- Banco antigo consegue ser atualizado.
- Banco novo consegue ser criado do zero.
- Teste cobre migração.

## 5.4 Melhorar backup e restauração

Criar tela ou serviço para:

- Fazer backup manual.
- Fazer backup automático diário.
- Fazer backup antes de migração.
- Restaurar backup.
- Testar integridade do backup.

Critério de aceite:

- Backup gera arquivo válido.
- Restauração funciona em banco de teste.
- Usuário recebe mensagem clara.

## 5.5 Proteger dados reais

Implementar:

- Confirmação antes de excluir.
- Preferir desativar em vez de apagar cliente/produto/funcionário.
- Log de alterações críticas.
- Validação de campos obrigatórios.
- Tratamento de CPF/CNPJ duplicado.
- Tratamento de placa duplicada.

---

# FASE 6 — LOGIN, USUÁRIOS, PERFIS E SEGURANÇA

## 6.1 Melhorar login

O login deve ter:

- Campo email/usuário visível.
- Campo senha visível conforme tema.
- Botão entrar claro.
- Mensagem amigável de erro.
- Login centralizado.
- Janela abrindo corretamente.
- Senha armazenada com hash, não texto puro.

## 6.2 Criar usuário administrador padrão seguro

Regras:

- Criar admin inicial somente se não existir nenhum usuário.
- Solicitar troca de senha no primeiro acesso, se possível.
- Não deixar senha fraca fixa em produção.

## 6.3 Melhorar permissões

Criar matriz de permissões por módulo:

```text
Dashboard
Clientes
Veículos
Orçamentos
OS
PDV
Estoque
NF-e
Financeiro
Fornecedores
Funcionários
Agendamentos
Relatórios
Configurações
```

Para cada perfil, definir:

- Ver.
- Criar.
- Editar.
- Excluir/desativar.
- Exportar.
- Aprovar.
- Cancelar.

## 6.4 Criar log de auditoria

Registrar:

- Login.
- Logout.
- Tentativa de acesso negado.
- Exclusão/desativação.
- Cancelamento de venda.
- Alteração de preço.
- Alteração de estoque.
- Alteração financeira.
- Restauração de backup.

---

# FASE 7 — MELHORAR MÓDULOS EXISTENTES

## 7.1 Dashboard

Implementar ou revisar:

- Faturamento do dia.
- Faturamento do mês.
- OS abertas.
- OS atrasadas.
- Orçamentos pendentes.
- Veículos na oficina.
- Contas a receber.
- Contas a pagar.
- Estoque crítico.
- Agenda do dia.
- Serviços em andamento.
- Cards com ícones consistentes.
- Gráficos simples.
- Atalhos rápidos.

Critério de aceite:

- Dashboard mostra dados reais do banco.
- Não mostra números fixos falsos sem aviso.

## 7.2 Clientes

Melhorias obrigatórias:

- Cadastro completo.
- Nome/Razão social.
- CPF/CNPJ.
- RG/IE, se necessário.
- Telefone.
- WhatsApp.
- Email.
- Endereço completo.
- Observações.
- Tipo: pessoa física/jurídica.
- Status ativo/inativo.
- Botão visualizar.
- Botão editar.
- Botão histórico.
- Botão excluir/desativar.
- Botão WhatsApp.
- Botão novo orçamento.
- Botão nova OS.

Histórico do cliente deve mostrar:

- Veículos.
- Orçamentos.
- OS.
- Vendas.
- Pagamentos.
- Débitos.
- Fotos/documentos.
- Observações.

## 7.3 Veículos

Campos importantes:

- Placa.
- Marca.
- Modelo.
- Ano.
- Cor.
- Chassi.
- Renavam, se desejado.
- Cliente vinculado.
- Tipo: carro, moto, caminhão, ônibus, van, máquina.
- Sistema elétrico: 12V, 24V ou híbrido.
- Combustível.
- Quilometragem.
- Observações.

Recursos:

- Histórico de OS.
- Histórico de defeitos.
- Histórico de peças aplicadas.
- Fotos do veículo.
- Prontuário elétrico.

## 7.4 Orçamentos

Deve permitir:

- Selecionar cliente.
- Selecionar veículo.
- Adicionar peças.
- Adicionar serviços/mão de obra.
- Adicionar diagnóstico.
- Adicionar observações.
- Calcular subtotal.
- Aplicar desconto em R$ ou %.
- Calcular total.
- Definir validade.
- Definir status: rascunho, enviado, aprovado, recusado, vencido.
- Enviar por WhatsApp.
- Gerar PDF.
- Converter em OS.

Critério de aceite:

- Ao converter em OS, não perder peças, serviços, cliente, veículo e valores.

## 7.5 Ordens de Serviço

A OS deve conter:

- Número sequencial.
- Cliente.
- Veículo.
- Técnico responsável.
- Data de entrada.
- Previsão de entrega.
- Data de conclusão.
- Status.
- Defeito reclamado.
- Diagnóstico.
- Serviços executados.
- Peças utilizadas.
- Fotos antes/depois.
- Checklist inicial.
- Checklist final.
- Garantia.
- Termo de autorização.
- Assinatura do cliente.

Status recomendados:

```text
Aberta
Em diagnóstico
Aguardando aprovação
Aguardando peça
Em execução
Finalizada
Aguardando pagamento
Entregue
Cancelada
```

## 7.6 PDV e Caixa

Recursos obrigatórios:

- Abrir caixa.
- Fechar caixa.
- Sangria.
- Suprimento.
- Venda rápida.
- Venda vinculada a cliente.
- Venda vinculada a OS.
- Produto por código.
- Produto por busca.
- Quantidade.
- Desconto.
- Acréscimo.
- Dinheiro.
- PIX.
- Cartão débito.
- Cartão crédito.
- Pagamento misto.
- Cancelamento.
- Estorno.
- Comprovante.
- Lançamento automático no financeiro.
- Baixa automática de estoque.

Critério de aceite:

- Venda não pode baixar estoque sem lançar financeiro.
- Cancelamento deve estornar estoque e financeiro.

## 7.7 Estoque

Campos:

- Código interno.
- Código de barras.
- Nome.
- Descrição.
- Categoria.
- Marca.
- Aplicação.
- Unidade.
- Localização física.
- Fornecedor principal.
- Custo.
- Preço de venda.
- Margem.
- Estoque atual.
- Estoque mínimo.
- Estoque máximo.
- Status.

Movimentações:

- Entrada manual.
- Entrada via NF-e.
- Saída por venda.
- Saída por OS.
- Ajuste de estoque.
- Perda/avaria.
- Devolução.

Alertas:

- Estoque crítico.
- Produto sem preço.
- Produto sem fornecedor.
- Margem baixa.

## 7.8 Importação NF-e

O sistema deve:

- Importar XML.
- Ler fornecedor.
- Conferir CNPJ.
- Criar fornecedor se não existir.
- Ler produtos.
- Comparar com produtos existentes.
- Sugerir vínculo.
- Permitir criar produto novo.
- Atualizar custo.
- Atualizar estoque.
- Gerar contas a pagar.
- Mostrar prévia antes de confirmar.
- Registrar histórico da importação.

## 7.9 Financeiro

Implementar:

- Contas a pagar.
- Contas a receber.
- Fluxo de caixa.
- DRE simples.
- Faturamento por período.
- Lucro por OS.
- Lucro por produto.
- Lucro por serviço.
- Despesas fixas.
- Despesas variáveis.
- Recebimentos por forma de pagamento.
- Caixa por operador.
- Inadimplência.

## 7.10 Fornecedores

Campos:

- Nome/Razão social.
- CNPJ/CPF.
- IE.
- Telefone.
- WhatsApp.
- Email.
- Endereço.
- Contato responsável.
- Prazo médio de pagamento.
- Observações.

Recursos:

- Produtos fornecidos.
- Histórico de compras.
- NF-e importadas.
- Contas a pagar vinculadas.

## 7.11 Funcionários

Campos:

- Nome.
- CPF.
- Telefone.
- Email.
- Cargo.
- Perfil de acesso.
- Status ativo/inativo.
- Data de admissão.
- Observações.

Recursos:

- Histórico de login.
- OS executadas.
- Vendas realizadas.
- Caixa operado.
- Permissões.

## 7.12 Agendamentos

Recursos:

- Agenda diária.
- Agenda semanal.
- Agenda mensal.
- Cliente.
- Veículo.
- Serviço previsto.
- Técnico responsável.
- Horário.
- Status.
- Confirmação.
- Reagendamento.
- Cancelamento.
- Conversão para OS.
- Envio de lembrete por WhatsApp.

## 7.13 Relatórios

Relatórios mínimos:

- Faturamento diário.
- Faturamento mensal.
- OS abertas.
- OS finalizadas.
- OS por técnico.
- Orçamentos pendentes.
- Orçamentos aprovados.
- Produtos mais vendidos.
- Estoque crítico.
- Lucro por produto.
- Lucro por serviço.
- Clientes mais ativos.
- Caixa por operador.
- Contas vencidas.
- Serviços mais realizados.

Exportações:

- PDF.
- Excel/CSV, se possível.
- Impressão.

---

# FASE 8 — MELHORAR VISUAL, DESIGN SYSTEM E EXPERIÊNCIA DE USO

## 8.1 Criar design system definitivo

Documentar em:

```text
Docs/DESIGN_SYSTEM.md
```

Padronizar:

- Paleta de cores.
- Tema claro.
- Tema escuro.
- Tipografia.
- Tamanho de fonte.
- Espaçamento.
- Bordas.
- Radius.
- Sombras.
- Ícones.
- Botões.
- Inputs.
- Cards.
- Tabelas.
- Modais.
- Alertas.
- Badges de status.

## 8.2 Corrigir contraste

Verificar:

- Campo de email/senha no login.
- Placeholder.
- TextBox.
- ComboBox.
- DataGrid.
- Botões secundários.
- Status badges.
- Tema escuro.
- Tema claro.

Critério:

- Nenhum texto pode ficar invisível.
- Nenhum campo pode ter texto da mesma cor do fundo.

## 8.3 Padronizar cabeçalhos das telas

Todas as telas devem ter:

- Título claro.
- Subtítulo, se útil.
- Botão principal à direita.
- Botões secundários padronizados.
- Espaçamento igual.

## 8.4 Melhorar Dashboard visualmente

Criar visual profissional:

- Cards modernos.
- Ícones coerentes.
- Gráficos limpos.
- Alertas importantes.
- Atalhos rápidos.
- Layout responsivo.

## 8.5 Melhorar PDV visualmente

O PDV deve ser extremamente prático:

- Campo de busca grande.
- Lista de produtos fácil.
- Carrinho grande.
- Total em destaque.
- Botões grandes para pagamento.
- Atalhos de teclado.
- Leitura por código de barras.
- Layout pensado para uso rápido.

## 8.6 Melhorar OS e Orçamentos visualmente

Criar telas com:

- Resumo do cliente.
- Resumo do veículo.
- Itens/peças.
- Serviços.
- Totais.
- Status destacado.
- Ações rápidas.
- Timeline.

## 8.7 Criar modo compacto e modo confortável

Modo compacto:

- Mais dados na tela.
- Útil para computador pequeno.

Modo confortável:

- Mais espaçamento.
- Visual premium.

---

# FASE 9 — RECURSOS PROFISSIONAIS DE SISTEMAS SEMELHANTES

## 9.1 Checklist visual com fotos

Criar módulo de checklist para entrada e saída do veículo.

Itens sugeridos:

- Estado geral do veículo.
- Painel.
- Luzes.
- Faróis.
- Lanternas.
- Setas.
- Luz de freio.
- Luz de ré.
- Limpador.
- Buzina.
- Bateria.
- Alternador.
- Motor de partida.
- Fusíveis.
- Relés.
- Chicotes aparentes.
- Aterramentos visíveis.
- Acessórios instalados.
- Avarias.
- Objetos deixados no veículo.

Recursos:

- Fotos antes.
- Fotos depois.
- Observações.
- Assinatura do cliente.
- PDF do checklist.
- Envio por WhatsApp.

## 9.2 Aprovação de orçamento por WhatsApp

Criar fluxo:

1. Gerar orçamento.
2. Criar mensagem pronta.
3. Abrir WhatsApp Web ou link `wa.me`.
4. Registrar que foi enviado.
5. Permitir marcar como aprovado.
6. Permitir marcar como recusado.
7. Registrar data/hora da aprovação.

Mensagem deve incluir:

- Nome da oficina.
- Cliente.
- Veículo.
- Resumo dos serviços.
- Valor total.
- Validade.
- Observações.

## 9.3 Kanban da oficina

Criar tela Kanban com colunas:

```text
Agendado
Recebido
Em diagnóstico
Aguardando aprovação
Aguardando peça
Em execução
Finalizado
Aguardando pagamento
Entregue
Cancelado
```

Cada card deve mostrar:

- Número da OS.
- Cliente.
- Veículo.
- Placa.
- Serviço principal.
- Técnico.
- Prazo.
- Status.
- Valor estimado.

Recursos:

- Arrastar entre colunas, se possível.
- Alterar status.
- Abrir OS.
- Enviar WhatsApp.

## 9.4 Timeline do cliente e do veículo

Criar linha do tempo com:

- Cadastro.
- Agendamentos.
- Orçamentos.
- OS.
- Vendas.
- Pagamentos.
- Fotos.
- Observações.
- Garantias.

## 9.5 Assinatura digital simples

Criar recurso para cliente assinar:

- Autorização de serviço.
- Retirada do veículo.
- Aprovação de orçamento.
- Checklist.

Pode ser assinatura com mouse/touch em canvas simples.

## 9.6 Garantia de serviço

Registrar:

- Serviço executado.
- Peça aplicada.
- Prazo de garantia.
- Condições.
- Data final da garantia.
- PDF/termo.

## 9.7 Comunicação com cliente

Criar modelos de mensagens:

- Orçamento enviado.
- Orçamento aprovado.
- Veículo em diagnóstico.
- Aguardando peça.
- Serviço finalizado.
- Veículo pronto para retirada.
- Cobrança amigável.
- Lembrete de retorno.

---

# FASE 10 — DIFERENCIAIS ESPECÍFICOS DE AUTO ELÉTRICA AUTOMOTIVA

Esta fase é essencial para o sistema não ser apenas mais um sistema de oficina genérico.

## 10.1 Prontuário elétrico do veículo

Criar aba no veículo chamada:

```text
Prontuário Elétrico
```

Registrar:

- Sistema 12V/24V.
- Bateria instalada.
- Marca da bateria.
- Amperagem.
- Data de instalação.
- Teste de tensão em repouso.
- Teste de tensão na partida.
- Teste de carga do alternador.
- Corrente de fuga.
- Estado dos aterramentos.
- Chicotes reparados.
- Fusíveis substituídos.
- Relés substituídos.
- Lâmpadas substituídas.
- Acessórios instalados.
- Observações técnicas.
- Fotos técnicas.

## 10.2 Diagnóstico guiado de defeitos elétricos

Criar módulo de diagnóstico com roteiros.

Roteiros mínimos:

- Veículo não dá partida.
- Bateria descarregando.
- Alternador não carrega.
- Motor de partida pesado.
- Fusível queimando.
- Farol fraco.
- Luz de ré não acende.
- Lanterna não acende.
- Limpador não funciona.
- Limpador só funciona uma velocidade.
- Vidro elétrico não funciona.
- Trava elétrica não funciona.
- Seta não funciona.
- Painel marcando errado.
- Curto intermitente.
- Relé não aciona.
- Mau aterramento.

Cada roteiro deve ter:

- Sintoma.
- Possíveis causas.
- Ferramentas necessárias.
- Sequência de testes.
- Valores esperados.
- Campo para resultado.
- Campo para conclusão.
- Possibilidade de anexar foto.
- Possibilidade de gerar orçamento a partir do diagnóstico.

## 10.3 Biblioteca técnica de auto elétrica

Criar módulo de consulta com:

- Como testar relé 4 pinos.
- Como testar relé 5 pinos.
- Como testar fusível.
- Como testar aterramento.
- Como testar queda de tensão.
- Como testar bateria.
- Como testar alternador.
- Como testar motor de partida.
- Tabela básica de bitola de fios.
- Tabela básica 12V/24V.
- Tipos de terminais.
- Tipos de conectores.
- Cores comuns de fios.
- Cuidados com rede CAN.
- Cuidados com módulos eletrônicos.

## 10.4 Histórico de defeitos recorrentes

O sistema deve permitir consultar:

- Veículos que voltaram pelo mesmo defeito.
- Peças que falham mais.
- Serviços que deram garantia.
- Defeitos comuns por modelo.
- Tempo médio para resolver defeitos.

## 10.5 Sugestão de peças por tipo de serviço

Ao criar OS/orçamento, o sistema pode sugerir peças comuns.

Exemplos:

- Instalação de lanterna: terminais, fio, fita isolante, soquete, conector.
- Revisão de alternador: rolamento, regulador, escova, ponte retificadora.
- Motor de partida: escova, automático, bucha, induzido, porta-escovas.
- Chicote de farol: terminal, relé, fusível, porta-fusível, fio.

## 10.6 Cadastro de serviços técnicos de auto elétrica

Criar tabela de serviços:

- Diagnóstico elétrico.
- Revisão de alternador.
- Revisão de motor de partida.
- Instalação de farol.
- Instalação de lanterna.
- Instalação de LED.
- Reparo de chicote.
- Instalação de relé auxiliar.
- Instalação de tomada carreta.
- Instalação de alarme.
- Instalação de trava elétrica.
- Instalação de som/acessório.
- Teste de fuga de corrente.
- Revisão de aterramento.

Cada serviço deve ter:

- Nome.
- Descrição.
- Valor padrão.
- Tempo médio.
- Garantia padrão.
- Peças sugeridas.

---

# FASE 11 — DOCUMENTOS, PDF, IMPRESSÃO E EXPORTAÇÃO

## 11.1 Padronizar PDFs

Criar modelos para:

- Orçamento.
- OS.
- Checklist.
- Recibo.
- Comprovante de venda.
- Termo de garantia.
- Termo de autorização.
- Relatório financeiro.

Cada PDF deve conter:

- Logo/nome da oficina.
- Dados da oficina.
- Dados do cliente.
- Dados do veículo.
- Itens.
- Valores.
- Observações.
- Assinatura, quando aplicável.

## 11.2 Testar impressão real

Criar teste/manual para:

- Impressora A4.
- Impressora térmica, se futuramente usar.
- Margens.
- Quebra de página.
- Fonte legível.

## 11.3 Exportar relatórios

Permitir exportar:

- PDF.
- CSV.
- Excel, se viável.

---

# FASE 12 — CONFIGURAÇÕES DO SISTEMA

Criar tela de configurações para:

- Dados da oficina.
- Logo.
- CNPJ/CPF.
- Telefone.
- WhatsApp.
- Endereço.
- Tema claro/escuro.
- Backup automático.
- Impressora padrão.
- Numeração de OS.
- Numeração de orçamento.
- Permissões.
- Modelos de mensagens.
- Garantia padrão.
- Margem padrão de produtos.

Critério de aceite:

- Configurações persistem no banco.
- Alterações importantes exigem perfil autorizado.

---

# FASE 13 — ROBUSTEZ, TRATAMENTO DE ERROS E LOGS

## 13.1 Tratamento de exceções

Nenhuma tela deve fechar o sistema sem mensagem amigável.

Implementar:

- Try/catch em operações críticas.
- Log técnico detalhado.
- Mensagem simples para usuário.
- Botão copiar erro, se útil.

## 13.2 Logs estruturados

Registrar logs em:

```text
Logs/app-yyyy-MM-dd.log
```

Logs devem conter:

- Data/hora.
- Usuário.
- Tela.
- Ação.
- Erro técnico.
- Stack trace quando necessário.

## 13.3 Validações de formulário

Todo formulário deve validar:

- Campos obrigatórios.
- Formato de email.
- Telefone.
- CPF/CNPJ, se implementado.
- Valores monetários negativos.
- Quantidade negativa.
- Data inválida.
- Duplicidade.

---

# FASE 14 — INSTALAÇÃO, ATUALIZAÇÃO E USO REAL

## 14.1 Criar instalador

Preparar instalador para Windows:

- Instalar programa.
- Criar atalho na área de trabalho.
- Criar pasta de dados.
- Verificar .NET necessário.
- Permitir atualização futura.

## 14.2 Testar em Windows limpo

Executar teste em máquina/pasta limpa:

- Instala.
- Abre.
- Cria banco.
- Login funciona.
- Cadastro funciona.
- Backup funciona.
- Impressão funciona.

## 14.3 Criar rotina de atualização

Antes de atualizar versão:

- Fazer backup.
- Rodar migração.
- Validar banco.
- Abrir sistema.
- Gerar log da atualização.

---

# FASE 15 — DOCUMENTAÇÃO PARA O DONO E FUTURO USUÁRIO

Criar pasta:

```text
Docs/ManualUsuario/
```

Documentos necessários:

- Como instalar.
- Primeiro acesso.
- Como cadastrar cliente.
- Como cadastrar veículo.
- Como criar orçamento.
- Como converter orçamento em OS.
- Como vender no PDV.
- Como fechar caixa.
- Como importar NF-e.
- Como fazer backup.
- Como restaurar backup.
- Como cadastrar funcionário.
- Como configurar permissões.

Criar também:

```text
Docs/ManualTecnico/
```

Com:

- Estrutura do projeto.
- Banco de dados.
- Serviços principais.
- Como rodar testes.
- Como gerar pacote limpo.
- Como criar release.

---

# FASE 16 — PREPARAÇÃO PARA PRODUTO VENDÁVEL NO FUTURO

Não implementar antes de estabilizar o sistema, mas planejar:

## 16.1 Multiempresa

Permitir no futuro:

- Uma instalação para uma oficina.
- Dados isolados por empresa.
- Configuração de logo e dados por empresa.

## 16.2 Multiestação em rede local

Avaliar futuramente:

- SQLite em rede pode não ser ideal para muitos usuários simultâneos.
- Considerar PostgreSQL/SQL Server no futuro.
- Criar camada de dados preparada para migração.

## 16.3 Licenciamento

Planejar:

- Chave de licença.
- Ativação offline/online.
- Controle de versão.
- Backup do cliente.

## 16.4 Planos comerciais futuros

Possíveis planos:

- Básico: clientes, veículos, OS, orçamento.
- Profissional: estoque, PDV, financeiro, relatórios.
- Premium: WhatsApp, checklist com fotos, assinatura, diagnóstico guiado.

---

# FASE 17 — CHECKLIST FINAL DE QUALIDADE ANTES DE USAR NA OFICINA

Antes de usar o sistema em oficina real, confirmar:

- Build sem erro.
- UI Smoke 100%.
- Workflow 100%.
- Teste de tema 100%.
- Teste de banco 100%.
- Teste de permissões 100%.
- Backup funcionando.
- Restauração funcionando.
- Login funcionando.
- Cadastro de cliente funcionando.
- Cadastro de veículo funcionando.
- Orçamento funcionando.
- OS funcionando.
- PDV funcionando.
- Estoque baixando corretamente.
- Financeiro lançando corretamente.
- Caixa fechando corretamente.
- Relatórios abrindo.
- PDF gerando.
- Impressão testada.
- Dados persistem depois de fechar e abrir.
- Erros são registrados em log.
- Nenhum botão principal está sem ação.
- Nenhum texto invisível.
- Nenhum botão cortado.
- Nenhuma tela fora do padrão visual.

---

# ORDEM RESUMIDA DE EXECUÇÃO, UMA POR VEZ

Use esta lista como sequência de acompanhamento:

1. Criar `Scripts/Run-FullValidation.ps1`.
2. Melhorar UI Smoke Test.
3. Criar teste de tema/ResourceDictionary.
4. Melhorar Workflow Test com fluxo real de oficina.
5. Criar teste de permissões por perfil.
6. Criar teste de banco/persistência.
7. Criar captura automática de telas.
8. Criar relatório automático de qualidade.
9. Corrigir erro `Effect/Style/DependencyProperty.UnsetValue`.
10. Corrigir sombras/cards quebrados.
11. Fazer UI Smoke voltar para 100%.
12. Fazer Workflow continuar 100%.
13. Criar checkpoint estável.
14. Criar script de pacote limpo.
15. Corrigir `.gitignore`.
16. Documentar arquitetura atual.
17. Refatorar UiSmokeTestService.
18. Refatorar DatabaseService.
19. Documentar banco de dados.
20. Criar versionamento de schema.
21. Criar migrações.
22. Melhorar backup/restauração.
23. Melhorar login.
24. Melhorar senha/hash.
25. Melhorar permissões.
26. Criar log de auditoria.
27. Melhorar Dashboard.
28. Melhorar Clientes.
29. Melhorar Veículos.
30. Melhorar Orçamentos.
31. Melhorar OS.
32. Melhorar PDV/Caixa.
33. Melhorar Estoque.
34. Melhorar NF-e.
35. Melhorar Financeiro.
36. Melhorar Fornecedores.
37. Melhorar Funcionários.
38. Melhorar Agendamentos.
39. Melhorar Relatórios.
40. Criar Design System.
41. Corrigir contraste geral.
42. Padronizar cabeçalhos.
43. Melhorar Dashboard visualmente.
44. Melhorar PDV visualmente.
45. Melhorar OS/Orçamentos visualmente.
46. Criar modo compacto/confortável.
47. Criar checklist visual com fotos.
48. Criar aprovação por WhatsApp.
49. Criar Kanban da oficina.
50. Criar timeline cliente/veículo.
51. Criar assinatura digital simples.
52. Criar garantia de serviço.
53. Criar modelos de mensagens ao cliente.
54. Criar prontuário elétrico do veículo.
55. Criar diagnóstico guiado.
56. Criar biblioteca técnica de auto elétrica.
57. Criar histórico de defeitos recorrentes.
58. Criar sugestão de peças por serviço.
59. Criar cadastro de serviços técnicos.
60. Padronizar PDFs.
61. Testar impressão real.
62. Criar exportação PDF/CSV/Excel.
63. Criar tela de configurações.
64. Melhorar tratamento de exceções.
65. Melhorar logs estruturados.
66. Melhorar validações de formulário.
67. Criar instalador.
68. Testar em Windows limpo.
69. Criar rotina de atualização.
70. Criar manual do usuário.
71. Criar manual técnico.
72. Planejar multiempresa.
73. Planejar multiestação.
74. Planejar licenciamento.
75. Planejar planos comerciais.
76. Executar checklist final de qualidade.

---

# MODELO DE RELATÓRIO QUE O CODEX DEVE ENTREGAR APÓS CADA FASE

Ao concluir cada fase, entregar relatório com este formato:

```text
FASE EXECUTADA:

OBJETIVO:

ARQUIVOS ALTERADOS:
- arquivo 1: motivo
- arquivo 2: motivo

O QUE FOI IMPLEMENTADO:

O QUE FOI CORRIGIDO:

TESTES EXECUTADOS:
- dotnet restore: aprovado/reprovado
- dotnet build: aprovado/reprovado
- UI Smoke: aprovado/reprovado
- Workflow: aprovado/reprovado
- Theme Test: aprovado/reprovado
- Database Test: aprovado/reprovado

RESULTADO FINAL:

FALHAS RESTANTES:

PRÓXIMO PASSO RECOMENDADO:

OBSERVAÇÕES IMPORTANTES:
```

---

# COMANDO INICIAL RECOMENDADO PARA PASSAR AO CODEX

Copie e cole este comando para iniciar a execução:

```text
Codex, leia o arquivo PLANO_MESTRE_ORDEM_EXECUCAO_CODEX_PRIMO_AUTOELETRICA_2026-06-07.md inteiro antes de alterar qualquer coisa.

Execute somente a FASE 1 neste momento.

Não crie funcionalidades novas.
Não mexa no visual ainda.
Não refatore telas ainda.
Primeiro melhore o processo de testes, validação e simulações.

Crie o script Run-FullValidation.ps1, melhore o UI Smoke Test, crie o teste de tema/ResourceDictionary, melhore o Workflow Test com fluxo real de oficina, crie teste de permissões, teste de banco, captura automática de telas e relatório automático de qualidade.

Depois execute build e todos os testes disponíveis.

Ao final, entregue relatório dizendo exatamente quais arquivos foram criados/alterados, quais testes foram executados, quais passaram, quais falharam e qual é o próximo passo.

Não avance para a FASE 2 sem autorização.
```

---

# OBSERVAÇÃO FINAL AO CODEX

Este projeto é importante porque será usado futuramente em uma oficina real de auto elétrica automotiva. Portanto, priorize estabilidade, clareza, segurança dos dados, visual profissional e testes confiáveis.

Não trabalhe como se fosse apenas um protótipo. Trabalhe como se o sistema fosse usado todos os dias para cadastrar clientes, controlar veículos, vender peças, abrir ordens de serviço, fechar caixa e guardar o histórico técnico dos veículos.
