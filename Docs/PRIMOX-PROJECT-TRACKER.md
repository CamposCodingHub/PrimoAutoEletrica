# PRIMOX Workshop — Acompanhamento do Projeto

**Documento vivo para acompanhamento futuro**  
**Atualizado:** 2026-09-10  
**Produto:** PRIMOX Workshop (PrimoAutoEletrica)  
**Versão comercial tag:** `v1.0.0` = `72d85fa`  
**HEAD no momento deste documento:** `736682e` (I18N-07 fechada)  
**Repositório:** https://github.com/CamposCodingHub/PrimoAutoEletrica  

---

## 1. O que é o produto

PRIMOX Workshop é um sistema desktop Windows (WPF · .NET 6) para oficinas de auto elétrica / manutenção automotiva.

**Módulos principais**
- Dashboard operacional
- Clientes · Veículos · Ordens de Serviço · Orçamentos
- PDV / caixa
- Estoque · Catálogo de peças
- Financeiro
- Agenda
- Funcionários · Fornecedores
- Relatórios · Auditoria
- Operações fiscais (NF-e foundation / homologação)
- Help Center · Configurações · Login / sessão

**Regra de negócio crítica (i18n)**  
`CurrentCulture` de formatação (moeda, número, datas de domínio) permanece **pt-BR** mesmo com UI em EN/ES.

---

## 2. Estado atual (snapshot 2026-10-03)

| Área | Status | Notas |
|------|--------|-------|
| Release comercial `v1.0.0` | **GO** (tag protegida) | Tag legada de 1.0.0 preservada |
| **Release Oficial `v2.0.0`** | **RELEASED (100% GO)** | Versão oficial 2.0.0.0 com Compras Anti-Ruptura, Controle Ferramental, Copilot IA, Hardening de Banco de Dados, Notificações WhatsApp e Instaladores Multiplataforma |
| **Enterprise Corporativo (B2B/Frotas/Multi-Filial)** | **100% CONCLUÍDO (ENTERPRISE-GRADE)** | Implementação completa dos 6 pilares: Multi-Filial persistente, Romaneio & Transferência de Estoque Inter-Lojas, Gestão de Frotas B2B & Telemetria, Licenciamento HMAC Anti-Tampering & HWID, Gateway Fiscal NFS-e/NFC-e e Etiquetas ZPL II para Oficina 4.0 |
| Instalador Windows | **CONCLUÍDO** | Inno Setup `PRIMOX-Workshop-Setup-2.0.0.exe` e Pacote Portable `.zip` |
| Instalador Linux | **CONCLUÍDO** | Script automatizado `install.sh`, launcher nativo com WinePrefix isolado `primox-launcher.sh`, atalho `.desktop` e pacote `.tar.gz` |
| Bateria de Testes & Hardening | **CONCLUÍDO** | Testes de concorrência pessimista multioficina (`RegistroBloqueioService`), segurança PBKDF2, integridade ACID, persistência enterprise e suite xUnit |
| Internacionalização | **YELLOW — CLOSED** | I18N-07 encerrou a frente (Strict ~93% EN/ES) |
| **Benchmark Evolution Roadmap (Doutor-IE/Mitchell 1/Tekmetric)** | **100% CONCLUÍDO (FASES 1 A 6 100% GO)** | Fases 1 a 6 concluídas com louvor e homologadas com 114/114 testes (100.0%) sob Wine: Fase 1 (DVI 2.0 Inspeção Digital Fotográfica + WhatsApp wa.me), Fase 2 (Ponte 1-Click IA Copilot ➔ OS & Estoque), Fase 3 (Bancada de Esquemas Interativos 2.0 com Trace Wire Neon), Fase 4 (Base SureTrack Local da Oficina com Estatísticas de Falhas), Fase 5 (Biblioteca Técnica Automotiva Nacional & Linha Pesada 24V com Centrais de Fusíveis/Relés HB20, Onix, Gol, Corolla, Fiat Fire, Scania e Atego), Fase 6 (Assistente Neural de Diagnóstico Guiado por Passo a Passo / Troubleshooting Flowcharts & Calculadora de Queda de Tensão SAE/DIN) e Hardening de Alta Precisão (Roteamento determinístico de caixas de fusíveis sem falsos positivos clínicos). |

### Pilares Enterprise Corporativo (Grandes Redes e Frotistas)
1. **Multi-Filial Corporativo & Transferência Inter-Lojas (`FilialService`, `TransferenciaEstoqueService`):**
   - Suporte nativo a matriz e filiais descentralizadas com persistência real em banco de dados (`Filiais`, `TransferenciasEstoque`, `TransferenciasEstoqueItens`).
   - Ciclo ACID de transferência: Solicitada -> Despachada (baixa física da origem para trânsito) -> Recebida (conferência física no destino) ou Cancelada (estorno automático).
