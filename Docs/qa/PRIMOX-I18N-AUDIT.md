# PRIMOX-I18N-AUDIT — PRIMOX-I18N-2026-09

**Data:** 09–10/09/2026  
**Fase:** Localization / Internationalization  
**Produto:** PRIMOX Workshop 1.0.0  
**TFM:** `net6.0-windows`  
**HEAD inicial:** `0fe0a58` (`chore(release): harden commercial installer and signing readiness`)  
**Tag `v1.0.0`:** preservada (`72d85fa` / histórico intacto)

---

## 1. Estado anterior

O seletor de idioma no Header (`LanguageComboBox`) existia e chamava `LocalizationService.SetLanguage`, porém:

- a maior parte da UI era texto hardcoded em português no XAML;
- apenas ~18 bindings usavam `LocalizationHelper`;
- a propriedade `Logout` era referenciada no XAML **sem existir** no helper;
- **não havia persistência** da preferência de idioma;
- `LanguageManager.ChangeLanguage` só aceitava códigos de 2 letras (`pt`/`en`/`es`), incompatível com Tags `pt-BR`/`en-US`/`es-ES`;
- `.resx` thin (≈8 chaves) coexistia com dicionário parcial (~35 chaves).

**Classificação:** sistema **parcialmente implementado** (ComboBox + serviço incompleto), não localização ponta a ponta.

## 2. Causa encontrada

A troca de idioma atualizava `CultureInfo` / UICulture e um subconjunto mínimo de strings bound, mas **não** a Sidebar completa, Header contextual, Command Palette nem a maioria dos módulos. Sem persistência, o idioma voltava ao default no próximo start. Resultado observado pelo usuário: “seleciona e não traduz”.

## 3. Arquitetura escolhida

Reuso e endurecimento da pilha existente (sem arquitetura paralela):

```
UI (bindings / GetString)
  → LocalizationHelper (INotifyPropertyChanged)
    → LocalizationService (catálogo pt/en/es + fallback)
      → language_settings.json (persistência)
```

- **Fonte completa:** catálogo em memória (`BuildCatalog`) para pt-BR / en-US / es-ES.
- **`.resx` legado:** overlay opcional após o catálogo (não bloqueia chaves novas).
- **UICulture:** muda com o idioma.
- **CurrentCulture (números/moeda/datas de negócio):** permanece **pt-BR** (R$, decimais fiscais, PDV).

## 4. Idiomas suportados

| Código | UI label | Papel |
|--------|----------|-------|
| `pt-BR` | Português | Default + fallback |
| `en-US` | English | Oficial |
| `es-ES` | Español | Oficial |

## 5. Localização dos recursos

- `PrimoAutoEletrica/Services/LocalizationService.cs` — catálogo + persistência + cultura
- `PrimoAutoEletrica/Helpers/LocalizationHelper.cs` — propriedades XAML
- `PrimoAutoEletrica/Resources/Strings*.resx` — legado thin (não expandido nesta fase)
- Persistência: `%LOCALAPPDATA%\PrimoAutoEletrica\language_settings.json` (ou `App.RuntimeAppDataPath`)

## 6. Padrão de chaves

PascalCase semântico, inglês técnico estável:

`Dashboard`, `Clients`, `WorkOrders`, `SectionOperation`, `OpenModule`, `Logout`, …

Não misturar `client_title` / `TXT_CLIENTE` / `cliente.titulo`.

## 7. Persistência

`SetLanguage` grava JSON `{ "language": "en-US", "updatedAt": ... }`.  
`InitializeAtStartup()` / construtor recarrega; inválido → `pt-BR`.

## 8. Fallback

Ordem: catálogo do idioma → catálogo pt-BR → chave técnica (nunca string vazia / nunca exception).

## 9. Runtime

Troca **sem restart** no Shell:

- Sidebar / seções / Logout / tooltips / AutomationProperties
- título do módulo no Header
- Theme / Density labels
- Command Palette (itens gerados)
- Login: ComboBox de idioma no rodapé

Módulos internos com XAML hardcoded **não** re-traduzem automaticamente nesta fase (limitação honesta).

## 10. Limitações (honestas)

1. **Cobertura parcial de módulos:** ≈26 bindings `LocalizationHelper` vs ≈1500+ literais `Text="..."` em XAML; telas CRUD/PDV/Financeiro/OS ainda majoritariamente pt-BR no conteúdo.
2. **Help Center:** navegação shell localizada; **corpo da documentação** permanece pt-BR.
3. **Descrições de módulo / busca global:** subtítulos técnicos ainda pt-BR.
4. **Login:** seletor + persistência OK; labels internos (Email/Senha/Entrar) ainda hardcoded pt-BR nesta fase.
5. **Calendar nativo Dark:** limitação conhecida pré-existente — não alterada.
6. **Formatação monetária internacional:** deliberadamente **não** alterada (cultura de negócio = pt-BR).
7. **Toasts / MessageBox / validações** de módulos: em grande parte ainda pt-BR.

## 11. Testes

| Suite | Resultado |
|-------|-----------|
| Build Release | **0 errors** |
| Unit `LocalizationServiceTests` | **17/17 PASS** |
| Fiscal units (filtro Focus/Fiscal) | **incluídos no lote 51/51 PASS** |
| QaEngine (+ CompleteUi via filtro) | **43/43 PASS** (`TestResults/UiSmoke/2026-09-09_21-33-06`) |
| DeepQa / LongRun | **PASS** (`TestResults/UiSmoke/2026-09-09_21-41-00`) |
| ExhaustiveUi | **PASS** (`TestResults/UiSmoke/2026-09-09_21-43-54`, 1/1) |

`QaEngine:CompleteUiPlaceholdersI18n` PASS (pt/en/es placeholders).

## 12. Cobertura por área

| Área | Status |
|------|--------|
| Shell / Sidebar / Header seletor | **PASS** (runtime) |
| Command Palette | **PASS** (labels principais) |
| Login seletor | **PASS** (persistência compartilhada) |
| Dashboard / CRUD / PDV / OS / etc. conteúdo | **PARTIAL** (hardcoded residual) |
| Ajuda conteúdo | **PARTIAL** (shell sim / docs não) |
| Fiscal lógica | **não alterada** |

## 13. Pendências futuras

1. Migrar literais XAML dos módulos críticos para chaves `LocalizationHelper` / catálogo.
2. Localizar Login labels e diálogos/toasts de domínio.
3. Expandir `.resx` satélites ou manter catálogo único (escolher um e consolidar).
4. Traduzir Help Center por tópico (sem auto-tradução cega).
5. Revisar exportações CSV/PDF se cabeçalhos devem seguir UI language.

## Decisão

**YELLOW — Localization Core COMPLETE / READY WITH LIMITATIONS**

Sistema real de i18n no Shell + persistência + fallback + testes.  
**Não** declarar “100% internacionalizado”.
