# -*- coding: utf-8 -*-
"""
Pack 4 of B5.0 Documents:
18. B5_0_EXECUTIVE_BACKLOG.csv
19. B5_0_FINAL.md
"""

import os
import csv

DOC_DIR = r"c:\Projetos\PrimoAutoEletrica\Docs\audit\2026-09-20"
os.makedirs(DOC_DIR, exist_ok=True)

# -------------------------------------------------------------
# 18. B5_0_EXECUTIVE_BACKLOG.csv
# -------------------------------------------------------------
backlog_data = [
    ("B5-001", "Interface Grafica Dedicada de Pos-Venda (Garantias e Retornos)", "Pos-Venda", "PosVendaService", "P1", "UI / Frontend", "PLANEJADO", "Unit + UI Smoke", "v1.0-rc1", "Nenhum"),
    ("B5-002", "Interface Grafica do Checklist Tecnico Multiponto na OS", "AutoEletricaTecnica", "ChecklistTecnicoOS", "P1", "UI / Frontend", "PLANEJADO", "Unit + UI Smoke", "v1.0-rc1", "Nenhum"),
    ("B5-003", "Polimento Visual e Integracao DVI Local com Vehicle360", "DVI", "DviChecklistService", "P2", "UI / Integracao", "PLANEJADO", "UI Smoke", "v1.0-rc1", "Nenhum"),
    ("B5-004", "Gerador de Laudo Eletrico Tecnico em PDF (Antes/Depois Delta)", "AutoEletricaTecnica", "DiagnosticoTecnicoService", "P2", "Feature", "PLANEJADO", "Unit + E2E", "v1.0-rc1", "Nenhum"),
    ("B5-005", "Utilitario de Migracao de Banco e Rehearsal em Homologacao", "Banco / Money", "DatabaseService", "P1", "Ferramenta / Script", "PLANEJADO", "Migration Rehearsal", "v1.0-rc1", "Nenhum"),
    ("B5-006", "Script Oficial de Empacotamento Inno Setup (primox_setup.iss)", "Instalador", "Inno Setup Compiler", "P1", "DevOps / Release", "PLANEJADO", "Instalacao Limpa", "v1.0-rc1", "Nenhum"),
    ("B5-007", "Detector de Pre-requisito .NET 10 Desktop Runtime no Instalador", "Instalador", "Inno Setup Pascal Script", "P2", "DevOps / Release", "PLANEJADO", "Instalacao Limpa", "v1.0-rc1", "Nenhum"),
    ("B5-008", "Camada de Abstracao de Certificado Digital A1 com DPAPI", "Fiscal", "CryptoService", "P2", "Seguranca / Backend", "PLANEJADO", "Unit Tests", "v1.0-rc2", "Nenhum"),
    ("B5-009", "Gerador de Licencas Criptograficas Offline (RSA-4096)", "Licenciamento", "LicenseService", "P2", "Seguranca / Backend", "PLANEJADO", "Unit Tests", "v1.0-rc2", "Nenhum"),
    ("B5-010", "Coletor de Diagnostico de Suporte (Exportacao de Logs com 1 Clique)", "Suporte", "DatabaseBackupService", "P2", "Feature", "PLANEJADO", "Smoke Test", "v1.0-rc2", "Nenhum"),
    ("B5-011", "Rotina de Backup Automatico ao Fechar a Aplicacao", "Backup", "DatabaseBackupService", "P1", "Confiabilidade", "PLANEJADO", "Integration Tests", "v1.0-rc1", "Nenhum"),
    ("B5-012", "Exportacao de Ficha e Historico do Cliente para Conformidade LGPD", "Clientes", "Primox360Service", "P3", "Compliance", "PLANEJADO", "Unit Tests", "v1.0-rc2", "Nenhum"),
    ("B5-013", "Impressao de Comprovante de Abertura/Fechamento de Caixa 80mm", "Caixa", "FinanceiroDatabaseService", "P2", "Feature", "PLANEJADO", "Smoke Test", "v1.0-rc1", "Nenhum"),
    ("B5-014", "Visualizacao de Enderecamento Fisico de Pecas na Requisicao da OS", "Estoque", "ProdutoRepository", "P3", "UI / Ergonomia", "PLANEJADO", "UI Smoke", "v1.0-rc2", "Nenhum"),
    ("B5-015", "Envio Formatado de Proposta de Orcamento com Link wa.me", "Orcamentos", "OrcamentoDatabaseService", "P2", "Feature", "PLANEJADO", "Smoke Test", "v1.0-rc1", "Nenhum"),
    ("B5-016", "Monitoramento de Consistencia Temporal contra Atraso de Relogio", "Seguranca", "LicenseService", "P2", "Seguranca", "PLANEJADO", "Unit Tests", "v1.0-rc2", "Nenhum"),
    ("B5-017", "Filtro Global de Sanitizacao de Logs para Mascaramento de Segredos", "Auditoria", "DatabaseService", "P1", "Seguranca", "PLANEJADO", "Security Tests", "v1.0-rc1", "Nenhum"),
    ("B5-018", "Homologacao SEFAZ de NF-e em Ambiente de Testes com Certificado Real", "Fiscal", "FiscalDatabaseService", "P1", "Integracao", "BLOQUEADO", "SEFAZ Sandbox", "v1.1 (Fase B7)", "Certificado A1 e CNPJ Homologado"),
    ("B5-019", "Migracao Fisica Definitiva de Money no Banco de Producao", "Banco / Money", "DbMigrationTool", "P1", "Migracao", "BLOQUEADO", "Data Proof 100%", "v1.1 (Fase B7)", "Aprovacao formal do Gate de Producao"),
    ("B5-020", "Servidor Web de Ativacao Remota e Revogacao de Licencas", "Licenciamento", "API Cloud ASP.NET", "P3", "Cloud / SaaS", "FUTURO", "Integration Tests", "v2.0 (Fase B8)", "Infraestrutura Cloud / Servidor"),
    ("B5-021", "Portal do Cliente Web para Aprovacao Remota de DVI e Orcamento", "DVI", "Next.js / Cloud Storage", "P3", "Cloud / SaaS", "FUTURO", "E2E Web Tests", "v2.0 (Fase B8)", "Infraestrutura Cloud / Storage S3"),
    ("B5-022", "Aplicativo Mobile do Mecanico para Pátio e Box", "Operacional", "Flutter / React Native", "P3", "Mobile", "FUTURO", "Mobile Tests", "v2.0 (Fase B8)", "API REST Nuvem"),
    ("B5-023", "Sincronizacao de Matriz e Filiais em Nuvem (Multi-Loja)", "Corporativo", "Sync Engine Cloud", "P4", "Cloud / SaaS", "FUTURO", "Sync Tests", "v2.0 (Fase B8)", "Servidor Central Nuvem"),
    ("B5-024", "Integracao Oficial WhatsApp Business Platform (API Meta)", "Comunicacao", "Meta Graph API", "P3", "Integracao", "FUTURO", "API Tests", "v1.1 (Fase B8)", "Conta Empresarial Meta Verificada"),
    ("B5-025", "Integracao de Maquininha de Cartao TEF no PDV Balcao", "PDV", "Provedor TEF (Sitef)", "P4", "Integracao", "FUTURO", "Hardware Tests", "v2.0 (Fase B8)", "Contrato de Homologacao TEF")
]

