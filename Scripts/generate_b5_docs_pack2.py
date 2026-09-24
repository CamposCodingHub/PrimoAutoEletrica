# -*- coding: utf-8 -*-
"""
Pack 2 of B5.0 Documents:
8. B5_0_CERTIFICATE_ARCHITECTURE.md
9. B5_0_LICENSE_RELEASE_PLAN.md
10. B5_0_CLOUD_ROADMAP.md
11. B5_0_INSTALLER_DECISION.md
12. B5_0_UPDATE_STRATEGY.md
"""

import os

DOC_DIR = r"c:\Projetos\PrimoAutoEletrica\Docs\audit\2026-09-20"
os.makedirs(DOC_DIR, exist_ok=True)

# -------------------------------------------------------------
# 8. B5_0_CERTIFICATE_ARCHITECTURE.md
# -------------------------------------------------------------
cert_arch_md = """# PRIMOX WORKSHOP — B5.0
## ARQUITETURA TÉCNICA DE CERTIFICADOS DIGITAIS (A1 E A3)

**Módulo:** Fiscal e Assinatura Digital  
**Padrão ICP-Brasil:** e-CNPJ / e-CPF / NF-e / NFC-e  
**Status da Arquitetura:** **DEFINIDA PARA HOMOLOGAÇÃO (FASE B7)**

---

### 1. Comparativo Técnico: Certificados A1 vs A3

O PRIMOX Workshop adota arquiteturas distintas para suporte a certificados digitais A1 e A3, considerando as restrições de ambiente de oficina mecânica:

| Característica | Certificado A1 | Certificado A3 |
| :--- | :--- | :--- |
| **Mídia de Armazenamento** | Arquivo de software (`.pfx` / `.p12`) | Mídia física (Token USB criptográfico ou Smartcard) |
| **Validade Típica** | 1 ano | 1 a 3 anos |
| **Interatividade** | Totalmente automatizado em background | Exige digitação manual de PIN pelo usuário ou CSP driver |
| **Instalação Multi-Estação** | Pode ser replicado ou carregado via rede local segura | Preso fisicamente a 1 computador específico |
| **Desempenho de Assinatura**| Criptografia em memória de alta performance (<50ms)| Depende do hardware do token USB (300ms a 1500ms) |
| **Recomendação PRIMOX** | **ALTAMENTE RECOMENDADO (Padrão Oficina)** | Suportado sob demanda com ressalvas operacionais |

---

### 2. Segurança de Armazenamento e Senhas

#### Regra Absoluta:
**NENHUMA SENHA OU CHAVE PRIVADA DE CERTIFICADO DEVE SER GRAVADA EM TEXTO PURO (PLAIN TEXT).**

1. **Proteção de Senha do Certificado A1:**
   - A senha do arquivo `.pfx` é criptografada utilizando a API nativa do Windows **DPAPI (Data Protection API)** com escopo de máquina local ou usuário (`DataProtectionScope.CurrentUser` / `LocalMachine`), garantindo que apenas a aplicação autorizada no Windows possa descriptografar o segredo.
   - O segredo criptografado é armazenado na tabela `Configuracoes` do SQLite sob a chave `Fiscal.CertificadoA1.SecretBytes`.
2. **Manipulação em Memória do Certificado A1:**
   - O certificado é carregado em instância efêmera `X509Certificate2(pfxBytes, securePassword, X509KeyStorageFlags.EphemeralKeySet)`.
   - O arquivo original em disco pode ser mantido em diretório protegido `%LOCALAPPDATA%\\PrimoAutoEletrica\\Cert\\` com permissão restrita de leitura apenas para o usuário do Windows executando a aplicação.
3. **Tratamento do Certificado A3:**
   - Comunicação via Cryptographic Service Provider (CSP) nativo do Windows ou PKCS#11.
   - Detecção do certificado no repositório de certificados do Windows (`Cert:\\CurrentUser\\My`).
   - O PIN é mantido em memória protegida (`SecureString`) durante a sessão do operador ou delegado ao diálogo de segurança nativo do driver do fabricante (SafeNet, CryptoID, etc.).

---

### 3. Monitoramento de Validade e Alertas Preventivos

O sistema executa checagem diária na inicialização do módulo fiscal:
- **Status Normal:** Validade > 30 dias.
- **Alerta Amarelo:** Validade entre 15 e 30 dias (Aviso na barra de status da oficina).
- **Alerta Crítico:** Validade < 15 dias (Notificação ao Administrador para renovação com a Autoridade Certificadora).
- **Certificado Expirado:** Bloqueio preventivo de tentativa de emissão para evitar rejeição SEFAZ (Rejeição 290 - Certificado Assinatura inválido/expirado).

---

### 4. Sanitização e Logs de Auditoria

Todas as operações de assinatura digital registram logs de auditoria estruturados sem expor dados confidenciais:
- **Registrado no Log:** Subject, CNPJ do titular, Número de série, Autoridade Emissora, Data de expiração, Algoritmo (SHA-256), Tempo de assinatura (ms), DigestValue do XML assinado.
- **PROIBIDO no Log:** Senha do certificado, PIN, Chave privada (Private Key exponent/modulus).
"""

