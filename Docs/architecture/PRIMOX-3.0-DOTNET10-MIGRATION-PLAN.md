# PRIMOX 3.0 — .NET 10 LTS MIGRATION PLAN
**Data:** 04 de Outubro de 2026  
**Versão:** 3.0.0-DOTNET10  
**Status:** PLANO DE MIGRAÇÃO TECNOLÓGICA E PADRONIZAÇÃO DE SDK  
**SDK de Destino:** .NET 10.0 LTS (Host atual: `10.0.112` / Runtime `10.0.12`)

---

## 1. INVENTÁRIO ATUAL DE TARGET FRAMEWORKS (TFMs)

A auditoria identificou fragmentação perigosa de TFMs entre os projetos da solução:

| Projeto | Caminho | TFM Atual | Status de Suporte |
| :--- | :--- | :---: | :--- |
| **`PrimoAutoEletrica`** | `PrimoAutoEletrica/PrimoAutoEletrica.csproj` | `net6.0-windows` | **EOL (Fora de Suporte Oficial)** |
| **`PrimoAutoEletrica.Api`** | `PrimoAutoEletrica.Api/PrimoAutoEletrica.Api.csproj` | `net9.0-windows` | Em suporte temporário STS |
| **`PrimoAutoEletrica.Tests`** | `Tests/PrimoAutoEletrica.Tests/PrimoAutoEletrica.Tests.csproj` | `net10.0-windows` | Alinhado com SDK 10 |
| **`PrimoAutoEletrica.UiTests`**| `PrimoAutoEletrica.UiTests/PrimoAutoEletrica.UiTests.csproj` | `net6.0` | **EOL (Fora de Suporte Oficial)** |
| **`Tools.DbConfigurator`** | `Tools/DbConfigurator/DbConfigurator.csproj` | `net9.0` | Em suporte temporário STS |
| **`Tools.LocalSyncSimulator`**| `Tools/LocalSyncSimulator/LocalSyncSimulator.csproj` | `net9.0` | Em suporte temporário STS |

---

## 2. ANÁLISE DE DEPENDÊNCIAS E COMPATIBILIDADE NUGET

| Pacote NuGet | Versão Atual | Compatibilidade com .NET 10 | Ação Recomendada |
| :--- | :---: | :---: | :--- |
| **`Microsoft.Data.Sqlite`** | 7.0.20 | Compatível (com warnings) | Atualizar para `9.0.x` / `10.0.x` |
| **`Microsoft.Data.SqlClient`** | 5.2.2 | Totalmente compatível | Manter em versão estável `5.2.2+` |
| **`Dapper`** | 2.1.35 | Totalmente compatível | Manter `2.1.35` |
| **`CommunityToolkit.Mvvm`** | 8.3.2 | Totalmente compatível | Manter `8.3.2` |
| **`SixLabors.ImageSharp`** | 3.1.12 | Totalmente compatível | Manter `3.1.12` |
| **`EPPlus`** | 8.7.0 | Totalmente compatível | Manter `8.7.0` |
| **`PdfSharpCore`** | 1.3.56 | Totalmente compatível | Manter `1.3.56` |
| **`Otp.NET`** | 1.4.1 | Totalmente compatível (.NET Standard 2.0) | Manter `1.4.1` |
| **`System.Security.Cryptography.Xml`** | 9.0.18 | Compatível | Alinhar para `10.0.0` quando disponível |

---

## 3. RISCOS TÉCNICOS MAPEADOS

1. **Compilação Multi-Plataforma de WPF (Linux vs Windows):**
   * O WPF (`UseWPF=true`) depende do runtime `Microsoft.WindowsDesktop.App`.
   * No Linux, para permitir que `dotnet build` compile código direcionado a Windows sem falha `NETSDK1100`, é obrigatório configurar `<EnableWindowsTargeting>true</EnableWindowsTargeting>`.
2. **Desacoplamento da API Web:**
   * A API web **não deve** ter `-windows` em seu TFM.
   * Para alcançar isso, os modelos e contratos devem ser movidos para projetos de biblioteca de classes puros (`net10.0`), permitindo que a API seja `net10.0` sem qualquer dependência de Windows Desktop.
3. **Quebra de APIs Internas do WPF:**
   * A transição de .NET 6 para .NET 10 no WPF é majoritariamente compatível em nível binário, mas requer validação do renderizador DirectWrite e temas XAML.

---

## 4. SEQUÊNCIA DE MIGRAÇÃO EM 5 ETAPAS

```
[Etapa 1: Governança de Build Centralizada]
  Criação de Directory.Build.props e Directory.Packages.props na raiz da solução.
  Configuração de LangVersion=latest, Nullable=enable, EnableWindowsTargeting=true.
      │
      ▼
[Etapa 2: Elevação dos Projetos de Ferramentas e Testes]
  Migração de DbConfigurator, LocalSyncSimulator e Tests para net10.0 unificado.
      │
      ▼
[Etapa 3: Desacoplamento da Web API]
  Remoção da dependência direta de PrimoAutoEletrica.csproj.
  Migração do TFM da API de net9.0-windows para net10.0 puro (cross-platform).
      │
      ▼
[Etapa 4: Elevação do Projeto Principal WPF]
  Atualização de PrimoAutoEletrica.csproj de net6.0-windows para net10.0-windows.
  Alinhamento de pacotes Microsoft.Extensions.* para 10.0.
      │
      ▼
[Etapa 5: Validação da Suite de Testes e Homologação]
  Execução completa de build Release e suíte de 114 testes automatizados.
```

---

## 5. ESTRUTURA DOS ARQUIVOS DE GOVERNANÇA

### 5.1 `Directory.Build.props` (Raiz)
```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>13.0</LangVersion>
    <EnableWindowsTargeting>true</EnableWindowsTargeting>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
</Project>
```
