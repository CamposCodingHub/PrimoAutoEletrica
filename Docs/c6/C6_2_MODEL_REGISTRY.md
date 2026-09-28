# C6.2 Model Registry

**Branch:** `cycle-c6/primox-intelligence`  
**origin/main:** UNTOUCHED  
**Protected DB:** unchanged (rehash each phase)  
**Keys:** ABSENT → LIVE pricing = LIVE_NOT_TESTED / PRICE_NOT_VERIFIED

## Deliverable
`Services/Intelligence/ModelRegistry.cs` — `ModelDefinition` with PriceSource / PriceCheckedAt / PriceStatus.

## Honesty
- Default entries (rules, local-model-generic, openai-compatible-external) are **PRICE_NOT_VERIFIED**
- Never invent vendor prices
- Verified only when PriceSource ≠ PRICE_NOT_VERIFIED AND InputPricePer1M set AND PriceCheckedAt set

## Status
**PASS** (unit). External LIVE price check: **LIVE_NOT_TESTED**.