with open(os.path.join(DOC_DIR, "B5_0_CERTIFICATE_ARCHITECTURE.md"), "w", encoding="utf-8") as f:
    f.write(cert_arch_md)

print("8. B5_0_CERTIFICATE_ARCHITECTURE.md OK")

# -------------------------------------------------------------
# 9. B5_0_LICENSE_RELEASE_PLAN.md
# -------------------------------------------------------------
license_plan_md = """# PRIMOX WORKSHOP — B5.0
## PLANO DE ARQUITETURA E RELEASE DE LICENCIAMENTO COMERCIAL

**Módulo:** Gestão de Licenças e Proteção de Propriedade Intelectual  
**Tecnologia:** Criptografia Assimétrica RSA-4096 / Ed25519 + Assinatura Digital  
**Status:** **ARQUITETURA DEFINIDA (SEM DEPENDÊNCIA DE SERVIDOR FAKE)**

---

### 1. Diagnóstico do Scaffold Atual e Objetivo de Transição

- **Estado Atual (Fase B4):** O sistema possui o `LicenseService` com a flag de segurança `IsCommercialScaffoldOnly = true`, operando em modo local fail-closed com verificação sintática e permissões ativas.
- **Diretriz de Transição:** A versão comercial v1.0 **NÃO criará um servidor SaaS fake**. 
  A arquitetura é projetada em **2 Níveis Consecutivos**:
  - **Nível 1 (Offline Standalone Cryptographic License):** Licença assinada digitalmente pela software house, entregue como arquivo `.lic` importado na instalação.
  - **Nível 2 (Online Heartbeat Activation Server):** Ativação pela internet com verificação periódica (Fase B7/B8).

---

### 2. Arquitetura da Licença Offline Criptográfica (Nível 1 - v1.0)

O PRIMOX v1.0 utiliza o modelo de **Licença Assinada por Chave Assimétrica Privada**:

```mermaid
graph TD
    A[Software House PRIMOX] -->|Chave Privada RSA-4096| B[Gerador de Licença Oficial]
    B -->|Payload JSON Assinado| C[Arquivo de Licenca: primox.lic]
    C -->|Entregue ao Cliente na Instalacao| D[PRIMOX Workshop Desktop]
    D -->|Chave Publica RSA Embutida no Binario| E[Validador de Assinatura Local]
    E -- Valido --> F[Liberacao Total dos Recursos Contratados]
    E -- Invalido / Adulterado --> G[Modo Demonstracao / Bloqueio Fail-Closed]
```

#### Estrutura do Payload da Licença (`primox.lic`):
```json
{
  "LicenseId": "LIC-2026-PRX-00842",
  "Customer": {
    "CNPJ": "12.345.678/0001-90",
    "RazaoSocial": "AUTO ELETRICA CENTRAL LTDA",
    "Email": "financeiro@autoeletricacentral.com.br"
  },
  "HardwareBinding": {
    "MotherboardUuidHash": "A8F1C3E0...B4D1",
    "CpuIdHash": "7D2E...88F0"
  },
  "Plan": {
    "Type": "COMMERCIAL_WORKSHOP_HEAVY",
    "MaxWorkstations": 3,
    "ExpiresAt": "2027-09-30T23:59:59Z",
    "Modules": [
      "CLIENT_VEHICLE_360",
      "ORCAMENTOS_OS",
      "ESTOQUE_CATALOGO",
      "FINANCEIRO_COMPLETO",
      "AUTO_ELETRICA_TECH_HEAVY_24V",
      "DVI_INSPECTION_LOCAL",
      "POS_VENDA_GARANTIAS"
    ]
  },
  "Signature": "MEQCID...AssinaturaDigitalRSA4096EmBase64..."
}
```

---

### 3. Vínculo de Hardware (Hardware Binding)

Para prevenir pirataria por cópia indiscriminada da pasta de instalação:
1. O aplicativo calcula o `HardwareFingerprint` do computador mestre combinando:
   - UUID da Placa-Mãe (via WMI / `Win32_BaseBoard.SerialNumber`)
   - Identificador do Processador (via `Win32_Processor.ProcessorId`)
   - Hash SHA-256 dos componentes.
2. A licença é gerada vinculada ao hash da máquina instalada.
3. Permite tolerância de troca de componentes menores (memória, placa de vídeo, disco secundário) sem invalidação acidental.

---

### 4. Período de Graça e Tolerância Offline (Grace Period)

- **Validade do Token Offline:** 365 dias para licença anual, ou renovação mensal programada.
- **Tolerância a Relógio Adulterado:** O software monitora timestamps crescentes no banco SQLite (`AuditLog`, `EventosOS`). Se a data do sistema for atrasada artificialmente para tentar burlar a expiração, o sistema entra em alerta de inconsistência temporal.
- **Grace Period Operacional:** Ao atingir a data de expiração, a oficina tem **15 dias de período de tolerância operacional** com aviso diário ao gestor antes de travar a emissão de novas ordens de serviço, garantindo que o negócio da oficina nunca pare de forma abrupta.
"""

