# PRIMOX RELEASE-01 — PACKAGE AUDIT

> **RELATÓRIO DE AVANÇO / FASE — 2026-09-13**
>
> Este arquivo registra **melhorias e evidências da fase em que foi escrito**.
> **Não** é inventário operacional atual.
>
> Verdade atual: `Docs/CURRENT-TRUTH.md` · Crônica: `Docs/PRIMOX-ADVANCES-CHRONICLE.md` · Índice: `Docs/DOCUMENTATION-INDEX.md`
> HEAD pós-NET10-26: `1372e11` · TFM `net10.0-windows`

---

**Data:** 12/09/2026  
**Commit baseline:** `58b8e1f`  
**Publish:** `artifacts/publish/win-x64` · self-contained · RID `win-x64` · Trim=false · SingleFile=false · DebugType=None  

## Contagens (pós-política PDB)

| Item | Valor |
|---|---|
| Arquivos | **590** |
| DLL | ~583 |
| EXE | 2 (`PrimoAutoEletrica.exe`, `createdump.exe`) |
| JSON | 3 (deps/runtimeconfig) |
| SQL | 1 (`SqlServerSchema.sql` — OPTIONAL) |
| TXT | 1 (`ProdutosIniciais.txt` — seed template) |
| PDB | **0** (política comercial) |
| DB | **0** |

## Classificação

| Classe | Exemplos |
|---|---|
| REQUIRED | `PrimoAutoEletrica.exe`, runtime .NET, WPF/native deps, app DLLs |
| OPTIONAL | `createdump.exe`, `SqlServerSchema.sql`, `ProdutosIniciais.txt` |
| DEBUG ONLY | `*.pdb` — **excluídos** |
| LEGACY / UNEXPECTED | nenhum harness/test/FakeFiscal; nenhum `.db` |

## Secrets scan

Em textos do publish: apenas nomes NuGet (`JsonWebTokens` / `Tokens`) em `deps.json` — **não** credenciais reais.

## Metadata EXE (FileVersionInfo)

| Campo | Valor |
|---|---|
| ProductVersion | 1.0.0 |
| FileVersion | 1.0.0.0 |
| Company | CamposCodingHub |
| Product | PRIMOX Workshop |
| Signature | **UNSIGNED** |

## Hashes (artefato oficial pós PDB strip)

| Artefato | Size | SHA256 |
|---|---:|---|
| `PRIMOX-Workshop-Setup-1.0.0.exe` | 61 687 781 | `0BECE6AE5E7C582C51C3B81783DE8557A81F70881B82CA2505373433988607BF` |
| `PrimoAutoEletrica.exe` (publish) | 192 512 | `E5EE9DF4E3EE40E9F152AB1EC66061F47217DA0FD31EDCD6C90318BD1CDA9995` |

SHA difere de builds anteriores (PDB strip + rebuild) — esperado; não é bit-reproducible deterministic.
