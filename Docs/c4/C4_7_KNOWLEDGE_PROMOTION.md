# C4.7 KNOWLEDGE_PROMOTION

**Date:** 2026-09-27 20:20:19 -03 | **Branch:** cycle-c4/primox-intelligence | **Protected DB:** `c7420d1811d4cfea16ce833326c6a331f360bebf025ea7f3a7ee785192a7ce0b` unchanged

## Scope
Pipeline Candidate→Review→Draft→Approved→Published (+Rejected).
Never auto-publish from OS (`AutoPublishedFromOs => false`).
RBAC: AUTHOR / REVIEWER / PUBLISHER.

## Reuse
Existing KnowledgePromotionService (Pending/Approved/Rejected, draft-on-approve) remains; C4.7 adds explicit stage pipeline.

## Tests
C47_Promotion_CandidateToPublished_RequiresRbac_NeverAutoFromOs **PASS**

## Decision
**C4.7 = PASS** (local unit). LIVE_NOT_TESTED. AUDIT NOT_IMPLEMENTED. Suggest-only.