2. **Gestão de Frotas & Contratos B2B (`GestaoFrotasService`):**
   - Gestão de contratos corporativos com tabela diferenciada de mão de obra e descontos automáticos de peças/serviços.
   - Vinculação de veículos de frota com telemetria de KM e Horímetro, disparando alertas proativos de manutenção preventiva elétrica.
   - Fechamento periódico agrupado de faturas (`FecharFaturaPeriodicaAsync`) com conciliação de OSs.
   - Geração e validação de tokens de aprovação remota via HMAC-SHA256 (para aprovação via WhatsApp/Email pelo frotista).
3. **Licenciamento Comercial SaaS & Anti-Pirataria (`LicenseService`, `LicenseActivationWindow`):**
   - Assinatura digital inviolável HMAC-SHA256 e detecção anti-tampering (bloqueia adulteração de arquivos de licença).
   - Identificador exclusivo de máquina estável (Hardware ID / HWID) e limite de filiais por licença (`MaxFiliais`).
   - Ativação imediata via Token Criptografado portátil (`PRMX-...`) e geração automática de Trial de 15 dias.
4. **Gateway Fiscal Municipal & Balcão (`GatewayFiscalCorporativoService`):**
   - Emissão de NFS-e Municipal para serviços de mão de obra automotiva (CNAE 4520-0/07, LC 116 item 14.01, ISSQN retido ou próprio).
   - Emissão de NFC-e (modelo 65) para vendas de balcão no PDV com cálculo Simples Nacional e QR-Code SEFAZ.
   - Gerador de Cupom Térmico NFC-e formatado para impressoras térmicas não-fiscais (80mm/58mm).
5. **Oficina 4.0 - Gerador de Etiquetas Térmicas Zebra ZPL II (`EtiquetaTermicaZplService`):**
   - Etiquetas de Chaveiro/Chave do Veículo (OS, placa grande em destaque, cliente e código de barras Code 128).
   - Etiquetas de Almoxarifado para caixas de peças com EAN-13/Code 128, marca, preço e localização (rua/prateleira).
   - Etiquetas Patrimoniais para ferramentaria com QR-Code 2D para check-in/check-out instantâneo no leitor óptico.
   - Envio direto para impressoras térmicas via rede TCP (porta 9100) e exportação em arquivos `.zpl`.

### Decisão I18N vigente

**YELLOW — CLOSED WITH EXPLICIT NON-BLOCKING EXCEPTIONS**

Significa: a internacionalização **está fechada** para uso real PT/EN/ES nos fluxos principais.  
Não abrir I18N-08 automaticamente. Exceções: Help Extended, filtros técnicos de auditoria, dados reais, termos fiscais, brand, UNKNOWN não user-visible.

Docs: `Docs/qa/PRIMOX-I18N-07-FINAL-GATE.md`

---

## 3. Arquitetura resumida

```
UI (XAML / code-behind / ViewModels)
  → LocalizationHelper / UiText.T
  → LocalizationService
      Pt / En / Es
      + Modules + Interaction + Content + Closure + Gate
  → language_settings.json (%LOCALAPPDATA%)
```

**Stack**
- WPF · net6.0-windows
- SQLite (Microsoft.Data.Sqlite)
- Testes: xUnit (`Tests/PrimoAutoEletrica.Tests`)
- Smoke UI: `Scripts/Run-UiSmoke.ps1` + `UiSmokeTestService.*`

