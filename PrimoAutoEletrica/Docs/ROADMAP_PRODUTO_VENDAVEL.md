# Roadmap para Produto Vendável

**Atualizado:** 2026-09-13 · alinhado a [`Docs/CURRENT-TRUTH.md`](../../Docs/CURRENT-TRUTH.md) e NET10-26  
**Princípio:** Desktop Workshop primeiro → fiscal completo testável → mobile → cloud. **Não** abandonar a base desktop.

Este documento planeja recursos futuros. Estado técnico fiscal detalhado:  
`Docs/qa/PRIMOX-NET10-26-FISCAL-COMPLETE-IMPLEMENTATION.md`

---

## 0. O que já existe (não tratar como “futuro”)

| Capacidade | Estado |
|------------|--------|
| Core oficina (clientes, veículos, OS, orçamento, agenda, estoque, PDV, financeiro, relatórios) | REAL |
| Importação NF-e | REAL |
| Fundação fiscal (operações, idempotência, Focus emit/consult/cancel/XML, Fake, multiempresa fiscal DB) | IMPLEMENTED + TESTED (NET10-26) |
| NFC-e / NFS-e | SCAFFOLD + Fake |
| DANFE | PDF informativo (≠ oficial SEFAZ) |
| WhatsApp `wa.me` share | REAL (manual) |
| WhatsApp Business API | Abstração; LIVE blocked |
| Calendar Dark | Mitigado (`CalendarContrastHealer`) |
| Emissão homolog/produção SEFAZ | **BLOCKED_EXTERNAL** (credencial/empresa) |

---

## 1. Multiempresa

### Já entregue (fiscal)
- Tabela `FiscalEmpresas`, `EmpresaId` em operações/documentos/eventos, isolamento testado A/B.

### Ainda futuro (produto)
- Isolamento completo de dados comerciais (clientes/OS/estoque) por empresa/filial.
- UI de seleção de empresa, logo/numeração comercial por estabelecimento.
- Uma instalação padrão continua atendendo **uma** oficina até haver GO explícito.

Riscos: migração sensível; PDFs/backup/permissões devem respeitar isolamento.

---

## 2. Fiscal — próximos degraus (ordem correta)

1. **Auditoria forense** do NET10-26 (anti-superdeclaração) — próximo passo recomendado.  
2. Homologação Focus **live** somente com token/credencial reais (sem inventar).  
3. Completar NFC-e (CSC/QR) e NFS-e municipal **com** contrato/fonte oficial.  
4. DANFE/PDF oficial via provider quando caminho comprovado.  
5. Certificado A1 real + assinatura XML quando necessário além do provedor.  
6. Tributação extensível (preparar modelo; **não** hardcoded só ICMS/IPI/PIS/COFINS; IBS/CBS só com fonte oficial).

**Não** pular para “Semanas 1–3 homolog live” sem fechar fundação + audit.

Ciclo comercial desejado:

```text
Cliente → Veículo → Orçamento → Aprovação → OS → Peças/Serviços
  → Faturamento → Documento fiscal → XML → DANFE → Cliente → WhatsApp
```

---

## 3. Multiestação em rede local

Objetivo futuro: mais de uma máquina na oficina.

- SQLite adequado para instalação simples.
- Concorrência real: avaliar PostgreSQL/SQL Server.
- Evitar SQLite em rede com muitos writers.

---

## 4. Licenciamento

Controle comercial sem prejudicar oficina offline:

- Chave offline assinada e/ou ativação online com tolerância.
- Nunca bloquear acesso emergencial aos dados sem contingência clara.

---

## 5. Planos comerciais (orientação)

**Básico:** Clientes, veículos, orçamentos, OS.  
**Profissional:** + Estoque, PDV, financeiro, relatórios, importação NF-e.  
**Premium:** + WhatsApp API (quando houver), checklist/fotos (DVI), assinatura digital de aprovação, diagnóstico guiado, multiestação estabilizada, emissão fiscal homologada.

`wa.me` já agrega valor no Profissional/Premium sem confundir com API Cloud.

---

## 6. Diferenciação pós-fiscal — DVI / inspeção

Após fiscal estável:

```text
Entrada → Checklist → Fotos → Defeitos → Diagnóstico
  → Orçamento → Aprovação digital → OS → Fotos execução → Entrega
```

Não iniciar DVI em paralelo ao fechamento fiscal.

---

## 7. Pré-requisitos antes de vender amplamente

- [x] Build sem erro (net10 na branch de migração)
- [x] UI smoke QaEngine / DeepQa verdes (gates recentes)
- [ ] Homologação fiscal live comprovada (externo)
- [ ] Installer assinado (cert comercial)
- [ ] Manual usuário/técnico atualizados pós-NET10
- [ ] Backup/restore em ambiente limpo revalidado na TFM atual
- [ ] Checklist de qualidade por versão

---

## 8. Explicitamente fora do escopo imediato

- SaaS multi-tenant / billing cloud
- “Produção SEFAZ ready” sem evidência
- Inventar endpoints PlugNotas / regras IBS/CBS
