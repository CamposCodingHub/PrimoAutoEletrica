# PRIMOX WORKSHOP — B3
## RELATÓRIO OFICIAL DE PRONTIDÃO DE LICENCIAMENTO (LICENSE READINESS)

**Data:** 2026-09-24  
**Branch:** `audit/product-discovery-2026-09`  
**Status do Licenciamento Comercial:** **SCAFFOLD LOCAL (NÃO É SAAS DEFINITIVO)**

---

### 1. Diagnóstico do Mecanismo Atual

O sistema de licenciamento atual é governado pela classe:
`PrimoAutoEletrica.Services.LicenseService`

A própria classe declara formalmente em seu código-fonte:
```csharp
// SCAFFOLD_ONLY: licenca local JSON + SHA256 — SEM assinatura RSA/ECDSA e SEM license server.
public const bool IsCommercialScaffoldOnly = true;
```

**Classificação Oficial:** `PARTIAL / MOCK / SCAFFOLD`

---

### 2. Recursos Presentes vs. Recursos Faltantes

#### O que o PRIMOX possui hoje:
- Geração determinística de `HardwareId` combinando `MachineName`, `UserName`, processadores e versão do SO;
- Validação offline de chave de licença com cálculo de expiração de dias;
- Armazenamento em arquivo local JSON (`license.json`);
- Tela de ativação de licença amigável (`LicenseActivationWindow.xaml`);
- Bloqueio de acesso se a data de expiração for atingida.

#### O que o PRIMOX NÃO possui hoje (Gaps para distribuição em massa):
- Servidor central de licenciamento online (SaaS License Server);
- Assinatura assimétrica de chaves criptográficas (RSA-4096 ou ECDSA P-256);
- Revogação remota de licenças;
- Proteção contra manipulação manual de relógio de sistema operacional;
- Controle de concorrência de estações de trabalho em rede local.

---

### 3. Recomendação Estratégica para Distribuição Comercial
Para implantação em clientes externos sem risco de evasão de licença, deve ser desenvolvido um serviço web leve (ex: Cloudflare Worker ou Azure Function) com par de chaves assimétricas, onde o PRIMOX valida localmente a assinatura com a chave pública do fornecedor.