with open(os.path.join(DOC_DIR, "B5_0_LICENSE_RELEASE_PLAN.md"), "w", encoding="utf-8") as f:
    f.write(license_plan_md)

print("9. B5_0_LICENSE_RELEASE_PLAN.md OK")

# -------------------------------------------------------------
# 10. B5_0_CLOUD_ROADMAP.md
# -------------------------------------------------------------
cloud_roadmap_md = """# PRIMOX WORKSHOP — B5.0
## ROADMAP DE SERVIÇOS EM NUVEM E COMPATIBILIDADE SAAS

**Estratégia:** Desktop-First Local com Serviços Híbridos Opcionais  
**Status do Roadmap:** **PLANEJAMENTO DE LONGO PRAZO (TRILHA B / PÓS-RELEASE)**

---

### 1. Diretriz Estratégica: Por que o PRIMOX é Desktop-First?

No mercado automotivo real (oficinas de reparação, centros de linha pesada, pátios de transportadoras):
1. **Conectividade Instável:** Muitas oficinas operam em galpões industriais ou beiras de rodovias com sinal 4G/5G oscilante. O software não pode travar ou congelar porque a internet caiu no meio de uma ordem de serviço.
2. **Velocidade e Produtividade:** A abertura de telas, leitura de PDFs de catálogo e resposta do leitor de código de barras devem ser instantâneas (<10ms).
3. **Privacidade de Dados:** A oficina tem a garantia de que seu cadastro de clientes e margens de lucro estão armazenados no próprio estabelecimento.

---

### 2. Arquitetura Alvo de Serviços Híbridos em Nuvem

O roadmap divide os serviços em nuvem em componentes satélites independentes, sem transformar o núcleo desktop em web:

```mermaid
graph TD
    subgraph "Oficina Local (Desktop)"
        D[PRIMOX Workshop Desktop WPF]
        LDB[(SQLite Local)]
        D <--> LDB
    end

    subgraph "Nuvem PRIMOX (Serviços Satélites)"
        A[Serviço de Backup Cloud Criptografado]
        B[Portal Web do Cliente - Consulta de OS]
        C[Gateway de WhatsApp / SMS Corporativo]
        D2[Servidor de Ativação e Atualizações]
    end

    D -.->|Envio Seguro Diário| A
    D -.->|Upload de Status/Fotos| B
    D -.->|Disparo de Mensagens| C
    D -.->|Heartbeat / Checagem| D2
```

---

### 3. Fases de Implementação Cloud

| Serviço Cloud | Fase Prevista | Dependências Técnicas | Benefício ao Cliente |
| :--- | :---: | :--- | :--- |
| **Backup Automático em Nuvem** | **B6 / B7** | Storage AWS S3 / Azure Blob + Criptografia AES-256 | Proteção total contra queima de HD ou roubo na oficina |
| **Servidor de Licenciamento & Update** | **B7** | API ASP.NET Core + PostgreSQL | Atualização automática sem necessidade de técnico no local |
| **Portal Web do Cliente (DVI/OS)**| **B8 (Trilha B)**| WebApp React/Next.js + Bucket de Imagens CDN | Cliente aprova orçamento e vê fotos do DVI no celular |
| **Gateway WhatsApp Oficial** | **B8 (Trilha B)**| Meta Cloud API / Provedor BSP Z-API ou Gupshup | Envio de status de OS diretamente pelo WhatsApp |
| **Sincronização Multi-Filial** | **B8 (Trilha B)**| Engine de Replicação Bidirecional / Sync Agent | Rede de oficinas compartilha estoque e histórico de clientes |
"""

