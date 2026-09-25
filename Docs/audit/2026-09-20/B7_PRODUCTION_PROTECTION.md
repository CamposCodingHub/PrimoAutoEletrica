# PRIMOX WORKSHOP — GATE 01: PRODUCTION BASELINE PROTECTION AUDIT

Data: 2026-09-25  
Versão: 1.0.0  
Regra Zero: Imutabilidade Estrita da Base Original Protegida  

---

## 1. Verificação Criptográfica e Fisiológica

| Atributo | Esperado | Observado | Conformidade |
|:---|:---|:---|:---:|
| Arquivo | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db` | `C:\Users\campo\AppData\Local\PrimoAutoEletrica\primoauto.db` | EXACT MATCH |
| SHA-256 | `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` | `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` | EXACT MATCH |
| Tamanho | 20.201.472 bytes | 20.201.472 bytes | EXACT MATCH |
| IsReadOnly | TRUE | TRUE | EXACT MATCH |
| PRAGMA integrity_check | ok | ok | EXACT MATCH |
| PRAGMA foreign_key_check | 0 violações | 0 violações | EXACT MATCH |

---

## 2. Declaração de Proteção Absoluta

A base de dados acima é o **PROTECTED BASELINE**.
Sob as regras de engajamento do PRIMOX Workshop:
1. Ela NUNCA será usada como alvo direto de alteração ou migração de produção.
2. Ela NUNCA receberá `UPDATE`, `INSERT`, `DELETE`, `ALTER TABLE`, `DROP`, `VACUUM` ou alteração de `PRAGMA user_version`.
3. Todos os ensaios, shadow migrations e simulações são executados em instâncias operacionais ou transitórias isoladas.
4. O arquivo operacional ativo do ambiente de produção/piloto é `primoauto_operacional.db` (configurado em `database-settings.json`).

---

## 3. Conclusão do Gate 01

TESTE: Verificação de imutabilidade e integridade da base protegida de produção  
RESULTADO: Integridade 100% comprovada, hash idêntico ao baseline mestre, permissão somente-leitura ativa.  
EVIDÊNCIA: Execução de SHA256 e PRAGMA via Python SQLite3.  
STATUS: **PASS**
