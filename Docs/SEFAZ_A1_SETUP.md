# Docs/SEFAZ_A1_SETUP.md

**Atualizado:** 2026-09-13  

## Estado atual

Certificado A1/PFX **não** é o modelo operacional atual do PRIMOX na branch `migration/net10`.

| Item | Estado |
|------|--------|
| Modelo atual Focus | Token homolog + DPAPI (`FiscalConfigurationService`) |
| `ICertificateProvider` | `NullCertificateProvider` em produção DI |
| `IXmlSigner` | `BlockedXmlSigner` (BLOCKED_EXTERNAL) |
| Fake cert (testes) | `FakeCertificateProvider` |

## Quando este guia for preenchido

Somente com:

1. Certificado A1 de homologação real do estabelecimento  
2. Política de armazenamento seguro (nunca senha em texto puro / nunca commit)  
3. Decisão se assinatura local é necessária além do provedor Focus  

Até lá: **não** inventar passos de instalação de PFX neste arquivo.

## Avanço

Abstrações de certificado/assinatura foram criadas no NET10-26 para receber A1 no futuro sem reescrever o domínio fiscal.
