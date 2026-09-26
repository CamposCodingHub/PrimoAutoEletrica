# REGISTRO DE BUGS PARA ANÁLISE POSTERIOR — PRIMOX WORKSHOP

Data da Auditoria: 25/09/2026  
Branch: `cycle-c1/operational-intelligence`  
Commit: `8a397fe6e28ab3591848e59713cb077b02e2070a`  
Ambiente: Windows 11 Pro 64-bit | .NET 10 | SQLite CentsV1  
Status da Varredura: QA_WITH_FINDINGS

---

# BUG-001

Severidade: P1 — CRÍTICO  
Módulo: Base de Conhecimento (`BaseConhecimentoControl`) e Compras / Reposição (`NecessidadesCompraControl`)  
Tela: `BaseConhecimentoControl` e `NecessidadesCompraControl`  
Janela/Modal: UserControl / Visualização Principal  
Botão/Ação: Navegar para o módulo ou alternar tema Claro/Escuro  
Reprodução:
1. Abrir o executável instalado (`PrimoAutoEletrica.exe`).
2. Realizar login com usuário autenticado (ex.: `smoke-admin@primoauto.com` ou `Administrador`).
3. Clicar no menu lateral "Base de Conhecimento" ou "Necessidades de Compra".
Esperado: A tela do módulo correspondente deve carregar com abas operacionais, indicadores e listagens.  
Obtido: Exceção XAML fatal disparada durante o carregamento dos componentes visuais. O módulo não é renderizado.  
Mensagem exata:
`System.Windows.Markup.XamlParseException: O valor fornecido em 'System.Windows.StaticResourceExtension' iniciou uma exceção.`  
Exception: `System.Windows.Markup.XamlParseException`  
InnerException: `System.Windows.ResourceReferenceKeyNotFoundException: Não foi possível localizar o recurso 'ModernTabControl'.`  
StackTrace:
```
   at System.Windows.Markup.WpfXamlLoader.Load(XamlReader xamlReader, IXamlObjectWriterFactory writerFactory, Boolean skipJournaledProperties, Object rootObject, XamlObjectWriterSettings settings, Uri baseUri)
   at System.Windows.Markup.XamlReader.LoadBaml(Stream stream, ParserContext parserContext, Object parent, Boolean closeStream)
   at System.Windows.Application.LoadComponent(Object component, Uri resourceLocator)
   at PrimoAutoEletrica.UserControls.BaseConhecimentoControl.InitializeComponent() in C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\UserControls\BaseConhecimentoControl.xaml:line 1
   at PrimoAutoEletrica.UserControls.BaseConhecimentoControl..ctor()
   at PrimoAutoEletrica.Services.NavigationService.GetOrCreateControl(String moduleName) in C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\Services\NavigationService.cs:line 241
```
Arquivo:
- `c:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\UserControls\BaseConhecimentoControl.xaml`
- `c:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\UserControls\NecessidadesCompraControl.xaml`  
Linha:
- `BaseConhecimentoControl.xaml`: linhas 207 (`ModernTabControl`), 213, 334, 463 (`ModernTabItem`)
- `NecessidadesCompraControl.xaml`: linhas 245 (`ModernTabControl`), 251, 386 (`ModernTabItem`)  
Método: `InitializeComponent()`  
SQL: N/A (Falha estática de marcação XAML)  
Frequência: 100% reproduzível (toda tentativa de instanciação ou navegação para o controle)  
Impacto: Bloqueia completamente o acesso aos módulos de Base de Conhecimento e Necessidades de Compra. Além disso, causa falha na suíte de testes de alternância de temas `Tema:ClaroEscuroModulosPrincipais`.  
Evidência: `QA_EVIDENCE/BUG-001/BUG-001_EVIDENCE.md` e logs da suíte de testes em `Temp/PrimoAuto_Automated/AutomatedTests/ui-smoke-test-20260925-201500-24088/Logs/app-2026-09-25.log`.

---

# BUG-002

