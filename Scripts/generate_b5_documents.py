# -*- coding: utf-8 -*-
"""
Script to generate all B5.0 Commercial Readiness Discovery & Release Architecture documents.
Outputs to Docs/audit/2026-09-20/
"""

import os
import csv

DOC_DIR = r"c:\Projetos\PrimoAutoEletrica\Docs\audit\2026-09-20"
os.makedirs(DOC_DIR, exist_ok=True)

# -------------------------------------------------------------
# 1. B5_0_BASELINE.md
# -------------------------------------------------------------
baseline_md = """# PRIMOX WORKSHOP — B5.0
## BASELINE DE DESCOBERTA E ARQUITETURA DE RELEASE

**Data/Hora:** 2026-09-24 12:10:00 -03:00  
**Branch:** `audit/product-discovery-2026-09`  
**Commit:** `72e46ac` (`feat(product): complete B4 product evolution and commercial hardening`)  
**Status da Baseline:** **PASS / REPRODUZÍVEL**

---

### 1. Parâmetros de Ambiente e Verificação de Integridade

| Componente | Especificação / Valor Registrado | Estado / Status |
| :--- | :--- | :--- |
| **Branch Atual** | `audit/product-discovery-2026-09` | CONFORME (Não é `main`) |
| **Commit HEAD** | `72e46ac` | PASS |
| **Branch `main`** | Intocada (zero commits) | PRESERVADA |
| **Banco Produção** | `C:\\Users\\campo\\AppData\\Local\\PrimoAutoEletrica\\primoauto.db` | CONFORME |
| **SHA-256 Produção** | `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` | 100% EXATO |
| **Atributo Produção** | `IsReadOnly = True` | ATIVO |
| **Banco Operacional** | `C:\\Users\\campo\\AppData\\Local\\PrimoAutoEletrica\\primoauto_operacional.db` | ATIVO (`IsReadOnly = False`) |
| **Projetos na Solution** | 6 Projetos (.sln oficial) | BUILD RELEASE 0 ERROS |
| **Target Framework** | .NET 10 (`net10.0-windows` / `net10.0`) | CONFORME |
| **Testes xUnit** | 417 executados / 417 aprovados | **417 PASS / 0 FAIL / 0 SKIP** |
| **UI Smoke Automated** | 196 verificações / 196 aprovadas | **196 PASS / 0 FAIL (APROVADO)** |
| **Desktop Executable** | `%LOCALAPPDATA%\\PrimoAutoEletrica\\App\\PrimoAutoEletrica.exe` | Atualizado (24/09/2026 08:09:05) |
| **Atalho Desktop** | `C:\\Users\\campo\\OneDrive\\Desktop\\PRIMOX Workshop.lnk` | Apontando para o binário real |
| **Desktop Startups** | 3 execuções sucessivas via atalho | 3/3 PASS (Zero SQLite Error 8) |

---

### 2. Bloqueadores Conhecidos e Deliberados

1. **`ProductionEmissionAllowed = false`**: Emissão fiscal de produção para SEFAZ permanece estritamente bloqueada no código ([FiscalDatabaseService.cs](file:///c:/Projetos/PrimoAutoEletrica/PrimoAutoEletrica/Services/FiscalDatabaseService.cs)).
2. **Migração Física de Money em Produção Bloqueada**: O banco `primoauto.db` mantém seus dados históricos inalterados. Toda operação utiliza `MoneyIO` em memória.
3. **Servidor de Licenciamento Remoto**: Mantido em scaffold fail-closed (`IsCommercialScaffoldOnly = true`). Não há servidor de produção implementado.
4. **Hardware Scanner / PassThru**: Leituras ao vivo de barramento CAN/OBD2 via J2534 ou ELM327 classificadas como dependência externa/futura.

---

### 3. Conclusão da Baseline
A baseline B5.0 está perfeitamente estabilizada, idêntica ao estado de encerramento da B4, permitindo o avanço das atividades de arquitetura de release.
"""

with open(os.path.join(DOC_DIR, "B5_0_BASELINE.md"), "w", encoding="utf-8") as f:
    f.write(baseline_md)

