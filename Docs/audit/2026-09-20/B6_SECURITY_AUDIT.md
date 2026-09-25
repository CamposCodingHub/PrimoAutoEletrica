# PRIMOX WORKSHOP — B6 SECURITY & OPERATIONAL LGPD AUDIT
**Data:** 2026-09-25  
**Fase:** B6 — Piloto Comercial Controlado  

---

## 1. Segurança de Senhas e Criptografia
- **Algoritmo:** PBKDF2 com derivação HMAC-SHA256, 600.000 iterações (recomendação OWASP ASVS 2023+).
- **Salt:** Salt criptográfico aleatório de 16 bytes gerado via `RandomNumberGenerator` por usuário.
- **Proteção em Logs:** Proibição estrita de gravação de senhas em texto plano em qualquer arquivo de log ou mensagem de erro.
- **Proteção contra Força Bruta:** Bloqueio progressivo de login gerenciado pela tabela `LoginTentativasSeguranca`.

---

## 2. Controles Operacionais de LGPD
- **Dados Coletados:** Nome, CPF/CNPJ, Telefone, Endereço e Placa/Chassi do Veículo.
- **Finalidade:** Emissão de orçamentos, ordens de serviço, garantias legais e registros contábeis.
- **Armazenamento:** Estritamente local na máquina da oficina (`%LOCALAPPDATA%\PrimoAutoEletrica\`).
- **Acesso:** Restrito por perfil RBAC (apenas recepção, gerência e caixa têm visualização dos dados cadastrais completos).
- **Exportação e Backups:** Backups protegidos no disco local; sem envio automático não consentido para servidores externos ou terceiros.
- **Declaração de Conformidade:** Os controles técnicos operacionais implementados atendem aos requisitos de guarda segura e minimização de dados para o estágio do produto.