Severidade: P2 — ALTO  
Módulo: Estoque & Catálogo de Peças (`CatalogoPecas`)  
Tela: `CatalogoPecasControl`  
Janela/Modal: UserControl / Tela Principal de Catálogo  
Botão/Ação: Botão / Comando "Cadastrar Produto" a partir de peça catalogada  
Reprodução:
1. Logar no aplicativo como usuário com perfil `Administrador`.
2. Navegar para a tela "Catálogo de Peças".
3. Observar o estado do botão para conversão de peça de catálogo em produto do estoque.
Esperado: O botão deve estar habilitado para perfis com permissão `ESTOQUE_CRIAR` (incluindo Administrador).  
Obtido: O botão fica desabilitado ou é rejeitado com mensagem no log: `Permissao negada. Usuario=...; Perfil=Administrador; Tipo=Modulo; Alvo=ESTOQUE_CRIAR; Codigo=N/A`.  
Mensagem exata:
`Permissao negada. Usuario=Smoke Test; Perfil=Administrador; Tipo=Modulo; Alvo=ESTOQUE_CRIAR; Codigo=N/A`  
Exception: N/A (Rejeição lógica de autorização)  
InnerException: N/A  
StackTrace:
```
   at PrimoAutoEletrica.Services.PermissionService.RegistrarPermissaoNegada(String tipo, String alvo, String codigoModulo) in C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\Services\PermissionService.cs:line 612
   at PrimoAutoEletrica.Services.PermissionService.TemPermissao(String modulo) in C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\Services\PermissionService.cs:line 211
   at PrimoAutoEletrica.ViewModels.CatalogoPecasViewModel..ctor(CatalogoPecasService catalogoPecasService, PermissionService permissionService) in C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\ViewModels\CatalogoPecasViewModel.cs:line 78
```
Arquivo: `c:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\ViewModels\CatalogoPecasViewModel.cs`  
Linha: 78  
Método: Construtor `CatalogoPecasViewModel`  
SQL: N/A  
Frequência: 100% reproduzível  
Impacto: Impede a integração direta entre o catálogo de peças e a criação de produtos no estoque, forçando o usuário a cadastrar o produto manualmente do zero no módulo de Estoque.  
Evidência: `QA_EVIDENCE/BUG-002/BUG-002_EVIDENCE.md` e logs de auditoria em `AuditLogs` / `app-2026-09-25.log`.

---

# BUG-003

Severidade: P1 — CRÍTICO  
Módulo: Ferramentas & Equipamentos (`FerramentasControl`)  
Tela: `FerramentasControl`  
Janela/Modal: UserControl / Tela Principal de Ferramentas  
Botão/Ação: Navegar para o módulo de Ferramentas pelo menu lateral  
Reprodução:
1. Abrir o aplicativo `PrimoAutoEletrica.exe`.
2. Efetuar login com usuário autenticado.
3. No menu lateral, clicar na opção "Ferramentas".
Esperado: O painel de Ferramentas deve abrir exibindo os cards de KPI (Total, Disponíveis, Em Uso, Manutenção) e o grid de ferramentas cadastradas.  
Obtido: Exceção `NullReferenceException` fatal é lançada antes da tela se tornar visível. Um diálogo de erro de navegação é exibido ao usuário.  
Mensagem exata:
`System.NullReferenceException: Object reference not set to an instance of an object.`  
Exception: `System.NullReferenceException`  
InnerException: N/A  
StackTrace:
```
   at PrimoAutoEletrica.UserControls.FerramentasControl.AplicarFiltros() in C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\UserControls\FerramentasControl.xaml.cs:line 87
   at PrimoAutoEletrica.UserControls.FerramentasControl.FiltroCombo_SelectionChanged(Object sender, SelectionChangedEventArgs e) in C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\UserControls\FerramentasControl.xaml.cs:line 206
   at System.Windows.Controls.ComboBox.OnSelectionChanged(SelectionChangedEventArgs e)
   at PrimoAutoEletrica.UserControls.FerramentasControl.InitializeComponent() in C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\UserControls\FerramentasControl.xaml:line 1
   at PrimoAutoEletrica.UserControls.FerramentasControl..ctor() in C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\UserControls\FerramentasControl.xaml.cs:line 20
   at PrimoAutoEletrica.Services.NavigationService.GetOrCreateControl(String moduleName) in C:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\Services\NavigationService.cs:line 241
```
Arquivo: `c:\Projetos\PrimoAutoEletrica\PrimoAutoEletrica\UserControls\FerramentasControl.xaml.cs` e `FerramentasControl.xaml`  
Linha: 87 e 206  
Método: `AplicarFiltros()`  
SQL: N/A  
Frequência: 100% reproduzível (toda tentativa de abrir a tela de Ferramentas)  
Impacto: Bloqueia 100% da utilização do módulo de Ferramentas, Tool 360, retiradas e devoluções pelo shell principal.  
Evidência: `QA_EVIDENCE/BUG-003/BUG-003_EVIDENCE.md` e logs da aplicação em `app-2026-09-25.log`.
