# PRIMOX WORKSHOP — FASE B4
## AUDITORIA FINAL DE MOCKS, FAKES E SCAFFOLDS

**Data:** 24/09/2026  
**Critério:** Classificação objetiva para garantir transparência comercial.

---

| Item Identificado | Localização no Código | Natureza | Classificação | Justificativa Técnica |
|---|---|---|---|---|
| `IsCommercialScaffoldOnly = true` | `LicenseService.cs` | Licenciamento | MOCK / SCAFFOLD | Licença validada apenas localmente; não há servidor de DRM cloud. |
| `ProductionEmissionAllowed = false` | `FiscalProductionGuard.cs` | Fiscal | EXTERNAL_DEPENDENCY | Bloqueio de segurança intencional até contratação de certificado A1. |
| Chat de Suporte Técnico | `ChatSuporteControl.xaml` | Suporte | UI_ONLY | Interface desenvolvida sem servidor WebSocket de atendimento. |
| Central de Ajuda Interativa | `CentralAjudaWindow.xaml` | Suporte | UI_ONLY | Telas com tutoriais estáticos sem assistente interativo em nuvem. |
| Test Storage Overrides | `*Service.cs` | Testes | TEST_INFRASTRUCTURE | Diretórios de teste isolados para evitar colisão em execução paralela xUnit. |
