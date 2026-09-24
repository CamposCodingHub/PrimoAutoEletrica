# PRIMOX WORKSHOP — FASE B4
## TOP 20 GAPS DE PRODUTO E READINESS COMERCIAL

**Data:** 24/09/2026  
**Auditoria:** Honesta, sem eufemismos ou classificação promocional.

---

| Rank | Gap / Funcionalidade | Severidade | Impacto Comercial | Esforço | Status Atual | Arquivos / Componentes | Bloqueador para Ativação |
|---|---|---|---|---|---|---|---|
| **1** | Emissão Fiscal SEFAZ Produção | P1 | Alto | Médio | EXTERNAL_DEPENDENCY | `NFeEmissionService.cs`, `FiscalProductionGuard.cs` | Certificado Digital A1 e Credenciamento SEFAZ |
| **2** | Servidor de Licenciamento SaaS Nuvem | P1 | Alto | Alto | MOCK | `LicenseService.cs` | Infraestrutura Cloud / Servidor DRM |
| **3** | TEF / Maquininha Integrada | P2 | Médio | Médio | NOT_IMPLEMENTED | Módulo Financeiro / PDV | Contrato adquirente / SiTef |
| **4** | Conciliação Bancária OFX / CNAB | P2 | Médio | Médio | NOT_IMPLEMENTED | `FinanceiroControl.xaml` | Parser OFX e gerador CNAB |
| **5** | Portal do Cliente / DVI Remoto | P2 | Médio | Alto | NOT_IMPLEMENTED | Módulo DVI | Backend Web / API Nuvem |
| **6** | Integração Web Catálogos Fornecedores | P2 | Médio | Médio | NOT_IMPLEMENTED | `CatalogoPecasControl.xaml` | APIs de terceiros (DNI, Ikro, Bosch) |
| **7** | NFS-e Padrão Nacional REST | P2 | Médio | Médio | NOT_IMPLEMENTED | Módulo Fiscal | Homologação municipal |
| **8** | Sincronizador Multi-empresa Nuvem | P3 | Médio | Alto | NOT_IMPLEMENTED | `SelecaoFilialWindow.xaml` | Arquitetura multi-tenant cloud |
| **9** | App Mobile para o Cliente | P3 | Baixo | Alto | NOT_IMPLEMENTED | N/A | Desenvolvimento Mobile externo |
| **10** | Gateway PIX Automático (Webhook) | P2 | Médio | Médio | NOT_IMPLEMENTED | Módulo Financeiro | Conta PJ integrada API PIX |
| **11** | Chat Suporte Técnico Integrado | P3 | Baixo | Baixo | UI_ONLY | `ChatSuporteControl.xaml` | Servidor WebSocket de suporte |
| **12** | Central de Ajuda Interativa Guiada | P4 | Baixo | Baixo | UI_ONLY | `CentralAjudaWindow.xaml` | Conteúdo e vídeos de treinamento |
| **13** | Notificações Agendadas SMS/WhatsApp | P3 | Médio | Baixo | PARTIAL | `AgendamentoNotificacaoService.cs` | Provedor de mensageria (Z-API/Twilio) |
| **14** | Envio de Orçamento por E-mail SMTP | P3 | Médio | Baixo | UI_ONLY | `OrcamentosView.xaml` | Configuração SMTP corporativo |
| **15** | Migração Definitiva Money no Banco | P2 | Alto | Médio | BLOCKED | `MoneyMigrationRehearsalTests.cs` | Janela de migração com backup verificado |
| **16** | FornecedorId em ContasPagar Legadas | P3 | Médio | Baixo | PARTIAL | `FinanceiroDatabaseService.cs` | Dados históricos sem chave relacional |
| **17** | Roteiros D07 a D17 Estruturados | P3 | Médio | Médio | PARTIAL | `roteiros-resultados.json` | Migração de schema legado para classe dedicada |
| **18** | Relatórios Personalizados BI Avançado | P3 | Baixo | Médio | PARTIAL | `RelatorioExportService.cs` | Motor de geração dinâmica de relatórios |
| **19** | Entrada Automática XML NFe em Lote | P3 | Médio | Baixo | PARTIAL | `ImportarNFeControl.xaml` | Matching inteligente de fornecedor/produto |
| **20** | Backup em Nuvem Criptografado | P3 | Médio | Médio | PARTIAL | `DatabaseBackupService.cs` | Bucket AWS S3 / Azure Blob Storage |