with open(os.path.join(DOC_DIR, "B5_0_CLOUD_ROADMAP.md"), "w", encoding="utf-8") as f:
    f.write(cloud_roadmap_md)

print("10. B5_0_CLOUD_ROADMAP.md OK")

# -------------------------------------------------------------
# 11. B5_0_INSTALLER_DECISION.md
# -------------------------------------------------------------
installer_decision_md = """# PRIMOX WORKSHOP — B5.0
## DECISÃO TÉCNICA DO PACOTE DE INSTALAÇÃO: MSIX vs INNO SETUP

**Escopo:** Distribuição e Instalação Comercial no Windows  
**Decisão:** **INNO SETUP ADOTADO COMO INSTALADOR OFICIAL DA v1.0**

---

### 1. Matriz Técnica Comparativa

| Critério de Avaliação | Microsoft MSIX | Jordan Russell Inno Setup | Vencedor / Decisão |
| :--- | :--- | :--- | :---: |
| **Compatibilidade Windows** | Windows 10 (1809+) e Windows 11. Falha em Windows 7/8/Server legados. | Windows 7, 8, 8.1, 10, 11 e Windows Server (x86 e x64). | **Inno Setup** |
| **Preservação de Dados (%LOCALAPPDATA%)** | Sistema de virtualização de sistema de arquivos (VFS) pode isolar ou apagar dados ao desinstalar. | Permite controle cirúrgico para NUNCA deletar o banco SQLite nem backups na desinstalação. | **Inno Setup** |
| **Exigência de Assinatura de Código** | Obrigatório certificado digital reconhecido pela Microsoft (SmartScreen bloqueia se não for assinado). | Suporta instalação limpa mesmo sem certificado comercial (exibe apenas aviso UAC padrão). | **Inno Setup** |
| **Criação de Atalho na Área de Trabalho** | Restrito e mediado pela shell do Windows AppX. | Controle total de criação de atalho na Área de Trabalho e Menu Iniciar para Todos os Usuários. | **Inno Setup** |
| **Pré-requisitos (.NET 10 Desktop Runtime)** | Requer packaging complexo ou bundle de 120MB+ embutido. | Script verifica se o .NET 10 Runtime está instalado; se não, baixa e instala silenciosamente. | **Inno Setup** |
| **Facilidade de Customização de Script** | Baseado em XML e manifestos restritos. | Script Pascal Script altamente customizável com lógica procedural de pré-instalação e pós-instalação. | **Inno Setup** |

---

### 2. Racional da Decisão

Nas oficinas mecânicas do Brasil:
1. O ambiente operacional é heterogêneo: computadores de balcão podem ter Windows 10 Home, Windows 11 Pro ou versões corporativas sem loja Microsoft Store habilitada.
2. O **MSIX impõe isolamento de contêiner**, dificultando o acesso direto do suporte técnico ao arquivo `primoauto_operacional.db` e às pastas de log em caso de manutenção emergencial.
3. O **Inno Setup** é um padrão industrial consagrado no setor automotivo, permitindo:
   - Instalação no padrão `C:\\Program Files\\PRIMOX Workshop\\` ou `%LOCALAPPDATA%\\PrimoAutoEletrica\\App\\`.
   - Garantia formal de que atualizações de versão **NUNCA sobrescreverão o banco de dados de produção da oficina**.
   - Criação de backup automático do banco antes de atualizar os executáveis.

---

### 3. Estrutura do Script de Instalação Inno Setup (`primox_setup.iss`)

- **Nome do Produto:** PRIMOX Workshop
- **Versão:** 1.0.0
- **Publisher:** CamposCodingHub / PRIMOX Auto Elétrica
- **Diretório Alvo da Aplicação:** `{localappdata}\\PrimoAutoEletrica\\App`
- **Diretório de Dados:** `{localappdata}\\PrimoAutoEletrica\\`
- **Regra de Desinstalação (`UninstallDelete`):**
  - Apaga executáveis e DLLs.
  - **PRESERVA OBRIGATORIAMENTE:** `*.db`, `*.db-shm`, `*.db-wal`, subpasta `Backups\\` e subpasta `Dvi\\`.
"""

