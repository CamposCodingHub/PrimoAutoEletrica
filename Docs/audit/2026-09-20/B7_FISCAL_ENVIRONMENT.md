# PRIMOX WORKSHOP — FASE B7: GATE 12
# FISCAL ENVIRONMENT & PRODUCTION GUARD ARCHITECTURE
**Data da Auditoria:** 2026-09-25  
**Fase:** B7 — Production Money Migration + Fiscal/SEFAZ Homologation  
**Responsável Técnico:** Antigravity Autonomous Audit Engine  
**Status do Gate:** PASS  

---

## 1. OBJETIVO DO GATE 12

Auditar e comprovar a segurança arquitetural da camada fiscal do PRIMOX Workshop, demonstrando o isolamento absoluto entre o ambiente de **Homologação (Sandbox SEFAZ / FocusNFe)** e o ambiente de **Produção (SEFAZ SP / Live FocusNFe)**, com ênfase no mecanismo de trava inviolável implementado pela classe `FiscalProductionGuard`.

---

## 2. ARQUITETURA DO FISCAL PRODUCTION GUARD

O PRIMOX foi projetado sob a premissa de **Zero Risco Fiscal**. Em nenhuma hipótese o software pode emitir uma nota fiscal contra a SEFAZ de produção sem que todas as condicionantes legais, operacionais e de credenciamento do cliente estejam ativas e auditadas.

### 2.1. Implementação no Código-Fonte (`FiscalProductionGuard.cs`)
```csharp
public static class FiscalProductionGuard
{
    public static bool ProductionEmissionAllowed { get; private set; } // Default: false

    public static void EnsureEnvironmentAllowed(FiscalEnvironment environment)
    {
        if (environment != FiscalEnvironment.Production) return;

        if (!ProductionEmissionAllowed)
        {
            throw new FiscalProductionBlockedException(
                "Emissao fiscal em PRODUCAO esta bloqueada. Use Homologacao ate a fase de liberacao.");
        }
    }

    public static FiscalProviderResult? TryDenyProduction(
        FiscalEnvironment environment,
        Guid operationId,
        string idempotencyKey)
    {
        if (environment != FiscalEnvironment.Production || ProductionEmissionAllowed) return null;

        return FiscalProviderResult.Fail(
            FiscalDocumentStatus.ProductionBlocked,
            FiscalErrorKind.ProductionBlocked,
            "Producao fiscal bloqueada pela fundacao PRIMOX. Homologacao apenas.",
            operationId,
            idempotencyKey,
            internalCode: "FISCAL-PROD-BLOCKED");
    }
}
```

### 2.2. Comportamento Garantido por Testes Automatizados
| Cenário Auditado | Teste Unitário / Integração | Resultado | Comportamento Observado |
|---|---|---|---|
| Tentativa de chamada em Produção | `ProductionGuard_BloqueiaEmissaoProducao` | **PASS** | Retorna status `ProductionBlocked` e código interno `FISCAL-PROD-BLOCKED`. Nenhuma requisição é enviada. |
| Bloqueio no HTTP Provider | `FocusProvider_Producao_NaoChamaHttp` | **PASS** | O manipulador HTTP registra `SendCount = 0`. Zero bytes trafegados na rede. |
| URL de Produção mascarada como Homologação | `Config_NaoPermiteUrlProducaoComoHomolog` | **PASS** | `FiscalConfigurationService.Normalize` detecta URL `api.focusnfe.com.br` em Homologação, desativa o `LiveHttpEnabled` e restaura a URL oficial de sandbox. |
| Ausência de Token | `FocusProvider_SemToken_NaoAutoriza` | **PASS** | Bloqueia emissão com código `FISCAL-FOCUS-TOKEN-MISSING`. |

---

## 3. SEPARAÇÃO E CONFIGURAÇÃO DE AMBIENTES

O arquivo de configuração `fiscal-config.json` gerencia as variáveis de conexão com separação estrita de escopo:

```json
{
  "Environment": "Homologation",
  "HomologationBaseUrl": "https://homologacao.focusnfe.com.br/v2/",
  "ProductionBaseUrl": "https://api.focusnfe.com.br/v2/",
  "LiveHttpEnabled": false,
  "TimeoutSeconds": 30,
  "MaxRetries": 3,
  "Issuer": {
    "Cnpj": "11222333000181",
    "RazaoSocial": "Primo Auto Eletrica LTDA",
    "RegimeTributario": "1",
    "SerieNFe": "1"
  }
}
```

### Regras de Imutabilidade e Segurança de Ambiente:
1. **Default Seguro:** O ambiente padrão inicializado em qualquer instalação nova é sempre `Homologation` com `LiveHttpEnabled = false`.
2. **Prevenção de Tráfego Cego:** O sistema nunca executa polling descontrolado. Transações que sofrem timeout são registradas localmente com sua `IdempotencyKey` única para consulta posterior, sem criar novas emissões acidentais (`Timeout_DepoisConsultar_AutorizaSemNovaEmissaoCega`).
3. **Persistência Atômica:** O `FiscalOperationStore` registra o estado do documento (`Draft -> Processing -> Authorized / Rejected / ProductionBlocked`) em banco SQLite local protegido por transação.

---

## 4. CONCLUSÃO DO GATE 12

A infraestrutura de ambientes fiscais e o guardião de produção (`FiscalProductionGuard`) oferecem proteção integral contra emissões indesejadas ou desautorizadas em ambiente produtivo da SEFAZ.

**Resultado do Gate 12:** **PASS**
