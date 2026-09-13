# PRIMOX — Fiscal Homologation Plan (atualizado)

**Atualizado:** 2026-09-13  
**Papel:** Plano vivo para **live** homolog — a fundação de software já existe (NET10-26)

---

## Já executado (software)

- [x] `IFiscalProvider` + Focus adapter  
- [x] Emit/consult HTTP homolog URL only  
- [x] Cancel DELETE + justificativa  
- [x] ObterXml via `caminho_xml_*`  
- [x] Fake cycles + mega/stress  
- [x] Production guard  
- [x] Multiempresa fiscal DB  
- [x] DANFE informativo  

## Pendente (externo / evidência live)

- [ ] Token Focus homolog no ambiente  
- [ ] Emitente real de homologação (CNPJ/IE de teste do cliente — **não inventar**)  
- [ ] Emissão homolog observada (ref, status, chave)  
- [ ] Cancelamento homolog observado  
- [ ] XML autorizado baixado e armazenado  
- [ ] (Opcional) PDF Focus oficial se caminho existir  

## Fora deste plano

- Produção SEFAZ  
- WhatsApp API real  
- Inventar XSD/layout DANFE oficial  

---

## Ordem recomendada

1. Forensic audit NET10-26  
2. Configurar token DPAPI (sem commit)  
3. Uma emissão homolog controlada  
4. Consulta + XML + cancel  
5. Relatório de evidências (sem secrets)

---

## Registro histórico

O plano original (audit-only, “não executar emissão”) era da fase de decisão. A fundação foi implementada depois; o que resta é **prova live**.
