# PRIMOX — NF-e Homologation Test Matrix

| ID | Scenario | Result |
|----|----------|--------|
| NH-01 | Validator blocks missing NCM (no invent) | PASS (unit) |
| NH-02 | Validator blocks fictitious NCM 00000000 | PASS (unit) |
| NH-03 | Production Guard | PASS (unit) |
| NH-04 | Focus Production does not call HTTP | PASS (unit) |
| NH-05 | Missing token → NotConfigured | PASS (unit) |
| NH-06 | Timeout → Consult → Authorized (no blind re-emit) | PASS (unit) |
| NH-07 | Rejected keeps Rejected | PASS (unit) |
| NH-08 | Network / 503 / InvalidResponse | PASS (unit) |
| NH-09 | Duplicate IdempotencyKey | PASS (unit) |
| NH-10 | Restart recovers from DB | PASS (unit) |
| NH-11 | Mapper preserves totals, does not invent NCM | PASS (unit) |
| NH-12 | HTTP mapper 401/429/503/empty JSON | PASS (unit) |
| NH-13 | Prod URL forced off in Normalize | PASS (unit) |
| NH-14 | Foundation + Migration schema | PASS (unit) |
| NH-15 | Build 0 errors | PASS |
| NH-16 | QaEngine (+ CompleteUi) | PASS 43/43 |
| NH-17 | DeepQa + Long Run 5 | PASS 6/6 |
| NH-18 | Exhaustive Light/Dark × 4 res | PASS 1915/0/0 |
| NH-19 | Focus live Homologation | **NOT EXECUTED** (no credential configured in agent session) |
| NH-20 | Cancel real | **NOT EXECUTED** |
| NH-21 | NFC-e / NFS-e / Production emit | **NOT APPLICABLE** |