with open(os.path.join(DOC_DIR, "B5_0_INSTALLER_DECISION.md"), "w", encoding="utf-8") as f:
    f.write(installer_decision_md)

print("11. B5_0_INSTALLER_DECISION.md OK")

# -------------------------------------------------------------
# 12. B5_0_UPDATE_STRATEGY.md
# -------------------------------------------------------------
update_strat_md = """# PRIMOX WORKSHOP — B5.0
## ESTRATÉGIA DE ATUALIZAÇÃO CONTÍNUA E VERSIONAMENTO DE BANCO

**Escopo:** Transição entre versões (v1.0 → v1.0.1 → v1.1 → v2.0)  
**Status:** **DEFINIDO**

---

### 1. Ciclo de Vida de Atualização do Software

O processo de atualização do PRIMOX Workshop segue uma cadeia de segurança determinística para assegurar zero perda de dados:

```mermaid
graph TD
    A[Nova Versão Disponibilizada] --> B[Download do Pacote de Patch]
    B --> C[Fechamento do Software Principal]
    C --> D[Backup de Seguranca Automatico do SQLite]
    D --> E[Substituicao dos Binarios / DLLs]
    E --> F[Inicializacao do Migration Engine]
    F --> G{PRAGMA user_version == Schema Atual?}
    G -- Sim --> H[Abertura Normal do Sistema]
    G -- Nao --> I[Execucao dos Scripts de Migracao em Transacao]
    I -- Sucesso --> J[Atualizacao de user_version e Commit]
    I -- Falha --> K[Rollback Imediato + Restauracao do Backup]
```

---

### 2. Versionamento de Banco de Dados (`PRAGMA user_version`)

O SQLite disponibiliza nativamente o registrador `PRAGMA user_version` (inteiro de 32 bits), utilizado pelo PRIMOX para controle de migrações:

| Versão do Schema (`user_version`) | Versão do PRIMOX | Alterações de Estrutura |
| :---: | :---: | :--- |
| **0** | Legado | Criação inicial das tabelas por script dinâmico |
| **1** | B2 / B3 / B4 | Tabelas estruturadas com 17 campos elétricos, Pós-venda e D01-D06 |
| **2** | B5 / B6 (Piloto) | Tabelas de Checklist Técnico (`ChecklistTecnicoOS`, `ChecklistTecnicoItem`) |
| **3** | B7 (Produção) | Migração física definitiva de valores monetários para `INTEGER CentsV1` |
| **4** | B8 | Tabelas de sincronização de filas offline/online |

---

### 3. Protocolo de Rollback Automático em Caso de Erro de Atualização

Se durante a inicialização pós-atualização ocorrer qualquer erro de inicialização do SQLite ou falha de integridade:
1. O processo de migração interrompe a transação (`ROLLBACK`).
2. O backup criado no início da rotina (`primoauto_pre_update_<versao>.db`) é restaurado no local original.
3. O executável anterior é mantido como fallback (`PrimoAutoEletrica.exe.bak`).
4. Um log de diagnóstico detalhado é gerado em `%LOCALAPPDATA%\\PrimoAutoEletrica\\Logs\\update_error.log`.
5. O usuário recebe aviso claro orientando o contato com o suporte técnico.
"""

with open(os.path.join(DOC_DIR, "B5_0_UPDATE_STRATEGY.md"), "w", encoding="utf-8") as f:
    f.write(update_strat_md)

print("12. B5_0_UPDATE_STRATEGY.md OK")
