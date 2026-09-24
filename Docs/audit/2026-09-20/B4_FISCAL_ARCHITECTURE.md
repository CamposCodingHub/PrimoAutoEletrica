# PRIMOX WORKSHOP — FASE B4
## ARQUITETURA FISCAL DEFINITIVA (SEFAZ / NF-e / NFC-e / NFS-e)

**Data:** 24/09/2026  
**Status Atual:** EXTERNAL_DEPENDENCY / HOMOLOGAÇÃO  
**Regra:** `FiscalProductionGuard.ProductionEmissionAllowed = false` estritamente mantido.

---

### 1. Fluxo de Emissão Fiscal

1. **Geração do XML:** Mapeamento de produtos, NCM, CFOP, CST/CSOSN, ICMS, PIS, COFINS a partir da Ordem de Serviço concluída.
2. **Assinatura Digital A1:** Uso de certificado digital modelo A1 (.pfx em nuvem ou local) com `System.Security.Cryptography.Xml`.
3. **Transmissão SEFAZ:** Comunicação SOAP com envelope WS SEFAZ do estado do emitente.
4. **Contingência Offline (NFC-e):** Geração de DANFE NFC-e offline em contingência para transmissão posterior em até 24h.
5. **NFS-e (Serviços):** Mapeamento do Padrão Nacional de NFS-e via API REST.