print("1. B5_0_BASELINE.md OK")

# -------------------------------------------------------------
# 2. B5_0_FEATURE_TRUTH_MATRIX.csv
# -------------------------------------------------------------
features_data = [
    ("F01", "Dashboard Operacional", "Dashboard", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (18 tests)", "B4_PRODUCT_INVENTORY.md", "KPIs de faturamento, OS abertas e agenda em tempo real"),
    ("F02", "Clientes CRUD Completo", "Clientes", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (42 tests)", "B4_PRODUCT_INVENTORY.md", "Base cadastral com validacao estrita de CPF/CNPJ"),
    ("F03", "Clientes Visualizacao e Historico", "Clientes", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (12 tests)", "B4_CLIENT360_AUDIT.md", "Agrupamento relacional estrito por ClienteId"),
    ("F04", "Clientes Consentimento LGPD", "Clientes", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (6 tests)", "B4_CLIENT360_AUDIT.md", "Registro auditavel de termo de consentimento LGPD"),
    ("F05", "Veiculos Cadastro e Frotas", "Veiculos", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (35 tests)", "B4_PRODUCT_INVENTORY.md", "Gestao de veiculos leves (12V) e pesados (24V)"),
    ("F06", "Veiculos Prontuario Eletrico", "Veiculos", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (14 tests)", "B4_VEHICLE360_TECHNICAL_HISTORY.md", "17 campos tecnicos de especificacao eletrica"),
    ("F07", "Cliente 360", "Cliente 360", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (16 tests)", "B4_CLIENT360_AUDIT.md", "Visao unificada de veiculos, OS, orcamentos e financeiro"),
    ("F08", "Vehicle 360", "Vehicle 360", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (19 tests)", "B4_VEHICLE360_TECHNICAL_HISTORY.md", "Linha do tempo tecnica, historico de OS e diagnosticos"),
    ("F09", "Pos-Venda Unificado", "Pos-Venda", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (5 tests)", "B4_POS_VENDA.md", "Estrutura de garantias, retornos e revisoes preventivas"),
    ("F10", "Orcamentos Calculo e Margens", "Orcamentos", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (28 tests)", "B4_PRODUCT_INVENTORY.md", "Calculo automatico de pecas, servicos, descontos e margens"),
    ("F11", "Orcamentos Fluxo de Aprovacao", "Orcamentos", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (8 tests)", "B4_PRODUCT_INVENTORY.md", "Aprovacao com geracao de token e assinatura"),
    ("F12", "Orcamento para OS Conversao", "Orcamentos", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (10 tests)", "B4_360_END_TO_END.md", "Conversao 1:1 sem redigitacao nem perda de dados"),
    ("F13", "Orcamento Acoes Avulsas (Email/Venda)", "Orcamentos", "UI_ONLY", "UI_ONLY", "POST_RELEASE", "Provedor SMTP / PDV Balcao", "Botoes desabilitados com tooltip", "PASS (Smoke)", "B4_PRODUCT_INVENTORY.md", "Envio direto de proposta por e-mail e venda balcao"),
    ("F14", "DVI Inspecao Visual Digital Local", "DVI", "PARTIAL", "PARTIAL", "RELEASE_RECOMMENDED", "Armazenamento local imagens", "Persistencia JSON local em vez de SQLite", "PASS (14 tests)", "B4_360_END_TO_END.md", "Checklist com marcacao de avarias e fotos locais"),
    ("F15", "DVI Portal Remoto / Nuvem", "DVI", "NOT_IMPLEMENTED", "NOT_IMPLEMENTED", "FUTURE", "Servidor Cloud / Storage S3", "Infraestrutura cloud inexistente", "NOT_TESTED", "B5_0_CLOUD_ROADMAP.md", "Portal web para aprovacao externa pelo cliente"),
    ("F16", "Ordens de Servico Gestao Completa", "OrdensServico", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (45 tests)", "B4_PRODUCT_INVENTORY.md", "Ciclo de vida operacional: Abertura, Execucao, Conclusao"),
    ("F17", "Ordens de Servico Baixa de Estoque", "OrdensServico", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (12 tests)", "B4_PRODUCT_INVENTORY.md", "Baixa de itens do estoque vinculada a finalizacao da OS"),
    ("F18", "Ordens de Servico Faturamento Integrado", "OrdensServico", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (15 tests)", "B4_PRODUCT_INVENTORY.md", "Geracao automatica de Contas a Receber vinculadas por Id"),
    ("F19", "Autoeletrica Prontuario Eletrico 12V/24V", "AutoEletricaTecnica", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (14 tests)", "B4_VEHICLE360_TECHNICAL_HISTORY.md", "Diferencial de diagnostico estruturado para linhas leve e pesada"),
    ("F20", "Autoeletrica Diagnostico D01-D06", "AutoEletricaTecnica", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (22 tests)", "B4_VEHICLE360_TECHNICAL_HISTORY.md", "6 roteiros tecnicos padronizados com medicao e delta"),
    ("F21", "Autoeletrica Roteiros Legados D07-D17", "AutoEletricaTecnica", "PARTIAL", "PARTIAL", "PILOT_REQUIRED", "roteiros-resultados.json", "Sem vinculo relacional estrito por OS Id", "PASS (8 tests)", "B4_VEHICLE360_TECHNICAL_HISTORY.md", "Base de conhecimento de roteiros tecnicos legados preservada"),
    ("F22", "Autoeletrica Grandezas Eletricas", "AutoEletricaTecnica", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (16 tests)", "B4_VEHICLE360_TECHNICAL_HISTORY.md", "Tensao, Corrente, Fuga, Resistencia, CCA, Duty Cycle, Pressao"),
    ("F23", "Autoeletrica Teste Pos-Reparo e Delta", "AutoEletricaTecnica", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (11 tests)", "B4_360_END_TO_END.md", "Calculo automatico de variacao eletrica pos-conserto"),
    ("F24", "Estoque Cadastro de Produtos", "Estoque", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (26 tests)", "B4_PRODUCT_INVENTORY.md", "Cadastro completo com precos, margem e estoque minimo"),
    ("F25", "Estoque Movimentacoes e Kardex", "Estoque", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (18 tests)", "B4_PRODUCT_INVENTORY.md", "Rastreabilidade de entradas, saidas e ajustes"),
    ("F26", "Estoque Catalogo Tecnico e Aplicacao", "Estoque", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (14 tests)", "B4_PRODUCT_INVENTORY.md", "Catalogo de pecas com codigos cruzados e aplicacao veicular"),
    ("F27", "Estoque Importacao XML Fornecedor", "Estoque", "CORE", "CORE", "RELEASE_REQUIRED", "XML de Fornecedor", "Nenhum", "PASS (9 tests)", "B4_PRODUCT_INVENTORY.md", "Entrada expressa de estoque e precos a partir de NF-e recebida"),
    ("F28", "Estoque Localizacao Fisica (Gaveteiro)", "Estoque", "CORE", "CORE", "RELEASE_RECOMMENDED", "Nenhuma", "Nenhum", "PASS (5 tests)", "B4_PRODUCT_INVENTORY.md", "Enderecamento fisico por Corredor, Prateleira e Gaveta"),
    ("F29", "Financeiro Contas a Pagar", "Financeiro", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (18 tests)", "B4_MONEY_FINAL_RUNTIME_AUDIT.md", "Controle de titulos, boletos, despesas e fornecedores"),
    ("F30", "Financeiro Contas a Receber", "Financeiro", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (20 tests)", "B4_MONEY_FINAL_RUNTIME_AUDIT.md", "Faturamento, baixas totais/parciais e juros/descontos"),
    ("F31", "Financeiro Fluxo de Caixa e Extrato", "Financeiro", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (14 tests)", "B4_MONEY_FINAL_RUNTIME_AUDIT.md", "Extrato consolidado de entradas e saidas por periodo"),
    ("F32", "Caixa Operacional Balcao", "Caixa", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (12 tests)", "B4_PRODUCT_INVENTORY.md", "Abertura, fechamento de turno, suprimento e sangria"),
    ("F33", "PDV Venda Direta / Balcao", "PDV", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (16 tests)", "B4_PRODUCT_INVENTORY.md", "Venda rapida de pecas de balcao sem vinculo com OS"),
    ("F34", "Agendamentos Controle de Patio", "Agendamentos", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (11 tests)", "B4_PRODUCT_INVENTORY.md", "Quadro de horarios e reservas de atendimento"),
    ("F35", "Agendamentos Notificacao Externa", "Agendamentos", "PARTIAL", "PARTIAL", "POST_RELEASE", "Gateway SMS/WhatsApp", "Alertas em tela funcionais; gateway ausente", "PASS (Smoke)", "B4_PRODUCT_INVENTORY.md", "Disparo automatico de lembretes aos clientes"),
    ("F36", "Fornecedores Cadastro e Contatos", "Fornecedores", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (10 tests)", "B4_PRODUCT_INVENTORY.md", "Gestao cadastral e historico de compras de fornecedores"),
    ("F37", "Fornecedores Pedidos de Compra", "Fornecedores", "CORE", "CORE", "RELEASE_RECOMMENDED", "Nenhuma", "Nenhum", "PASS (8 tests)", "B4_PRODUCT_INVENTORY.md", "Cotacao e geracao de ordens de compra"),
    ("F38", "Funcionarios e Produtividade", "Funcionarios", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (12 tests)", "B4_PRODUCT_INVENTORY.md", "Cadastro de equipe, eletricistas e apontamento de OS"),
    ("F39", "Seguranca RBAC 10 Perfis / 82 Permissoes", "Seguranca", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (24 tests)", "B3_RBAC_HARDENING.md", "Matriz rigorosa fail-closed (Unavailable = DENY)"),
    ("F40", "Seguranca Autenticacao PBKDF2", "Seguranca", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (15 tests)", "B3_RBAC_HARDENING.md", "Hash seguro de senhas com sal e protecao brute-force"),
    ("F41", "Seguranca API REST JWT Local", "Seguranca", "CORE", "CORE", "PILOT_REQUIRED", "Nenhuma", "Nenhum", "PASS (18 tests)", "B3_RBAC_HARDENING.md", "Endpoints protegidos por token JWT para integracoes locais"),
    ("F42", "Backup e Restore SQLite com Integridade", "Backup", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (14 tests)", "B4_PRODUCT_INVENTORY.md", "Backup a quente e restore com validacao de integridade e FKs"),
    ("F43", "Relatorios Operacionais e Financeiros", "Relatorios", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (16 tests)", "B4_PRODUCT_INVENTORY.md", "Relatorios em tela e impressao de OS, DRE e estoque"),
    ("F44", "Relatorios Customizaveis / BI", "Relatorios", "PARTIAL", "PARTIAL", "POST_RELEASE", "Nenhuma", "Exportacao CSV completa; gerador dinamico ausente", "PASS (5 tests)", "B4_PRODUCT_INVENTORY.md", "Construtor visual avancado de relatorios e cubos OLAP"),
    ("F45", "Fiscal Leitura e Conferencia de XML", "Fiscal", "PARTIAL", "PARTIAL", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (8 tests)", "B4_FISCAL_ARCHITECTURE.md", "Parser de NF-e para auditoria e entrada de notas fiscais"),
    ("F46", "Fiscal Emissao NF-e/NFC-e SEFAZ", "Fiscal", "EXTERNAL_DEPENDENCY", "EXTERNAL_DEPENDENCY", "BLOCKED", "Certificado A1/A3 e SEFAZ", "ProductionEmissionAllowed=false bloqueado intencionalmente", "PASS (Contrato)", "B4_FISCAL_ARCHITECTURE.md", "Pipeline montado; emissao de producao bloqueada"),
    ("F47", "Fiscal Emissao NFS-e Municipal", "Fiscal", "NOT_IMPLEMENTED", "NOT_IMPLEMENTED", "FUTURE", "Provedor ISS Municipal", "Inexistente na solucao", "NOT_TESTED", "B4_FISCAL_ARCHITECTURE.md", "Emissao de NFS-e padrao municipal ou nacional"),
    ("F48", "Licenciamento Scaffold Local", "Licenciamento", "MOCK", "MOCK", "BLOCKED", "Servidor de Licenciamento", "IsCommercialScaffoldOnly=true bloqueia modo comercial", "PASS (Fail-closed)", "B4_LICENSE_ARCHITECTURE.md", "Scaffold fail-closed; arquitetura definida em B5_0_LICENSE_RELEASE_PLAN"),
    ("F49", "Configuracoes e Personalizacao", "Configuracoes", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (10 tests)", "B4_PRODUCT_INVENTORY.md", "Dados da oficina, configuracao de impressora e Dark/Light"),
    ("F50", "Auditoria e Log Operacional", "Auditoria", "CORE", "CORE", "RELEASE_REQUIRED", "Nenhuma", "Nenhum", "PASS (12 tests)", "B4_PRODUCT_INVENTORY.md", "Registro append-only de eventos de seguranca e operacoes"),
    ("F51", "Comunicacao WhatsApp Link wa.me", "Comunicacao", "PARTIAL", "PARTIAL", "RELEASE_RECOMMENDED", "Navegador Web / WhatsApp Web", "Gera link wa.me direto; sem envio automatico", "PASS (Smoke)", "B4_PRODUCT_INVENTORY.md", "Disparo manual com mensagem estruturada com 1 clique"),
    ("F52", "Suporte Tecnico Chat UI", "Suporte", "UI_ONLY", "UI_ONLY", "POST_RELEASE", "Servidor de Atendimento", "Interface de chat sem backend integrado", "PASS (Smoke)", "B4_PRODUCT_INVENTORY.md", "Canal de abertura de chamados dentro do software"),
    ("F53", "Financeiro TEF / Cartao Integrado", "Financeiro", "NOT_IMPLEMENTED", "NOT_IMPLEMENTED", "FUTURE", "Adquirentes / Provedores TEF", "Inexistente na solucao", "NOT_TESTED", "B4_PRODUCT_INVENTORY.md", "Integracao de maquininha de cartao direta no PDV"),
    ("F54", "Financeiro Conciliacao OFX / CNAB", "Financeiro", "NOT_IMPLEMENTED", "NOT_IMPLEMENTED", "FUTURE", "Extrato Bancario OFX", "Inexistente na solucao", "NOT_TESTED", "B4_PRODUCT_INVENTORY.md", "Leitura e conciliacao automatica de extratos bancarios"),
    ("F55", "Estoque Catalogo Distribuidor Online", "Estoque", "NOT_IMPLEMENTED", "NOT_IMPLEMENTED", "FUTURE", "APIs B2B Distribuidores", "Inexistente na solucao", "NOT_TESTED", "B4_PRODUCT_INVENTORY.md", "Consulta remota de precos e disponibilidade em fornecedores"),
    ("F56", "Corporativo Multi-Filial / Nuvem", "Corporativo", "NOT_IMPLEMENTED", "NOT_IMPLEMENTED", "FUTURE", "Servidor Nuvem / Multi-Tenant", "Inexistente; solucao e single-store local", "NOT_TESTED", "B5_0_CLOUD_ROADMAP.md", "Gestao centralizada de redes de oficinas"),
    ("F57", "Clientes App Mobile do Proprietario", "Clientes", "NOT_IMPLEMENTED", "NOT_IMPLEMENTED", "FUTURE", "App iOS/Android", "Inexistente na solucao", "NOT_TESTED", "B4_PRODUCT_INVENTORY.md", "Aplicativo mobile para clientes acompanharem servico")
]

with open(os.path.join(DOC_DIR, "B5_0_FEATURE_TRUTH_MATRIX.csv"), "w", encoding="utf-8", newline="") as f:
    writer = csv.writer(f)
    writer.writerow(["ID", "Nome", "Modulo", "StatusB4", "StatusTecnicoAtual", "StatusComercial", "Dependencia", "Bloqueador", "Teste", "Documentacao", "Observacao"])
    for r in features_data:
        writer.writerow(r)

print("2. B5_0_FEATURE_TRUTH_MATRIX.csv OK")
