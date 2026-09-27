# C4.11 ADVERSARIAL + PROMPT INJECTION

**Date:** 2026-09-27 20:22:03 -03 | **DB:** `c7420d1811d4cfea16ce833326c6a331f360bebf025ea7f3a7ee785192a7ce0b`

## 14 attack classes
PromptInjectionDirect, PromptInjectionIndirectStored, JailbreakRoleOverride, SystemPromptExfiltration, CrossClientDataProbe, FinanceBypass, AutoActionCoercion, FakeEvidenceInjection, DiagnosisConfirmationForce, KnowledgeAutoPublish, IsolationBreakViaQuery, InstructionInOsNotes, InstructionInKnowledgeArticle, UnicodeHomoglyphObfuscation.

## Rule
Stored content cannot override system rules (`STORED_CONTENT_CANNOT_OVERRIDE_SYSTEM`).

## Tests
C411AdversarialGuardTests **3/3 PASS** (14/14 blocked)

## Decision
**C4.11 = PASS**
