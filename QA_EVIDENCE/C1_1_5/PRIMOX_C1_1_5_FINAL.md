# PRIMOX C1.1.5 FINAL — Close residual Owner-on-closed + certify Desktop

**Data:** 2026-09-26 (America/Sao_Paulo)  
**Branch:** `cycle-c1/operational-intelligence`  
**Commit:** `876e483197467bd2fef3a23154b4e31c1fa34f36`  
**Decisao:** **PASS**  
**C2 iniciado:** **NAO** (STOP apos C1.1.5)

## Residual fechado

`Interacao:Controle:BaseConhecimentoControl` — `InvalidOperationException: Nao e possivel definir a propriedade Owner para uma Janela fechada`.

## Classification (A–G)

**BOTH** (ver `QA_EVIDENCE/C1_1_5/classification/CLASSIFICATION.md`)

| ID | Hipotese | Veredito |
|----|----------|----------|
| A | Supervisor stale BeginInvoke fecha host seguinte | CONFIRMED (harness primario) |
| B | GetWindow retorna janela errada | REJECTED |
| C | Dialog ja fechado ao setar Owner | REJECTED |
| D | Off-screen IsVisible=false sozinho causa throw | REJECTED |
| E | App raw `Owner=` sem WindowOwnerHelper | CONFIRMED (bug real secundario) |
| F | Sem selecao so MessageBox | REJECTED (stack em AbrirArtigoDetalhes) |
| G | So timeout / empty catch | REJECTED (proibido) |

## Root cause

1. **Harness:** `AutomatedDialogSupervisor` agenda `HandleManagedDialogs` via `BeginInvoke`; `Dispose` so Cancel+Join(1s) nao drena a fila — BeginInvoke residual trata o proximo host (`Title=TypeName`, `Content=UserControl`) como dialog e chama `Close()`. Host fechado + `Owner = Window.GetWindow(this)` = throw.  
2. **App:** `BaseConhecimentoControl` / `FerramentasControl` usavam assign bruto de Owner em vez de `WindowOwnerHelper.ConfigureOwner` (ja usado em dezenas de telas).

## Fixes

1. `UiSmokeTestService.Types.cs` — Dispose drena `DispatcherPriority.ContextIdle`; `IsSmokeHostWindow` skip em HandleManagedDialogs.  
2. `UiSmokeTestService.Helpers.cs` — `EnsureHostWindowOperational` antes de `RaiseButtonClick` no generico.  
3. `BaseConhecimentoControl.xaml.cs` + `FerramentasControl.xaml.cs` — `WindowOwnerHelper.ConfigureOwner`.  
4. Testes: `BaseConhecimentoOwnerHarnessRegressionTests` (4 facts).

Constraints respeitadas: BaseConhecimento permanece na bateria; sem empty catch de produto; timeouts nao foram so elevados.

## Testes

| Suite | Resultado |
|-------|-----------|
| Unit Debug full | **510 PASS / 0 FAIL** |
| Novos C1.1.5 | 4 PASS |
| Bug regression filter (001–007 + related) | 50 PASS |

## Artefato Desktop

| Campo | Valor |
|-------|-------|
| EXE | `%LOCALAPPDATA%\PrimoAutoEletrica\App\PrimoAutoEletrica.exe` |
| EXE SHA256 | `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B` |
| DLL SHA256 | `9E74F733F33D7A3A9017E4F1ACB1669BD8DB5B1D1A3DBC12E0D7C461B49DB683` |
| Identity match publish | **True** |
| Shortcut | `PRIMOX Workshop.lnk` → App EXE / WorkingDirectory App |

## UiSmoke App EXE real

- Preflight: `QA_EVIDENCE/C1_1_5/after_publish/uismoke_preflight.txt`
- Resultado: **206 PASS / 0 FAIL / Total 206**
- Residual: **PASS** (`Interacao:Controle:BaseConhecimentoControl` 3973 ms)
- Ferramentas: **PASS** (7121 ms); `owner_on_closed_errors=0`
- Focused residual (Clientes→BC→Ferramentas): 3/3 PASS

## DB

Protected `primoauto.db` SHA: **INTACTO** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B`

## Light/Dark / resolucoes / E2E / P01–P25

Ver `QA_EVIDENCE/C1_1_5/manual/matriz_tema_resolucao_e2e_p01p25.md`  
- Tema UiSmoke: PASS  
- Manual Light/Dark por pagina, resolucoes, E2E dedicado, P01–P25 operador: **NOT_TESTED** (honesto)

## Evidencias

- `QA_EVIDENCE/C1_1_5/` (baseline, reproduce, classification, patches, unit_tests, after_publish, manual)
- Docs: `Docs/audit/2026-09-20/PRIMOX_C1_1_5_FINAL.md` + `PRIMOX_C1_1_5_FINAL.md` (raiz evidencia)

## Git

- Commit mensagem: `C1.1.5: close final QA residual and certify Desktop`
- Push **somente** `cycle-c1/operational-intelligence`
- `main` nao alterado / nao mergeado

## Proximo

**STOP.** Nao iniciar C2.
