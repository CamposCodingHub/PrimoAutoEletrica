# PRIMOX WORKSHOP — B5.0
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
   - O arquivo original em disco pode ser mantido em diretório protegido `%LOCALAPPDATA%\PrimoAutoEletrica\Cert\` com permissão restrita de leitura apenas para o usuário do Windows executando a aplicação.
3. **Tratamento do Certificado A3:**
   - Comunicação via Cryptographic Service Provider (CSP) nativo do Windows ou PKCS#11.
   - Detecção do certificado no repositório de certificados do Windows (`Cert:\CurrentUser\My`).
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
