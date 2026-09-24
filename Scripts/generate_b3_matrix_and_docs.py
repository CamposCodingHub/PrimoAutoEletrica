import os
import csv

def generate_b3_matrix():
    os.makedirs("Docs/audit/2026-09-20", exist_ok=True)
    
    # ID,Modulo,Tela,Service,Repository,Persistencia,API,Status,Mock,ExternalDependency,RBAC,Testes,DesktopValidado,Problema,CorrecaoNecessaria,Prioridade
    rows = [
        # Dashboard
        ["F01", "Dashboard", "DashboardControl.xaml", "DatabaseService", "N/A", "SQLite (Orcamentos, OrdensServico, Movimentacoes)", "CORE", "CORE", "NAO", "NAO", "SIM (DASHBOARD_VER)", "PASS", "SIM", "Nenhum", "Manter indicadores", "P4"],
        
        # Clientes
        ["F02", "Clientes", "ClientesControl.xaml, NovoClienteWindow.xaml, EditarClienteWindow.xaml", "CustomerService", "ClienteRepository", "SQLite (Clientes)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (CLIENTES_VER/CRIAR/EDITAR/EXCLUIR)", "PASS", "SIM", "Nenhum", "Manter operacao", "P4"],
        ["F03", "Clientes", "VisualizarClienteWindow.xaml, HistoricoClienteWindow.xaml", "Primox360Service", "ClienteRepository", "SQLite (Clientes, Veiculos)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (CLIENTES_VER)", "PASS", "SIM", "Nenhum", "Manter agregacao por ID", "P4"],
        ["F04", "Clientes", "VisualizarClienteWindow.xaml", "ClienteRepository", "ClienteRepository", "SQLite (Clientes.ConsentimentoLGPD)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (CLIENTES_EDITAR)", "PASS", "SIM", "Nenhum", "Manter validacao LGPD", "P4"],
        
        # Veículos
        ["F05", "Veiculos", "VeiculosControl.xaml, NovoVeiculoWindow.xaml", "VeiculoProfileService", "ClienteRepository", "SQLite (Veiculos)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (VEICULOS_VER/CRIAR/EDITAR/EXCLUIR)", "PASS", "SIM", "Nenhum", "Manter operacao", "P4"],
        ["F06", "Veiculos", "VisualizarVeiculoWindow.xaml", "AutoEletricaTecnicaService", "ClienteRepository", "SQLite (Veiculos.Bateria, Alternador, etc.)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (VEICULOS_VER)", "PASS", "SIM", "Nenhum", "Manter 17 campos de telemetria", "P4"],
        
        # 360
        ["F07", "Cliente 360", "HistoricoClienteWindow.xaml", "Primox360Service", "ClienteRepository, OrdemServicoRepository", "SQLite (Clientes, Veiculos, OS, Orcamentos, ContasReceber)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (CLIENTES_VER)", "PASS", "SIM", "Nenhum", "Agregacao estrita por ID", "P4"],
        ["F08", "Vehicle 360", "VisualizarVeiculoWindow.xaml", "Primox360Service", "ClienteRepository, OrdemServicoRepository", "SQLite (Veiculos, OS, Orcamentos) + JSON Diagnosticos", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (VEICULOS_VER)", "PASS", "SIM", "Nenhum", "Linha do tempo tecnica e OS unificada", "P4"],
        
        # Orçamentos
        ["F09", "Orcamentos", "OrcamentosControl.xaml, NovoOrcamentoWindow.xaml", "OrcamentoDatabaseService", "N/A", "SQLite (Orcamentos, OrcamentoItens)", "CORE", "CORE", "NAO", "NAO", "SIM (ORCAMENTOS_VER/CRIAR/EDITAR/EXCLUIR)", "PASS", "SIM", "Nenhum", "Manter motor de orcamentos", "P4"],
        ["F10", "Orcamentos", "OrcamentosControl.xaml", "OrcamentoAprovacaoService", "N/A", "SQLite (Orcamentos)", "CORE", "CORE", "NAO", "NAO", "SIM (ORCAMENTOS_EDITAR)", "PASS", "SIM", "Nenhum", "Manter tokens e aprovacao", "P4"],
        ["F11", "Orcamentos", "OrcamentosControl.xaml", "OrcamentosViewModel", "OrdemServicoRepository", "SQLite (Orcamentos, OrdensServico)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (ORCAMENTOS_CONVERTER_OS)", "PASS", "SIM", "Nenhum", "Conversao 1:1 sem redigitacao", "P4"],
        ["F12", "Orcamentos", "OrcamentosView.xaml", "N/A", "N/A", "Nenhum", "NOT_IMPLEMENTED", "UI_ONLY", "NAO", "NAO", "NAO", "NOT_TESTED", "NAO", "Botao Enviar Email sem handler", "Conectar SMTP ou desabilitar", "P3"],
        ["F13", "Orcamentos", "OrcamentosView.xaml", "N/A", "N/A", "Nenhum", "NOT_IMPLEMENTED", "UI_ONLY", "NAO", "NAO", "NAO", "NOT_TESTED", "NAO", "Botao Converter em Venda Balcao sem handler", "Conectar ao PDV ou desabilitar", "P3"],

        # DVI
        ["F14", "DVI", "DviOrcamentoWindow.xaml, OrdemServicoWindow.xaml", "DviChecklistService", "N/A", "JSON Files (AppData/Dvi)", "NOT_IMPLEMENTED", "PARTIAL", "NAO", "NAO", "SIM (ORDENS_SERVICO_EDITAR)", "PASS", "SIM", "Persistencia em arquivos JSON locais", "Manter JSON ou planejar tabela SQLite", "P3"],
        ["F15", "DVI", "N/A", "N/A", "N/A", "Nenhum", "NOT_IMPLEMENTED", "NOT_IMPLEMENTED", "NAO", "SIM", "NAO", "NOT_TESTED", "NAO", "Portal do cliente e aprovacao remota cloud inexistente", "Planejar arquitetura Cloud na Trilha B", "P2"],

        # Ordens de Serviço
        ["F16", "OrdensServico", "OrdensServicoControl.xaml, OrdemServicoWindow.xaml", "DatabaseService", "OrdemServicoRepository", "SQLite (OrdensServico, OrdemServicoItens, Eventos)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (ORDENS_SERVICO_VER/CRIAR/EDITAR/EXCLUIR)", "PASS", "SIM", "Nenhum", "Manter operacao", "P4"],
        ["F17", "OrdensServico", "OrdemServicoWindow.xaml", "DatabaseService", "OrdemServicoRepository", "SQLite (Produtos, MovimentacoesEstoque)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (ORDENS_SERVICO_EDITAR)", "PASS", "SIM", "Nenhum", "Baixa automatica ao finalizar OS", "P4"],
        ["F18", "OrdensServico", "OrdensServicoControl.xaml", "FinanceiroDatabaseService", "OrdemServicoRepository", "SQLite (ContasReceber, MovimentacoesFinanceiras)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (FINANCEIRO_RECEBER)", "PASS", "SIM", "Nenhum", "Geracao de receita vinculada por Id", "P4"],

        # Autoelétrica Técnica
        ["F19", "AutoEletricaTecnica", "AutoEletricaTecnicaControl.xaml, VisualizarVeiculoWindow.xaml", "AutoEletricaTecnicaService", "ClienteRepository", "SQLite (Veiculos - 17 campos eletricos)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (VEICULOS_VER)", "PASS", "SIM", "Nenhum", "Prontuario eletrico estruturado", "P4"],
        ["F20", "AutoEletricaTecnica", "AutoEletricaTecnicaControl.xaml", "DiagnosticoTecnicoService", "N/A", "JSON Files (AppData/AutoEletrica/diagnosticos/{Guid}.json)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (VEICULOS_VER)", "PASS", "SIM", "Nenhum", "Roteiros D01-D06 estruturados com vinculo VeiculoId e OrdemServicoId", "P4"],
        ["F21", "AutoEletricaTecnica", "AutoEletricaTecnicaControl.xaml", "AutoEletricaRoteiroPersistService", "N/A", "JSON File (roteiros-resultados.json legado)", "NOT_IMPLEMENTED", "PARTIAL", "NAO", "NAO", "SIM (VEICULOS_VER)", "PASS", "SIM", "Base legada D07-D17 sem vinculo OS/Veiculo", "Preservar integridade do historico legado", "P3"],
        ["F22", "AutoEletricaTecnica", "AutoEletricaTecnicaControl.xaml", "SintomaCausaHistoricoService", "OrdemServicoRepository", "SQLite (OrdensServico, OrdemServicoItens)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (VEICULOS_VER)", "PASS", "SIM", "Nenhum", "Historico estruturado de defeitos", "P4"],

        # Estoque
        ["F23", "Estoque", "EstoqueControl.xaml, NovoProdutoWindow.xaml, EditarProdutoWindow.xaml", "EstoqueOperationalService", "ProdutoRepository", "SQLite (Produtos)", "CORE", "CORE", "NAO", "NAO", "SIM (ESTOQUE_VER/CRIAR/EDITAR/EXCLUIR)", "PASS", "SIM", "Nenhum", "Manter catalogo de pecas", "P4"],
        ["F24", "Estoque", "AjusteEstoqueWindow.xaml, HistoricoEstoqueWindow.xaml", "EstoqueOperationalService", "ProdutoRepository", "SQLite (Produtos, MovimentacoesEstoque)", "CORE", "CORE", "NAO", "NAO", "SIM (ESTOQUE_AJUSTAR)", "PASS", "SIM", "Nenhum", "Manter rastreabilidade de estoque", "P4"],
        ["F25", "Estoque", "CatalogoPecasControl.xaml, RevisarCatalogoPecaWindow.xaml", "CatalogoPecasService", "N/A", "SQLite (CatalogoPecas, CatalogoPecaVeiculos)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (CATALOGO_VISUALIZAR)", "PASS", "SIM", "Nenhum", "Manter catalogo master com 4.287 pecas", "P4"],
        ["F26", "Estoque", "ImportarCatalogoPecasWindow.xaml", "CatalogoImportacaoService", "N/A", "SQLite (CatalogoImportacoes, CatalogoImportacaoErros)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (CATALOGO_VISUALIZAR)", "PASS", "SIM", "Nenhum", "Importacao estruturada de catalogos PDF/CSV", "P4"],
        ["F27", "Estoque", "TransferirEstoqueWindow.xaml", "DatabaseService", "ProdutoRepository", "SQLite (Produtos.Localizacao, Prateleira, Gaveta)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (ESTOQUE_EDITAR)", "PASS", "SIM", "Nenhum", "Reorganizacao fisica de pecas na oficina (prateleira/gaveta)", "P4"],

        # Financeiro
        ["F28", "Financeiro", "FinanceiroControl.xaml", "FinanceiroDatabaseService", "N/A", "SQLite (ContasPagar)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (FINANCEIRO_VER/PAGAR)", "PASS", "SIM", "FornecedorId ausente em parte dos registros historicos", "Preservar compatibilidade de leitura", "P3"],
        ["F29", "Financeiro", "FinanceiroControl.xaml", "FinanceiroDatabaseService", "N/A", "SQLite (ContasReceber)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (FINANCEIRO_VER/RECEBER)", "PASS", "SIM", "Nenhum", "Manter vinculo por ClienteId e Origem+ReferenciaExterna", "P4"],
        ["F30", "Financeiro", "FinanceiroControl.xaml", "FinanceiroDatabaseService", "N/A", "SQLite (MovimentacoesFinanceiras)", "CORE", "CORE", "NAO", "NAO", "SIM (FINANCEIRO_VER)", "PASS", "SIM", "Nenhum", "Manter fluxo de caixa e conciliacao", "P4"],

        # Caixa & PDV
        ["F31", "Caixa", "OperacaoCaixaWindow.xaml", "CaixaService", "N/A", "SQLite (CaixaSessoes, MovimentacoesCaixa)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (CAIXA_ABRIR/FECHAR/SANGRIA/SUPRIMENTO)", "PASS", "SIM", "Nenhum", "Manter controle de caixa por sessao", "P4"],
        ["F32", "PDV", "PDVControl.xaml, PagamentoMistoWindow.xaml", "VendaRepository", "VendaRepository", "SQLite (Vendas, VendaItens)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (PDV_VER/APLICAR_DESCONTO/CANCELAR)", "PASS", "SIM", "Nenhum", "Frente de caixa e venda balcao", "P4"],

        # Relatórios
        ["F33", "Relatorios", "RelatoriosControl.xaml", "RelatorioService", "N/A", "SQLite (OS, Orcamentos, Vendas, Produtos)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (RELATORIOS_VER)", "PASS", "SIM", "Nenhum", "Manter DRE, Fluxo, Curva ABC, Ticket Medio", "P4"],
        ["F34", "Relatorios", "RelatoriosControl.xaml", "ContabilExportService", "N/A", "Nenhum", "NOT_IMPLEMENTED", "PARTIAL", "NAO", "NAO", "SIM (RELATORIOS_VER)", "PASS", "SIM", "Exportacao SPED Fiscal e Contabil incompleta (TODO em codigo)", "Concluir gerador SPED em fase futura", "P3"],

        # Fiscal
        ["F35", "Fiscal", "FiscalOperationsControl.xaml", "FiscalOperationsCenterService, NFeHomologationService", "N/A", "SQLite (FiscalDocuments, FiscalOperations, FiscalEvents)", "NOT_IMPLEMENTED", "PARTIAL", "NAO", "SIM", "SIM (IMPORTAR_NFE_EXECUTAR)", "PASS", "SIM", "Integracao Focus em Homologacao funcional", "Manter em homologacao", "P3"],
        ["F36", "Fiscal", "FiscalOperationsControl.xaml", "FiscalProductionGuard", "N/A", "Nenhum", "NOT_IMPLEMENTED", "EXTERNAL_DEPENDENCY", "NAO", "SIM", "SIM (IMPORTAR_NFE_EXECUTAR)", "PASS", "SIM", "Emissao em producao intencionalmente bloqueada (ProductionEmissionAllowed=false)", "Liberar producao apos credenciamento SEFAZ e certificado A1/A3", "P1"],
        ["F37", "Fiscal", "ImportarNFeControl.xaml, ImportarNotaWindow.xaml", "NFeImportacaoService", "N/A", "SQLite (ImportacoesNFe, ImportacoesItens)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (IMPORTAR_NFE_EXECUTAR)", "PASS", "SIM", "Nenhum", "Importacao de XML de compra com cadastro automatico", "P4"],
        ["F38", "Fiscal", "FiscalOperationsControl.xaml", "DanfeInformationalPdfGenerator", "N/A", "SQLite (FiscalDocuments)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (IMPORTAR_NFE_EXECUTAR)", "PASS", "SIM", "Nenhum", "Geracao de DANFE informativo em PDF", "P4"],

        # Agenda
        ["F39", "Agenda", "AgendamentosControl.xaml, NovoAgendamentoPremiumWindow.xaml", "AgendamentoDatabaseService", "N/A", "SQLite (Agendamentos, AgendamentoTimeline, Produtos, Servicos)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (AGENDAMENTOS_VER/CRIAR/EDITAR/EXCLUIR)", "PASS", "SIM", "Nenhum", "Gestao de agendamentos e timeline da oficina", "P4"],
        ["F40", "Agenda", "AgendamentosControl.xaml", "AgendamentoDatabaseService", "OrdemServicoRepository", "SQLite (Agendamentos, OrdensServico)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (ORDENS_SERVICO_CRIAR)", "PASS", "SIM", "Nenhum", "Conversao de agendamento em OS por ID", "P4"],

        # Funcionários
        ["F41", "Funcionarios", "FuncionariosControl.xaml, NovoFuncionarioWindow.xaml, TwoFactorSetupWindow.xaml", "TwoFactorService", "FuncionarioRepository", "SQLite (Funcionarios)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (FUNCIONARIOS_VER/CRIAR/EDITAR/EXCLUIR)", "PASS", "SIM", "Nenhum", "Gestao de funcionarios, cargos e 2FA TOTP", "P4"],

        # Configurações & Backup
        ["F42", "Configuracoes", "ConfiguracoesSistemaWindow.xaml", "ConfiguracoesSistemaService", "N/A", "SQLite (ConfiguracoesSistema)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (SISTEMA_CONFIGURAR)", "PASS", "SIM", "Nenhum", "25 parametros de configuracao persistentes", "P4"],
        ["F43", "Backup / Restore", "BackupSettingsWindow.xaml, ConfiguracoesSistemaWindow.xaml", "DatabaseBackupService", "N/A", "SQLite (DatabaseBackups)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (SISTEMA_CONFIGURAR)", "PASS", "SIM", "Nenhum", "Backup consistente, deteccao de corrupcao e restore seguro", "P4"],
        ["F44", "Auditoria", "ConfiguracoesSistemaWindow.xaml", "AuditLogService", "AuditoriaRepository", "SQLite (AuditLogs)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (SISTEMA_CONFIGURAR)", "PASS", "SIM", "Nenhum", "Trilha de auditoria com 1.592 registros", "P4"],

        # RBAC & Segurança
        ["F45", "RBAC", "ConfigurarPermissoesWindow.xaml, GerenciarPerfisWindow.xaml, NovoPerfilWindow.xaml", "PermissionService", "N/A", "SQLite (PerfisAcesso, Permissoes, PerfilPermissoes)", "NOT_IMPLEMENTED", "CORE", "NAO", "NAO", "SIM (PERMISSOES_GERENCIAR)", "PASS", "SIM", "Duplicidade historica de perfil Mecanico / Mecânico no banco", "Normalizar IDs de perfil sem perda de historico", "P3"],

        # Licenciamento
        ["F46", "Licenciamento", "LicenseActivationWindow.xaml", "LicenseService", "N/A", "JSON File (license.json)", "NOT_IMPLEMENTED", "MOCK", "SIM", "NAO", "SIM (SISTEMA_CONFIGURAR)", "PASS", "SIM", "Licenciamento local SHA256 scaffold (IsCommercialScaffoldOnly=true)", "Construir servidor RSA/ECDSA definitivo para distribuicao em massa", "P1"],
        ["F47", "Licenciamento", "N/A", "N/A", "N/A", "Nenhum", "NOT_IMPLEMENTED", "NOT_IMPLEMENTED", "NAO", "SIM", "NAO", "NOT_TESTED", "NAO", "Servidor de licenciamento SaaS / Cloud inexistente", "Definir infraestrutura de licencas", "P1"],

        # API REST
        ["F48", "API REST", "Program.cs (Api)", "Microsoft.AspNetCore", "N/A", "Nenhum", "CORE", "CORE", "NAO", "NAO", "SIM (JwtBearer)", "PASS", "SIM", "Nenhum", "Healthcheck e autenticacao JWT com rate limiter", "P4"],
        ["F49", "API REST", "Program.cs (Api)", "OrcamentoDatabaseService, ProdutoRepository", "ProdutoRepository", "SQLite (Orcamentos, Produtos)", "CORE", "CORE", "NAO", "NAO", "SIM (Claims: ORCAMENTO_*, ESTOQUE_*)", "PASS", "SIM", "Nenhum", "Endpoints REST operacionais para Orcamentos e Estoque", "P4"],
        ["F50", "API REST", "N/A", "N/A", "N/A", "Nenhum", "NOT_IMPLEMENTED", "NOT_IMPLEMENTED", "NAO", "NAO", "NAO", "NOT_TESTED", "NAO", "Endpoints REST para OS, Clientes, Veiculos nao implementados", "Criar endpoints se integracao externa for demandada", "P3"],

        # Integrações Externas
        ["F51", "Integracoes", "VisualizarClienteWindow.xaml, NovoOrcamentoWindow.xaml", "CommercialDocumentActions", "N/A", "Nenhum", "NOT_IMPLEMENTED", "PARTIAL", "NAO", "SIM", "SIM (CLIENTES_VER)", "PASS", "SIM", "Integracao WhatsApp local via wa.me link", "Conectar API WhatsApp Cloud", "P3"],
        ["F52", "Integracoes", "N/A", "N/A", "N/A", "Nenhum", "NOT_IMPLEMENTED", "NOT_IMPLEMENTED", "NAO", "SIM", "NAO", "NOT_TESTED", "NAO", "TEF / Maquininha cartao integrada inexistente", "Integrar SDK TEF para automacao comercial de balcao", "P2"],
        ["F53", "Integracoes", "N/A", "N/A", "N/A", "Nenhum", "NOT_IMPLEMENTED", "NOT_IMPLEMENTED", "NAO", "SIM", "NAO", "NOT_TESTED", "NAO", "Gateway Pix / Boleto / Cartao online inexistente", "Integrar gateway de pagamento se cobranca online for necessaria", "P2"],
        ["F54", "Integracoes", "N/A", "N/A", "N/A", "Nenhum", "NOT_IMPLEMENTED", "NOT_IMPLEMENTED", "NAO", "SIM", "NAO", "NOT_TESTED", "NAO", "Envio automatico de email SMTP inexistente", "Configurar servico SMTP", "P3"],
        ["F55", "Integracoes", "N/A", "N/A", "N/A", "Nenhum", "NOT_IMPLEMENTED", "NOT_IMPLEMENTED", "NAO", "SIM", "NAO", "NOT_TESTED", "NAO", "Cloud Sync / Portal Web da Oficina inexistente (App 100% desktop local)", "Definir roadmap para cloud sync se aplicavel", "P2"],

        # UI / Design System
        ["F56", "UI", "Themes/Colors.Light.xaml", "ThemeResourceValidationService", "N/A", "Nenhum", "N/A", "CORE", "NAO", "NAO", "SIM", "PASS", "SIM", "Nenhum", "Tema Claro validado em todos os modulos", "P4"],
        ["F57", "UI", "Themes/Colors.Dark.xaml", "ThemeResourceValidationService", "N/A", "Nenhum", "N/A", "CORE", "NAO", "NAO", "SIM", "PASS", "SIM", "Nenhum", "Tema Escuro validado em todos os modulos com contraste WCAG AA", "P4"]
    ]
    
    matrix_csv_path = "Docs/audit/2026-09-20/B3_PRODUCT_HARDENING_MATRIX.csv"
    headers = [
        "ID", "Modulo", "Tela", "Service", "Repository", "Persistencia", "API", "Status",
        "Mock", "ExternalDependency", "RBAC", "Testes", "DesktopValidado", "Problema",
        "CorrecaoNecessaria", "Prioridade"
    ]
    
    with open(matrix_csv_path, "w", newline="", encoding="utf-8") as fp:
        writer = csv.writer(fp)
        writer.writerow(headers)
        writer.writerows(rows)
        
    print(f"B3 Matrix written: {len(rows)} features.")
    
    stats = {}
    for r in rows:
        st = r[7]
        stats[st] = stats.get(st, 0) + 1
    print("B3 Status Summary:", stats)
    return rows, stats

if __name__ == "__main__":
    generate_b3_matrix()
