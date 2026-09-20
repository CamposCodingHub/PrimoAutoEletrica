# Phases 2–6 — NÃO FEITO (roadmap honesto)

**Data:** 2026-09-19 (BRT)  
**Branch:** `audit/product-discovery-2026-09`  
**Status:** documentados como **NÃO FEITO**. Nenhum item abaixo foi implementado nesta sessão.

## Fase 2 — Portal / cliente web
- Portal do cliente, login web, acompanhamento de OS remoto: **NÃO FEITO**

## Fase 3 — Mobile
- App mobile (Android/iOS) ou PWA oficina: **NÃO FEITO**

## Fase 4 — Integrações cloud
- WhatsApp Cloud API (envio real): **NÃO FEITO** (só UI local / lembretes locais na Fase 1)
- PIX / gateway pagamento: **NÃO FEITO**
- SaaS multi-tenant / sync cloud: **NÃO FEITO**

## Fase 5 — Linha pesada avançada
- Frota / J1939 / telemetria: **NÃO FEITO** (catálogo pesado já existe em dados; protocolo não)

## Fase 6 — Intelligence + license server
- Motor de intelligence / ML: **NÃO FEITO**
- License server real (hoje: scaffold local JSON+SHA256 da Fase 0): **NÃO FEITO**
- JWT na API: **NÃO FEITO** (gate tests documentam ausência — deliberado)

## O que FALTA para “certeza comercial”
1. ExhaustiveUi smoke em **todos** os módulos + mapa de modais com contagem real PASS/FAIL
2. Suite unitária completa (não só filtro Fase 0/1)
3. Money as INTEGER cents (se ainda double em paths críticos)
4. Itens das Fases 2–6 acima, se forem escopo de produto

Não inventar PASS/DONE sem evidência Windows.

## Atualizacao 2026-09-20
- JWT na API: **FEITO** em `audit/product-discovery-2026-09` (`ApiJwtHttpTests`). Ainda **NÃO** em `main`.
- License server / PIX / portal / mobile / WhatsApp Cloud / frota J1939 / ML: continuam **NÃO FEITO**.
