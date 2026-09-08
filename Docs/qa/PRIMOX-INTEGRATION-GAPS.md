# PRIMOX — Integration Gaps 1.0

## CRITICAL

| ID | Descrição | Produto atual vs futuro |
|----|-----------|-------------------------|
| IG-C01 | NF-e emissão ausente; import ≠ emissão | Futuro P0 compliance se vender fiscal |
| IG-C02 | NotificationService grava/retorna sucesso sem enviar | Produto atual — risco de mentira operacional |

## HIGH

| ID | Descrição |
|----|-----------|
| IG-H01 | FilialService scaffold vendido como multi-filial |
| IG-H02 | API sem autenticação |
| IG-H03 | JWT packages sem wiring |
| IG-H04 | Testes API aceitam NotFound / rotas inexistentes |
| IG-H05 | Sync remoto inexistente |

## MEDIUM

| ID | Descrição |
|----|-----------|
| IG-M01 | WhatsApp só wa.me (sem Cloud) |
| IG-M02 | Sem SMTP |
| IG-M03 | VMs órfãs DI (Funcionarios/RelatoriosModerno/Estoque/Printer) |
| IG-M04 | Migrations código 27 vs DB histórico ~32 |
| IG-M05 | Cryptography.Xml package sem uso |
| IG-M06 | ExternalBackupService orphan |
| IG-M07 | Nested CI / empty Maui / empty Api stub |

## LOW / INFO

| ID | Descrição |
|----|-----------|
| IG-L01 | PIX gateway ausente (PIX interno OK) |
| IG-L02 | NFC-e/NFS-e futuros |
| IG-L03 | LicenseActivationWindow não ligada |
| IG-L04 | Shells 0-byte services |
| IG-L05 | Run-Keycloak.ps1 vazio |

## Decisões de produto pendentes

1. NF-e: SEFAZ direta vs Provider  
2. Comunicar via Cloud WhatsApp/SMS ou manter wa.me/mailto  
3. Prioridade API autenticada vs fiscal  
4. Remover shells 0-byte?  
5. Horizonte SaaS  
