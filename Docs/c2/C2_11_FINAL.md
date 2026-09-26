# C2.11 FINAL — UI polish of intelligence

**Branch:** `cycle-c2/primox-intelligence`  
**Date (America/Sao_Paulo):** 2026-09-26

## Decision

**PASS**

Contextual empty/error messages come from `ContextualSearchService` / Assist fail-closed responses and are shown via existing `KnowledgeSearchViewModel.StatusMessage = response.Message`. Design System brushes unchanged. No giant chatbot. No redesign of 360 screens (C2.6 is overlay service-only).

## Limitations

Dedicated Light/Dark screenshot matrix and all-resolution inventory not fully automated in Night Run; Search/MainWindow/Tema/Dashboard UiSmoke 13/13 PASS used as regression proxy.

Protected SHA unchanged. No OpenAI.