with open(os.path.join(DOC_DIR, "B5_0_EXECUTIVE_BACKLOG.csv"), "w", encoding="utf-8", newline="") as f:
    writer = csv.writer(f)
    writer.writerow(["ID", "Tarefa", "Modulo", "Dependencia", "Prioridade", "Tipo", "Status", "Teste", "Release", "Bloqueador"])
    for r in backlog_data:
        writer.writerow(r)

print("18. B5_0_EXECUTIVE_BACKLOG.csv OK")

# -------------------------------------------------------------
# 19. B5_0_FINAL.md
# -------------------------------------------------------------
final_md = """# PRIMOX WORKSHOP — FASE B5.0
## RELATÓRIO FINAL DE DESCOBERTA E ARQUITETURA DE RELEASE

**Fase:** B5.0 (Commercial Readiness Discovery & Release Architecture)  
**Data:** 2026-09-24  
**Branch:** `audit/product-discovery-2026-09`  
**Commit:** `72e46ac`  
**Status do Gate:** **PASS (DESCOBERTA CONCLUÍDA COM SUCESSO)**

---

### Respostas Objetivas às 24 Perguntas Obrigatórias de Auditoria (Seção 52)

#### 1. Quantas funcionalidades existem no inventário?
Existem exatamente **57 funcionalidades** mapeadas e rastreadas ponta a ponta na matriz [B5_0_FEATURE_TRUTH_MATRIX.csv](file:///c:/Projetos/PrimoAutoEletrica/Docs/audit/2026-09-20/B5_0_FEATURE_TRUTH_MATRIX.csv).

#### 2. Quantas são CORE?
**40 funcionalidades** são estritamente CORE (possuem tela, ViewModel, Service, Repository, persistência funcional em SQLite/JSON, testes automatizados e validação desktop).

#### 3. Quantas são PARTIAL?
**6 funcionalidades** são PARTIAL:
- F14: DVI Inspecao Visual Digital Local (persistência local em JSON)
- F21: Autoeletrica Roteiros Legados D07-D17 (base de consulta mantida)
- F35: Agendamentos Notificacao Externa (alertas em tela funcionais; gateway SMS ausente)
- F44: Relatorios Customizaveis / BI (exportação CSV operacional; gerador dinâmico ausente)
- F45: Fiscal Leitura e Conferencia de XML (leitura/validação funcional; emissão própria bloqueada)
- F51: Comunicacao WhatsApp Link wa.me (link rápido manual funcional; sem API oficial direta)

#### 4. Quantas são UI_ONLY?
**2 funcionalidades** são UI_ONLY:
- F13: Botões avulsos 'E-mail' e 'Venda' em Orçamentos (desabilitados intencionalmente com tooltips)
- F52: Tela de Chat de Suporte Técnico (interface pronta sem backend de chat conectado)

#### 5. Quantas são MOCK?
**1 funcionalidade** é MOCK:
- F48: Licenciamento Scaffold Local (`LicenseService` operando sob flag `IsCommercialScaffoldOnly = true` fail-closed)

#### 6. Quantas são EXTERNAL?
**1 funcionalidade** é EXTERNAL_DEPENDENCY:
- F46: Emissão Fiscal NF-e/NFC-e SEFAZ (pipeline estruturado, bloqueado por segurança via `ProductionEmissionAllowed = false`)

#### 7. Quantas são NOT_IMPLEMENTED?
**7 funcionalidades** são NOT_IMPLEMENTED (identificadas honestamente e planejadas para a Trilha B / Nuvem):
- F15: DVI Portal Remoto / Nuvem
- F47: Emissão de NFS-e Municipal
- F53: TEF / Maquininha Integrada
- F54: Conciliação Bancária OFX / CNAB
- F55: Consulta Online a Catálogos de Distribuidores via API
- F56: Sincronização Multi-Filial / Corporativo em Nuvem
- F57: Aplicativo Mobile do Proprietário do Veículo

#### 8. Quais são obrigatórias para a primeira versão (v1.0)?
São obrigatórias **33 funcionalidades (Classificação RELEASE_REQUIRED)** que constituem o fluxo operacional vital de uma oficina: cadastro completo de clientes com validação de CPF/CNPJ e LGPD, veículos com prontuário elétrico 12V/24V, Client360, Vehicle360, Orçamentos com margens e tokens de aprovação, conversão 1:1 para OS, execução de OS, roteiros elétricos estruturados D01-D06 com medições e teste pós-reparo (delta calculado), baixa atômica de estoque, catálogo técnico de peças com fotos/PDFs, contas a pagar, contas a receber, caixa diário operacional, backup local com integridade e segurança RBAC com 10 perfis fail-closed.

#### 9. Quais podem ficar para versões posteriores (Pós-Release)?
Podem ficar para versões posteriores (v1.1 ou v2.0): portal do cliente web, aplicativo mobile para mecânicos, sincronização multi-lojas em nuvem, integração TEF para cartões, conciliação automática de extrato bancário OFX e disparo automatizado de WhatsApp via API corporativa.

#### 10. Quais dependem de terceiros?
- Emissão fiscal (Webservices estaduais da SEFAZ e Autoridades Certificadoras ICP-Brasil).
- Mensageria automática oficial de WhatsApp (Meta Cloud API e parceiros BSP).
- Leitura de hardware OBD2 / Scanners (Fabricantes de hardware J2534 / PassThru).

#### 11. Quais exigem servidor?
Exigem infraestrutura de servidor:
- Servidor central de ativação de licenças e revogação remota.
- Servidor web e storage S3/Blob para o Portal do Cliente e fotos de DVI em nuvem.
- Servidor de replicação de dados para redes multi-loja.
*Nota: A versão 1.0 NÃO exige nenhum servidor externo para a operação diária da oficina.*

#### 12. Quais exigem certificado digital?
- **Certificado Digital de Empresa (e-CNPJ A1 ou A3):** Obrigatório para assinatura de XMLs e emissão de notas fiscais junto à SEFAZ.
- **Certificado Digital de Assinatura de Código (Authenticode):** Recomendado para assinar digitalmente o instalador Inno Setup e binários `.exe`, eliminando avisos do Windows SmartScreen.

#### 13. Quais exigem migração de banco?
Exige migração a transição definitiva dos tipos monetários de ponto flutuante (`REAL`) para centavos inteiros (`INTEGER CentsV1`) nas tabelas financeiras do SQLite.

#### 14. Qual é o risco da migração Money?
O risco reside na possibilidade de distorção de centavos históricos em registros antigos ou lock prolongado do arquivo de banco durante a conversão. 
**Mitigação:** O risco foi neutralizado pelo desenho do protocolo de 6 estágios em [B5_0_MONEY_MIGRATION_RELEASE_PLAN.md](file:///c:/Projetos/PrimoAutoEletrica/Docs/audit/2026-09-20/B5_0_MONEY_MIGRATION_RELEASE_PLAN.md), que exige rehearsal prévio, comparação 100% linha a linha e rollback atômico automático se houver diferença de 1 centavo.

#### 15. Qual é o caminho para Fiscal?
O caminho seguro consiste em: (1) manter `ProductionEmissionAllowed = false` na v1.0 local; (2) implementar a camada de abstração de certificado A1 com DPAPI na Fase B7; (3) executar testes ponta a ponta contra o ambiente oficial de Homologação da SEFAZ; (4) somente após aprovação dos retornos fiscais, habilitar a emissão de produção sob licença comercial.

#### 16. Qual é o caminho para Licenciamento?
O caminho para a v1.0 é a **Licença Offline Criptografada (RSA-4096)** via arquivo assinado `primox.lic`, vinculado ao hash da placa-mãe e CPU da máquina mestre da oficina, com tolerância de relógio e grace period de 15 dias, evitando criar um servidor SaaS fictício nesta fase.

#### 17. Qual é o caminho para o Instalador?
Adotar o **Inno Setup** com script `primox_setup.iss`, configurado para: instalar em `%LOCALAPPDATA%\\PrimoAutoEletrica\\App`, verificar o pré-requisito do .NET 10 Desktop Runtime, criar atalho na Área de Trabalho e **proteger com regra de ouro a preservação de todos os arquivos de banco de dados e backups locais durante atualizações ou desinstalações**.

#### 18. Qual é o caminho para Atualização (Update)?
Utilizar versionamento do schema do SQLite via `PRAGMA user_version`. Toda atualização é precedida de backup automático timestamped. Em caso de falha na migração do schema, o sistema desfaz as alterações e restaura a cópia de segurança imediatamente.

#### 19. Qual é o caminho para o Piloto Comercial?
Selecionar 1 oficina parceira com volume de 5 a 20 veículos/dia, implantar o PRIMOX em base operacional isolada (`primoauto_operacional.db`), ministrar treinamento de 4 horas para a equipe (Recepção, Eletricistas, Caixa) e acompanhar a operação durante 30 dias corridos sob SLA de suporte prioritário.

#### 20. O que NÃO deve ser prometido ao cliente?
1. Não prometer emissão de nota fiscal em produção enquanto o módulo estiver bloqueado por segurança.
2. Não prometer sincronização em nuvem ou portal web enquanto a infraestrutura cloud não estiver construída.
3. Não prometer aplicativo para celular antes de seu desenvolvimento formal.
4. Não prometer leitura automática de scanners OBD2 sem os drivers de hardware correspondentes.
5. Não prometer conciliação automática de extratos bancários antes da implementação do parser OFX/CNAB.
6. Não prometer multi-filiais em nuvem para a versão 1.0.
7. Não prometer TEF integrado sem homologação formal de adquirente.
8. Não prometer envio automático de WhatsApp sem contratação de conta oficial Meta API.
9. Não prometer emissão de nota fiscal municipal (NFS-e) na v1.0.
10. Não prometer licenciamento online sem a publicação prévia do servidor de licenças.

#### 21. Qual é o Backlog da Fase B5?
O backlog executivo consolida 25 tarefas prioritárias detalhadas em [B5_0_EXECUTIVE_BACKLOG.csv](file:///c:/Projetos/PrimoAutoEletrica/Docs/audit/2026-09-20/B5_0_EXECUTIVE_BACKLOG.csv), encabeçado pela criação das interfaces visuais de Pós-venda (B5-001) e Checklist Técnico (B5-002), script de empacotamento Inno Setup (B5-006) e utilitário de ensaio de migração de banco (B5-005).

#### 22. Qual é a ordem correta de execução das tarefas?
1. **B5.1:** Construção das interfaces gráficas dedicadas de Checklist Multiponto e Pós-Venda.
2. **B5.2:** Criação e teste do instalador oficial Inno Setup em ambiente de máquina limpa.
3. **B5.3:** Execução do rehearsal do utilitário de migração de dados em banco sintético isolado.
4. **B6:** Implantação e execução do Piloto Comercial Controlado (30 dias).
5. **B7:** Homologação fiscal SEFAZ com certificado A1 e migração de produção sob gate.

#### 23. Quais são os gates obrigatórios?
- **Gate B5 (Release Candidate):** Build Release 0 erros, 417+ testes automatizados, UI Smoke APROVADO, instalador funcional.
- **Gate B6 (Piloto Aprovado):** 30 dias sem falhas críticas, zero perda de dados, laudo formal da oficina piloto.
- **Gate B7 (Fiscal & Money Migration):** Emissão validada no ambiente de homologação SEFAZ e migração física com data proof 100% idêntico.

#### 24. O que permanece estritamente bloqueado nesta fase?
- `ProductionEmissionAllowed = false` (bloqueio incondicional de emissão fiscal real).
- O banco de produção `primoauto.db` permanece com `IsReadOnly = True` e SHA-256 `C7420D18...CE0B` intocado.
- A branch `main` permanece 100% intocada.
- Licenciamento online remoto sem servidor dedicado.

---

### Declaração de Conclusão do Gate B5.0

```
======================================================================
               PRIMOX WORKSHOP — GATE FINAL FASE B5.0
======================================================================
 [X] Baseline B4 reproduzida (417 testes PASS, 196 UI Smoke PASS)
 [X] 57 funcionalidades reavaliadas com honestidade técnica total
 [X] Matriz da verdade comercial gerada (B5_0_FEATURE_TRUTH_MATRIX.csv)
 [X] Escopo real do MVP e versão 1.0 definidos
 [X] Especificação técnica de Autoelétrica Tech/Heavy 2.0 concluída
 [X] Especificação visual do Checklist Multiponto concluída
 [X] Plano de release da migração de Money estruturado em 6 estágios
 [X] Arquitetura de certificados A1/A3 e segurança DPAPI definida
 [X] Plano de licenciamento offline assimétrico RSA-4096 definido
 [X] Roadmap de longo prazo de serviços em nuvem (Trilha B) definido
 [X] Decisão técnica do instalador oficial aprovada (Inno Setup)
 [X] Estratégia de atualização e versionamento SQLite formalizada
 [X] Checklist de segurança e conformidade técnica LGPD aprovado
 [X] Matriz operacional dos 10 perfis comerciais concluída
 [X] Simulação temporal de dia de oficina real documentada
 [X] Requisitos e critérios da oficina piloto (Fase B6) definidos
 [X] Roadmap estratégico de release (B5, B6, B7, B8) estruturado
 [X] Backlog executivo formalizado com 25 tarefas priorizadas
 [X] Relatório mestre respondendo às 24 perguntas concluído
 [X] Branch 'main' intacta
 [X] Banco de produção primoauto.db intacto (SHA-256 preservado)
======================================================================
 B5.0 STATUS: PASS
======================================================================
```
"""

with open(os.path.join(DOC_DIR, "B5_0_FINAL.md"), "w", encoding="utf-8") as f:
    f.write(final_md)

print("19. B5_0_FINAL.md OK")