**Deploy local (atalho da área de trabalho)**
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Scripts/Deploy-ToInstalledApp.ps1 -ForceStop -Launch
```
Instalação típica: `%LOCALAPPDATA%\PrimoAutoEletrica\App` (ou caminho do atalho PRIMOX).

---

## 4. Histórico de frentes relevantes (ordem cronológica recente)

| Frente | Resultado | Docs chave |
|--------|-----------|------------|
| Release Gate 1.0.0 | GO | PROJECT_STATUS · tag `v1.0.0` |
| Fase 15 A–E packaging/install | GO com limitações | Docs/qa packaging |
| Fiscal Foundation / Ops Center | Foundation ready | Docs fiscal |
| I18N-01 Core | YELLOW | PRIMOX-I18N-AUDIT |
| I18N-02 Modules | YELLOW | PRIMOX-I18N-MODULE-COVERAGE |
| I18N-03 Interaction | YELLOW | PRIMOX-I18N-INTERACTION-LOCALIZATION |
| I18N-04 UX audit | YELLOW | PRIMOX-I18N-04-* |
| I18N-05 Content | YELLOW | PRIMOX-I18N-05-* |
| I18N-06 Closure | YELLOW | PRIMOX-I18N-06-* |
| **I18N-07 Final Gate** | **YELLOW CLOSED** | **PRIMOX-I18N-07-*** |

Histórico detalhado vivo: `PROJECT_STATUS.md` (topo = mais recente).

---

## 5. Internacionalização — mapa rápido

### Idiomas
- **pt-BR** — padrão / fallback
- **en-US** — UI completa nos fluxos críticos
- **es-ES** — UI completa nos fluxos críticos

### O que NÃO traduzir
- Dados cadastrais reais
- Status internos de máquina de estados (IDs em PT técnico; só display localizado)
- Códigos fiscais (NCM, CFOP, CST…)
- Logs, SQL, nomes de classes/tabelas
- Marca PRIMOX

### Métricas I18N-07 (pós-fechamento)
- Static ~34,6% (não é critério de fechamento)
- TRANSLATION_REQUIRED ~322
- User-visible strict: PT 100% · EN 93,3% · ES 93,3%
- Catalog Missing EN/ES = 0

### Como revalidar
```powershell
dotnet build PrimoAutoEletrica/PrimoAutoEletrica.csproj -c Release
dotnet test Tests/PrimoAutoEletrica.Tests/PrimoAutoEletrica.Tests.csproj -c Release --filter "FullyQualifiedName~Localization|FullyQualifiedName~Fiscal"
powershell -File Scripts/Run-UiSmoke.ps1 -Configuration Release -Framework net6.0-windows -SmokeFilter I18n07 -SkipBuild
powershell -File Scripts/Audit-I18nCoverage.ps1
powershell -File Scripts/Audit-I18nCatalog.ps1
```

Evidência visual local (gitignored): `Logs/qa-visual/i18n-07/`

---

## 6. QA — suite de referência

| Suite | Expectativa | Como rodar |
|-------|-------------|------------|
| Build Release | 0 errors | `dotnet build … -c Release` |
| Localization + Fiscal | 69/69 (pós I18N-07) | `dotnet test` filter Loc/Fiscal/Nfe/Focus |
| QaEngine | 43/43 | `Run-UiSmoke.ps1 -SmokeFilter QaEngine` |
| DeepQa (+ LongRun) | 6/6 | `-SmokeFilter DeepQa` |
| ExhaustiveUi | PASS | `-SmokeFilter ExhaustiveUi` |
| DB | integrity ok · FK 0 | PRAGMA no `primoauto.db` de smoke |

**Não** remover testes nem alterar labels só para “passar” QA.

---

## 7. Regras de ouro (para próximos agentes / desenvolvedores)

1. **Não mover** a tag `v1.0.0` (`72d85fa`).
2. **Não** `git reset --hard` / `git clean -fd` destrutivo sem pedido explícito.
3. WIP fiscal (`Docs/qa/PRIMOX-FISCAL-LIVE-HOMOLOGATION-REPORT.md` e afins) **separar** de commits de outras frentes.
4. I18N: preferir `UiText.T` / `LocalizationHelper` / catálogo existente; novas strings UI nascem em PT+EN+ES.
5. Não traduzir dados reais nem alterar regras fiscais / autenticação / banco “de passagem”.
6. Após I18N-07: **não** abrir I18N-08 automático; próxima fase = decisão humana.
7. Commits: mensagens curtas focadas no *porquê*; não incluir `bin/`, `obj/`, logs, secrets.

---

## 8. Próximas frentes candidatas (decisão manual)

Ordem sugerida para análise humana (não automática):

1. **Fiscal Live / homologação real** — se WIP e ambiente estiverem prontos  
2. **NFC-e / NFS-e** — expansão fiscal  
3. **Installer comercial / code signing** — packaging  
4. **Help Extended EN/ES** — opcional, P3  
5. **SaaS / auto-update** — longo prazo  

Cada frente deve ter baseline próprio, QA verde e relatório em `Docs/qa/`.

---

## 9. Estrutura útil do repositório

```
PrimoAutoEletrica/          # App WPF
  Services/Localization*   # Catálogos i18n
  UserControls/ Views/     # UI
  Helpers/UiText.cs
Scripts/                   # Deploy, smoke, audits i18n
Tests/PrimoAutoEletrica.Tests/
Docs/qa/                   # Relatórios de fase (fonte da verdade de gates)
PROJECT_STATUS.md          # Histórico executivo vivo
Docs/PRIMOX-PROJECT-TRACKER.md  # Este arquivo — acompanhamento futuro
Installer/                 # Empacotamento
```

---

## 10. Checklist rápido pós-pull

- [ ] `git status` limpo (ou só WIP fiscal conhecido)
- [ ] `git rev-parse v1.0.0` == `72d85fa`
- [ ] Build Release OK
- [ ] Loc+Fiscal tests OK
- [ ] Deploy: `Deploy-ToInstalledApp.ps1 -ForceStop -Launch`
- [ ] Abrir app → trocar idioma PT→EN→ES → confirmar UI e persistência

---

## 11. Contatos de evidência I18N-07

| Artefato | Caminho |
|----------|---------|
| Gate final | `Docs/qa/PRIMOX-I18N-07-FINAL-GATE.md` |
| Matriz | `Docs/qa/PRIMOX-I18N-07-FINAL-MATRIX.md` |
| Residuais | `Docs/qa/PRIMOX-I18N-07-RESIDUALS.md` |
| Catálogo | `Docs/qa/PRIMOX-I18N-07-CATALOG-AUDIT.md` |
| Visual | `Docs/qa/PRIMOX-I18N-07-VISUAL-MATRIX.md` |
| Regressão | `Docs/qa/PRIMOX-I18N-07-REGRESSION.md` |
| Commits I18N-07 | `2f47039` · `f556fd8` · `736682e` |

---

*Manter este arquivo atualizado ao fechar cada frente maior (1 parágrafo + tabela de status no topo da seção 2).*
