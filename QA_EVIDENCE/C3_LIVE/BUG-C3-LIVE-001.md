# BUG FOUND — C3.21 UiSmoke Full App

**ID:** BUG-C3-LIVE-001  
**Status:** FOUND → FIXED → RETEST (targeted PASS) → REGRESS (full App pending)  
**When found:** 2026-09-27 08:34:12 -03  
**Check:** `Interacao:Janela:ExternalAiSettingsWindow`  
**Symptom:** `DialogResult somente pode ser definido após Window ser criado e exibido como caixa de diálogo.`  
**Cause:** `ExternalAiSettingsWindow` set `DialogResult` on Cancel/Save while UiSmoke opens the window via non-modal `Show()`.  
**Fix:** Use `WindowInteractionHelper.CloseWithDialogResult` + automation-safe `ShowMessage`.  
**Retest targeted:** 2026-09-27 08:36:26 -03 — `Interacao:Janela:ExternalAiSettingsWindow` **1/1 PASS** (`TestResults/UiSmoke/c3_live_retest_extai2_2026-09-27`)  
**Full App retest:** in progress `c3_live_fullapp_retest_2026-09-27`
