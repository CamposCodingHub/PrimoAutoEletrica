# C5.4 — Provider Lifecycle

**Date:** 2026-09-28 -03  
**Branch:** `cycle-c5/primox-intelligence`  
**Protected DB SHA256:** `C7420D1811D4CFEA16CE833326C6A331F360BEBF025EA7F3A7EE785192A7CE0B` **unchanged**

## Modes
LOCAL | EXTERNAL | DISABLED | FALLBACK | ERROR

## Priority
1. Kill-switch → DISABLED  
2. Enabled OFF → LOCAL  
3. Enabled ON + no key → LOCAL (never silent EXTERNAL)  
4. Armed + success → EXTERNAL  
5. Armed + fail/ungrounded/timeout/4xx/5xx/malformed/network → FALLBACK (recorded)

## Matrix results (unit / MOCK)
| Cell | Status |
|------|--------|
| OFF | PASS → LOCAL + audit |
| ON-no-key | PASS → LOCAL, 0 HTTP |
| kill-switch | PASS → DISABLED arming |
| valid (MOCK) | PASS → EXTERNAL + audit |
| invalid/4xx | PASS → FALLBACK + audit |
| 5xx | PASS → FALLBACK + audit |
| malformed | PASS → FALLBACK + audit |
| network | PASS → FALLBACK + audit |
| timeout | PASS (outcome mapping FALLBACK); live timeout = LIVE_NOT_TESTED |

## Tests
C54: **14/14 PASS**

## Status
**C5.4 = PASS** (LIVE cells remain LIVE_NOT_TESTED — keys ABSENT)