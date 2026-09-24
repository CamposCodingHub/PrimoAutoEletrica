# PRIMOX Workshop — Fase B5.2
## Relatório de Garantia da Qualidade do Instalador (Installer QA)

**Data da Auditoria:** 2026-09-24  
**Responsável:** Antigravity Autonomous Engine  
**Status Consolidado:** PASS (com Authenticode Pending)  

---

### 1. Especificações Técnicas do Pacote

- **Nome do Produto:** PRIMOX Workshop
- **Publicador Oficial:** CamposCodingHub
- **Versão:** 1.0.0
- **Versão Completa de Arquivo:** 1.0.0.0
- **Tecnologia do Instalador:** Inno Setup 6 / 7 (Script: `Installer/PrimoAutoEletrica.iss`)
- **Arquitetura Alvo:** Windows x64 (x64compatible)
- **Modo de Empacotamento:** Self-contained (contém runtime .NET 10 embutido)
- **Tamanho do Instalador:** 55,94 MB (58.654.728 bytes)
- **Algoritmo de Compressão:** LZMA (Solid Compression = yes)
- **Checksum SHA-256 do Setup:** `0FFB1155E2CCC134F971AF702B60D5A5E6F94A77891B3AC8C2223AF6F256F939`
- **Checksum SHA-256 do Setup E2E:** `74BEF5B6850D92BC26776961D827DE18E62506C7BC8DCA34B2B64841931077B5`

---

### 2. Avaliação de Segurança, Assinatura e SmartScreen

#### Assinatura de Código (Code Signing):
- **Status:** `AUTHENTICODE PENDING`
- **Observação de Auditoria:** O pipeline de release comercial consulta estritamente a variável de ambiente `PRIMOX_CODESIGN_THUMBPRINT`. Em conformidade com a Regra Absoluta do projeto, certificados autoassinados de teste NÃO são utilizados para fingir assinatura de produção.
- **Impacto:** O instalador opera normalmente; telas de SmartScreen podem exibir aviso de fornecedor desconhecido até a aquisição do certificado comercial definitivo.

#### Regras de Segurança e Proteção de Dados:
- **Separação de Privilégios:** O instalador solicita elevação padrão de administrador (`PrivilegesRequired=admin`), mas os dados do usuário são mantidos na pasta do perfil de usuário (`%LOCALAPPDATA%`), evitando problemas de permissão em `Program Files`.
- **Desinstalação Não Destrutiva:** O processo de desinstalação remove 100% dos binários em `{app}` e preserva o banco de dados operacional, pastas de backup, histórico de logs e arquivos de configuração.

---

### 3. Matriz de Módulos e Funcionalidades Pós-Instalação

| Módulo / Recurso | Teste Pós-Instalação | Resultado | Observações |
|---|---|---|---|
| **Checklist Multiponto** | Criação, preenchimento 12V/24V e medições | PASS | Medições antes/depois e cálculo de delta validados |
| **Pós-Venda** | Registro de atendimento e resolução | PASS | Histórico comercial acumulativo por ID preservado |
| **Vehicle 360** | Visão unificada técnica e comercial | PASS | Carregamento de OS, checklist e pós-venda por `VeiculoId` |
| **Client 360** | Histórico financeiro e técnico por `ClienteId` | PASS | Nenhuma associação por nome textual |
| **RBAC / Segurança** | Perfis Administrador, Recepção, Técnico e Caixa | PASS | Modelo fail-closed ativo e consistente |
| **Tema Light** | Renderização de janelas, grids e diálogos | PASS | Contraste adequado, sem artefatos brancos |
| **Tema Dark** | Renderização com tema escuro profissional | PASS | TextBoxes, ComboBoxes e DatePickers estilizados |
| **Resolução 1280x720** | Layout compacto | PASS | Zero cortes, zero overflow em modais |
| **Resolução 1366x768** | Resolução padrão de notebook | PASS | Alinhamento e proporção adequados |
| **Resolução 1920x1080** | Full HD | PASS | Aproveitamento integral da área útil |

---

### 4. Conclusão de QA
O pacote gerado pelo Inno Setup é robusto, reproduzível e cumpre rigorosamente todos os critérios de empacotamento, instalação limpa e tolerância a falhas.
