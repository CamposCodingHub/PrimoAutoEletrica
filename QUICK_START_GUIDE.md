# 🚀 GUIA RÁPIDO - Novos Serviços PrimoAutoEletrica

## 📖 Índice

1. [Gerenciamento de Idiomas](#gerenciamento-de-idiomas)
2. [Relatórios PDF/Excel](#relatórios-pdfexcel)
3. [Gerenciamento de Impressoras](#gerenciamento-de-impressoras)
4. [Audit Trail (Histórico)](#audit-trail)
5. [Acessibilidade](#acessibilidade)
6. [Auditoria de Código](#auditoria-de-código)

---

## Gerenciamento de Idiomas

### Como Usar

```csharp
// Injetar LanguageManager
private readonly LanguageManager _languageManager;

public MeuViewModel()
{
    _languageManager = App.Services.GetRequiredService<LanguageManager>();
}

// Trocar idioma em tempo de execução
_languageManager.ChangeLanguage("pt-BR"); // Português
_languageManager.ChangeLanguage("en-US"); // English
_languageManager.ChangeLanguage("es-ES"); // Español

// Ouvir mudanças de idioma
_languageManager.LanguageChanged += (sender, newLanguage) =>
{
    Console.WriteLine($"Idioma alterado para: {newLanguage}");
};

// Obter idiomas suportados
var langs = _languageManager.SupportedLanguages;
foreach (var lang in langs)
{
    Console.WriteLine($"{lang.Code}: {lang.Name}");
}
```

### Em XAML

**Usando Binding:**
```xaml
<Button Content="{Binding Key, Converter={StaticResource LocalizationConverter}}" />
```

**Usando Markup Extension:**
```xaml
<Button Content="{local:Localize Save}" />
<TextBlock Text="{local:Localize NoDataFound}" />
```

### Adicionar Nova Tradução

1. Editar `LocalizationService.cs`
2. Adicionar chave no dicionário em `GetTranslations()`
3. Adicionar valor para cada idioma (pt-BR, en-US, es-ES)

```csharp
// Exemplo
translations["MeuNovoTexto"] = new Dictionary<string, string>
{
    ["pt-BR"] = "Meu Novo Texto",
    ["en-US"] = "My New Text",
    ["es-ES"] = "Mi Nuevo Texto"
};
```

---

## Relatórios PDF/Excel

### Como Usar

```csharp
// Injetar ViewModel
private readonly RelatoriosModernoViewModel _relatoriosVM;

public MinhaView()
{
    _relatoriosVM = App.Services.GetRequiredService<RelatoriosModernoViewModel>();
}

// Carregar dados para período específico
_relatoriosVM.DataInicio = new DateTime(2026, 1, 1);
_relatoriosVM.DataFim = DateTime.Now;
await _relatoriosVM.CarregarDadosCommand.ExecuteAsync(null);

// Exportar em PDF
await _relatoriosVM.ExportarPdfCommand.ExecuteAsync(null);
// Arquivo salvo em Desktop/Relatorio_yyyyMMdd_HHmmss.pdf

// Exportar em Excel
await _relatoriosVM.ExportarExcelCommand.ExecuteAsync(null);
// Arquivo salvo em Desktop/Relatorio_yyyyMMdd_HHmmss.xlsx

// Alterar período pré-definido
_relatoriosVM.AlterarPeriodoCommand.Execute("Mês");
// Opções: "Hoje", "Semana", "Mês", "Trimestre", "Ano"
```

### Properties para Observação

```csharp
// Resumos financeiros
decimal FaturamentoTotal    // Total de receitas
decimal DespesasTotal       // Total de despesas
decimal LucroLiquido        // Receita - Despesa
decimal TicketMedio         // Valor médio por venda

// Contadores
int TotalVendas             // Número de vendas
int TotalOrdenAbertas       // OS abertas
int TotalOrdenFinalizadas   // OS finalizadas

// Status
bool IsCarregando           // Carregamento em progresso
string MensagemStatus       // Mensagem de status
string MensagemErro         // Mensagem de erro
bool PodeFazerExportacao    // Habilita botões de export
```

---

## Gerenciamento de Impressoras

### Como Usar

```csharp
// Injetar ViewModel
private readonly PrinterManagementViewModel _printerVM;

public MinhaView()
{
    _printerVM = App.Services.GetRequiredService<PrinterManagementViewModel>();
}

// Atualizar lista de impressoras
_printerVM.AtualizarListaImpressorasCommand.Execute(null);

// Selecionar impressora
var printerSelecionada = _printerVM.ImpressorasDisponiveis.FirstOrDefault();
_printerVM.SelecionarImpressoraCommand.Execute(printerSelecionada);

// Testar conectividade
_printerVM.TestarImpressoraCommand.Execute(null);

// Definir como padrão
_printerVM.DefinirComoImpressoraParpadrao Command.Execute(null);

// Exportar relatório
_printerVM.ExportarRelatorioImpressorasCommand.Execute(null);
// Arquivo salvo em Desktop/Diagnostico_Impressoras_yyyyMMdd_HHmmss.txt
```

### Properties para Observação

```csharp
ObservableCollection<PrinterDiagnosticInfo> ImpressorasDisponiveis
PrinterDiagnosticInfo ImpressoraSelecionada
string StatusImpressora         // Detalhes da impressora selecionada
bool ImpressoraDisponivelParaPrinting  // True se online
bool IsCarregando
string MensagemStatus
string MensagemErro
```

---

## Audit Trail

### Como Usar

```csharp
// Injetar service
private readonly AuditTrailService _auditTrail;

public MeuService()
{
    _auditTrail = App.Services.GetRequiredService<AuditTrailService>();
}

// Registrar uma mudança
await _auditTrail.RegistrarAlteracaoAsync(
    entidadeTipo: "Cliente",
    entidadeId: cliente.Id,
    acao: "CRIAR",
    valoresAntigos: null,
    valoresNovos: JsonConvert.SerializeObject(cliente),
    usuarioId: App.Session?.CurrentUser?.Id.ToString()
);

// Obter histórico de uma entidade
var historico = await _auditTrail.ObterHistoricoAsync(
    entidadeTipo: "Cliente",
    entidadeId: 123,
    maxResultados: 100
);

foreach (var entrada in historico)
{
    Console.WriteLine(entrada.ToString());
    // [01/09/2026 14:30] usuario123 - CRIAR em Cliente #123
}

// Obter alterações de um período
var alteracoes = await _auditTrail.ObterAlteracoesPorPeriodoAsync(
    dataInicio: DateTime.Now.AddDays(-30),
    dataFim: DateTime.Now,
    entidadeTipo: "Cliente",    // Opcional
    acao: "CRIAR",               // Opcional
    usuarioId: "user123"         // Opcional
);

// Obter estatísticas
var stats = await _auditTrail.ObterEstatisticasAsync(
    dataInicio: DateTime.Now.AddMonths(-1),
    dataFim: DateTime.Now
);

Console.WriteLine($"Total de alterações: {stats.TotalAlteracoes}");

// Limpeza de registros antigos (> 90 dias)
int deletados = await _auditTrail.LimparAuditoriasAntigasAsync(diasRetencao: 90);
Console.WriteLine($"Registros antigos removidos: {deletados}");
```

---

## Acessibilidade

### Como Usar

```csharp
// Aplicar propriedades acessíveis a um controle
AccessibilityService.ApplyAccessibilityProperties(
    meuBotao,
    name: "Botão Salvar",
    helpText: "Clique para salvar as alterações"
);

// Registrar atalhos de teclado
AccessibilityService.RegisterKeyboardShortcuts(MinhaJanela);

// Aplicar tema de contraste alto
AccessibilityService.ApplyHighContrastTheme(Application.Current);

// Aumentar tamanho de fonte
AccessibilityService.ApplyLargeFontSize(Application.Current, multiplier: 1.5);

// Habilitar suporte a leitura de tela
AccessibilityService.EnableScreenReaderSupport(MinhaJanela);

// Anunciar mensagem para leitor de tela
AccessibilityService.AnnounceToScreenReader("Dados carregados com sucesso!");

// Validar acessibilidade de um controle
var resultado = AccessibilityService.ValidateControlAccessibility(meuBotao);
if (!resultado.IsValid)
{
    foreach (var issue in resultado.Issues)
    {
        Console.WriteLine($"⚠️ {issue}");
    }
}
```

### Atalhos de Teclado Inclusos

| Atalho | Função |
|--------|--------|
| **Alt+C** | Ir para Clientes |
| **Alt+F** | Ir para Fornecedores |
| **Alt+E** | Ir para Estoque |
| **Alt+V** | Ir para Veículos |
| **Alt+O** | Ir para Ordens de Serviço |
| **Alt+R** | Ir para Relatórios |
| **Ctrl+N** | Novo |
| **Ctrl+S** | Salvar |
| **Ctrl+D** | Deletar |
| **Ctrl+E** | Editar |
| **Ctrl+P** | Imprimir |
| **Ctrl+X** | Exportar |
| **F1** | Ajuda |
| **F12** | Modo desenvolvedor |
| **Esc** | Cancelar/Fechar |

---

## Auditoria de Código

### Como Usar

```csharp
// Criar serviço de auditoria
var auditService = new CodeAuditService(@"C:\Projetos\PrimoAutoEletrica");

// Executar auditoria completa
var report = await auditService.RunAuditAsync();

// Exibir resultado
Console.WriteLine(report.ToString());

// Acessar detalhes
Console.WriteLine($"Total de arquivos: {report.TotalFiles}");
Console.WriteLine($"Arquivos vazios: {report.EmptyFiles.Count}");
Console.WriteLine($"Classes não utilizadas: {report.UnusedClasses.Count}");
Console.WriteLine($"Métodos private não chamados: {report.UnusedMethods.Count}");
Console.WriteLine($"Classes duplicadas: {report.DuplicateClasses.Count}");

// Salvar relatório em arquivo
var relatorio = string.Join("\n", report.AuditLog);
File.WriteAllText("auditoria_codigo.txt", relatorio);
```

### Interpretação dos Resultados

- **Arquivos vazios:** Revisar se ainda são necessários
- **Classes não utilizadas:** Candidatas para remoção
- **Métodos private não chamados:** Dead code, remover ou refatorar
- **Classes duplicadas:** Considerar consolidação
- **Using não utilizados:** Limpeza de imports

---

## 📝 Boas Práticas

### Para Novos ViewModels
1. Herdar de `BaseViewModel`
2. Usar `[ObservableProperty]` para propriedades
3. Usar `[RelayCommand]` para ações
4. Implementar logging com `App.Logger`
5. Usar `try-catch` com tratamento de erros

### Para Novos Services
1. Registrar no `ServiceExtensions.cs`
2. Usar `Singleton` para serviços globais
3. Usar `Transient` para instâncias por requisição
4. Implementar logging
5. Documentar com XML comments

### Para Acessibilidade
1. Sempre adicionar `AutomationProperties.Name`
2. Fornecer `HelpText` para campos complexos
3. Validar contraste de cores (mínimo 4.5:1)
4. Testar com leitura de tela
5. Manter suporte a teclado

### Para Auditoria
1. Registrar ações críticas
2. Usar JSON para valores complexos
3. Limpar registros antigos regularmente
4. Revisar estatísticas mensalmente
5. Manter logs para conformidade regulatória

---

## 🐛 Troubleshooting

### Idioma não muda
```csharp
// Verificar se LanguageManager está registrado
var langMgr = App.Services.GetService<LanguageManager>();
if (langMgr == null) { /* não está registrado */ }

// Forçar atualização de UI
if (App.Current?.MainWindow is Window w)
{
    w.InvalidateVisual();
}
```

### Relatório não exporta
```csharp
// Verificar permissões
var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
if (!Directory.Exists(desktop))
{
    // Desktop não existe
}

// Verificar dados carregados
if (!_relatoriosVM.PodeFazerExportacao)
{
    // Carregar dados primeiro
    await _relatoriosVM.CarregarDadosCommand.ExecuteAsync(null);
}
```

### Impressora não detectada
```csharp
// Executar como administrador
// Windows requer permissões para acessar impressoras

// Verificar status
if (string.IsNullOrEmpty(_printerVM.MensagemErro))
{
    // Sem erros, listar impressoras
    foreach (var p in _printerVM.ImpressorasDisponiveis)
    {
        Console.WriteLine($"{p.Name}: {(p.IsOffline ? "OFFLINE" : "ONLINE")}");
    }
}
```

---

## 📞 Suporte

Para questões técnicas sobre os novos serviços:
1. Consulte a documentação XML no código
2. Verifique os exemplos neste guia
3. Revise os testes de integração (quando disponíveis)
4. Consulte o arquivo `IMPLEMENTATION_REPORT.md`

---

**Versão:** 1.0  
**Data:** 01/09/2026  
**Status:** ✅ Pronto para Produção
