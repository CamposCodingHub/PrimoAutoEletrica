# C4.2 EVIDENCE RANKING

**Machine:** CIRO | **Date:** 2026-09-27 20:17:34 -03 | **Branch:** cycle-c4/primox-intelligence
**Protected DB:** `c7420d1811d4cfea16ce833326c6a331f360bebf025ea7f3a7ee785192a7ce0b` unchanged

## Pipeline
AUTHORIZATION → FILTER → RETRIEVAL → RANKING

## Score model (explainable, no magic)
Version `C4.2-EXPLAINABLE-V1` components:
- token_overlap (1.0 per query token in title/excerpt)
- title_hit (2.0 per token in title)
- source_type_boost (Knowledge 1.5 / Diagnostic 1.4 / Procedure 1.3 / WorkOrder 1.2 / Other 1.0)
- anchor_cliente (3.0) / anchor_veiculo (2.5) / anchor_os (2.0) when scope matches
- prior_retrieval (additive only if already present)

Output fields: SourceType / SourceId / Title / Excerpt / RelevanceScore / RankingReason (+ ScoreComponents).

## Guarantees
- Deterministic tie-break: score desc, EvidenceId, SourceId
- Cross-client evidence never retrieved
- Finance filtered unless IncludeFinancial AND CanIncludeFinance
- Reuses EvidenceItem + SourceTaggedContextPackage + ExternalEvidencePackage (no duplicate stack)

## Tests
C42EvidenceRankingTests **8/8 PASS**

## Decision
**C4.2 = PASS** (local). LIVE_NOT_TESTED. AUDIT NOT_IMPLEMENTED.
