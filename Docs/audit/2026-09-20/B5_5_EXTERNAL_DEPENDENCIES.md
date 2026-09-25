# PRIMOX Workshop — Fase B5.5: Matriz de Dependências Externas

**Data:** 2026-09-24  
**Escopo:** Mapeamento Honesto de Funcionalidades Locais vs Dependências Externas

---

## 1. Classificação das Dependências

| Funcionalidade | Estado Atual | Classificação Oficial | Detalhes |
| :--- | :--- | :---: | :--- |
| **SEFAZ / NF-e / NFC-e** | Scaffold e regras prontas | **PASS_WITH_EXTERNAL_DEPENDENCY** | Requer Certificado Digital A1 e SEFAZ |
| **Licenciamento Online** | Scaffold local ativo | **PASS_WITH_EXTERNAL_DEPENDENCY** | Requer servidor comercial de licenças |
| **WhatsApp Oficial** | Validações locais ativas | **PASS_WITH_EXTERNAL_DEPENDENCY** | Requer API / Gateway WhatsApp Business |
| **Hardware OBD-II Físico**| Camada de software pronta | **PASS_WITH_EXTERNAL_DEPENDENCY** | Requer interface scanner física acoplada |
| **TEF / Pagamento Integrado** | Suporte planejado | **PARTIAL** | Fluxo operacional atual usa POS externo |
| **NFS-e Nacional** | Suporte planejado | **PARTIAL** | Integração varia por município |
| **Multi-Loja em Nuvem** | Arquitetura desenhada | **NOT_IMPLEMENTED** | Previsto no Roadmap Cloud v2 |
