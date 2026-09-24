# PRIMOX WORKSHOP — B5.0
## ESPECIFICAÇÃO DE ESCOPO DO PRIMOX WORKSHOP v1.0

**Versão Alvo:** 1.0.0 (Release Comercial Desktop)  
**Status do Escopo:** **APROVADO PARA PLANEJAMENTO**

---

### 1. Definição do Produto PRIMOX Workshop v1.0

O PRIMOX Workshop v1.0 é um **Sistema de Gestão e Diagnóstico Técnico Avançado para Oficinas Mecânicas e Autoelétricas**, com foco diferenciado em **Sistemas Elétricos e Eletrônicos para Veículos Leves (12V) e Linha Pesada / Máquinas (24V)**.

O software opera em arquitetura **Local Desktop First**, garantindo funcionamento contínuo mesmo sem conexão com a internet, máxima velocidade de resposta e total privacidade dos dados da oficina.

---

### 2. Módulos Inclusos na Versão 1.0

| Módulo | Estado de Implementação | Nível de Validação | Observação de Release |
| :--- | :---: | :---: | :--- |
| **Dashboard Gerencial** | Implemented | Homologated | Indicadores de OS, faturamento e agenda em tempo real |
| **Clientes & Veículos** | Implemented | Homologated | Gestão cadastral completa, validação CPF/CNPJ, LGPD |
| **Prontuário Elétrico** | Implemented | Homologated | 17 parâmetros de arquitetura 12V e 24V |
| **Client360 & Vehicle360** | Implemented | Homologated | Histórico técnico, financeiro e temporal sem junções textuais |
| **Orçamentos & Aprovação** | Implemented | Homologated | Cálculo com margens, geração de tokens e conversão 1:1 para OS |
| **Ordens de Serviço** | Implemented | Homologated | Gestão operacional com apontamento técnico |
| **Autoelétrica Técnica** | Implemented | Homologated | Roteiros D01 a D06 estruturados + medições e delta pós-reparo |
| **Estoque & Catálogo** | Implemented | Homologated | Kardex, importação de XML de NF-e, fotos e esquemas em PDF |
| **Financeiro & Caixa** | Implemented | Homologated | Contas a pagar, receber, fluxo de caixa e frente de caixa |
| **Pós-Venda Estruturado** | Implemented | Validated | Gestão de retornos, garantias e revisões preventivas |
| **DVI Inspeção Digital** | Implemented | Validated | Checklist visual de recepção com fotos locais |
| **Segurança & RBAC** | Implemented | Homologated | 10 perfis, 82 permissões fail-closed, hash PBKDF2 |
| **Backup & Integridade** | Implemented | Homologated | Backup a quente SQLite com checagem física e de FKs |
| **Configurações & Temas** | Implemented | Homologated | Modo Dark e Light nativos, dados da empresa e parâmetros |

---

### 3. Limitações Técnicas Declaradas da v1.0

1. **Mono-Estação Primária / Rede Local Direta:** O banco SQLite opera no computador principal da oficina. O suporte a rede local multi-terminal deve ser homologado via pasta compartilhada com WAL mode ou API local na v1.1.
2. **Emissão Fiscal Direta Bloqueada:** A v1.0 suporta entrada e conferência de XMLs de terceiros. A emissão de notas fiscais próprias (NF-e/NFC-e) depende da contratação do módulo fiscal homologado na Fase B7.
3. **Licenciamento Local Standalone:** A validação da cópia na v1.0 utiliza assinatura digital assimétrica local ou chave de ativação offline, sem dependência de servidor cloud.
4. **Armazenamento de Imagens Local:** Fotos de DVI e documentos técnicos são armazenados no diretório local `%LOCALAPPDATA%\PrimoAutoEletrica\Dvi\`.

---

### 4. Itens Explicitamente Fora da Versão 1.0 (Out of Scope)

- Portal Web do Cliente (Acompanhamento em nuvem)
- Aplicativo Mobile para Mecânicos (Android / iOS)
- Sincronização Automática Multi-Filiais em Nuvem
- TEF Dedicado para Maquininha de Cartão
- Conciliação Bancária Automática por arquivo OFX/CNAB
- Consulta Online de Catálogos de Distribuidores via API B2B
- Emissão de Nota Fiscal de Serviços Municipal (NFS-e)
