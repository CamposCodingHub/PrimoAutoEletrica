# PRIMOX Workshop — Product Gaps (reconciliado)

**Atualizado:** 2026-09-13 · pós NET10-26  
**Crônica:** [`Docs/PRIMOX-ADVANCES-CHRONICLE.md`](../PRIMOX-ADVANCES-CHRONICLE.md)

---

## Gaps abertos (atuais)

| Gap | Severidade | Notas |
|-----|------------|-------|
| Emissão fiscal live (homolog/prod) | Alta (comercial) | Software pronto; credencial externa |
| DANFE layout oficial | Média | PDF informativo existe |
| NFC-e/NFS-e completos | Alta (roadmap) | Scaffold + Fake |
| WhatsApp Business API | Média | wa.me cobre manual |
| 2FA no login | Média | Setup existe |
| Multi-filial comercial | Alta (escala) | Fiscal DB ≠ produto multi-oficina |
| Code signing | Externa | Pipeline ready |
| Auto-update | Média | Manual/scripts |
| Sync/SaaS | Baixa agora | Desktop-first |

---

## Gaps fechados / reduzidos (avanços)

| Gap antigo | Resolução |
|------------|-----------|
| Sem engine fiscal / NFeEmissao vazio | Foundation NET10-11…26 |
| Cancel/XML/DANFE ausentes | Cancel+XML Focus; DANFE informativo |
| Multiempresa fiscal ausente | FiscalEmpresas + EmpresaId |
| net6-only na linha de migração | net10.0-windows |
| Calendar Dark “irreparável” | CalendarContrastHealer |

---

## Registro histórico

O Product Gaps 1.0 (08/09) listava emissão como gap absoluto de implementação.  
Isso foi o diagnóstico correto **antes** da fundação; hoje o gap é **externo/live**, não ausência de código.
