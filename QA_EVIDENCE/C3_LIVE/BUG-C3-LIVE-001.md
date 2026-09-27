# BUG FOUND — C3.21 UiSmoke Full App

**ID:** BUG-C3-LIVE-001  
**Status:** FOUND → FIXED → RETEST (targeted PASS) → REGRESS (Full App 219/219 PASS)  
**When found:** 2026-09-27 08:34:12 -03  
**Check:** `Interacao:Janela:ExternalAiSettingsWindow`  
**Symptom:** `DialogResult somente pode ser definido após Window ser criado e exibido como caixa de diálogo.`  
**Cause:** `ExternalAiSettingsWindow` set `DialogResult` on Cancel/Save while UiSmoke opens the window via non-modal `Show()`.  
**Fix:** Use `WindowInteractionHelper.CloseWithDialogResult` + automation-safe `ShowMessage`.  
**Retest targeted:** 2026-09-27 08:36:26 -03 — **1/1 PASS**  
**Full App regress:** 2026-09-27 08:50:34 -03 — **219/219 PASS** (`c3_live_fullapp_retest_2026-09-27`); ExternalAiSettingsWindow PASS (DialogResult suppressed in automation log 08:45:17).
