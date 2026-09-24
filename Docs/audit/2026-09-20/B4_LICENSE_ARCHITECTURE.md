# PRIMOX WORKSHOP — FASE B4
## ARQUITETURA DE LICENCIAMENTO COMERCIAL FUTURA

**Data:** 24/09/2026  
**Status Atual:** MOCK / SCAFFOLD LOCAL  
**Objetivo:** Desenho arquitetural definitivo para ativação comercial em SaaS (sem criar servidores fictícios agora).

---

### 1. Pilares da Arquitetura Comercial

1. **Criptografia Assimétrica (RSA-4096 / Ed25519):**
   - Chave privada reside exclusivamente no servidor central da Primox.
   - Chave pública embutida no binário do Desktop para verificação de licenças assinadas digitalmente.
2. **Fingerprint de Máquina (Hardware ID):**
   - Combinação de Motherboard UUID + CPU ID + MAC Address hash com salt.
3. **Validação Offline com Grace Period:**
   - Licença válida por 30 dias offline.
   - Revalidação automática em segundo plano a cada 7 dias quando houver conexão com a internet.
4. **Revogação Remota (CRL / OCSP-like):**
   - Lista de revogação de licenças consultada periodicamente para bloqueio de inadimplentes.
