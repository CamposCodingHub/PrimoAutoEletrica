# C6.3 Official PRIMOX Benchmark Dataset

**Branch:** cycle-c6/primox-intelligence | **main:** UNTOUCHED | **Protected DB:** unchanged  
**Keys:** ABSENT → model LIVE paths LIVE_NOT_TESTED

## Deliverable
`Services/Intelligence/PrimoxBenchmarkCatalog.cs` — **112** structured cases (4 × **28** domains), path toward 500 later.

## Domains (28)
ElectricalFundamentals … InsufficientEvidence … AdversarialPrompt … WorkOrderContext (see catalog `Domains`).

## Case fields
Question, Domain, Difficulty, ExpectedKnowledge, ExpectedReasoning, RequiredEvidence, ForbiddenAssumptions, ExpectedAnswerCharacteristics, SafetyConstraints, GroundTruthStatus (+ GoldenAnswer / DeterministicEvaluable for C6.4).

## Honesty
Real reasoning expectations — not vanity answers. Adversarial variants refuse invention / unsafe actions.

## Status
**PASS** (unit catalog integrity). LIVE model scoring deferred.