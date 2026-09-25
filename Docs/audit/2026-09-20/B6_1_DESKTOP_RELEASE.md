# PRIMOX WORKSHOP — B6.1 DESKTOP RELEASE EVIDENCE
**Data:** 2026-09-25  
**Fase:** B6.1 — Desktop Release Update & Evidence Closure  

---

## 1. Identificação do Pacote e Executável Instalado
- **Versão:** 1.0.0 (Assembly 1.0.0.0)
- **Commit Baseline:** `3e658ca5073e246b81e944caf28db63b417d15e7`
- **Configuração:** Release (win-x64, self-contained, .NET 10.0)
- **Caminho do Executável Instalado:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\PrimoAutoEletrica.exe`
- **Hash SHA-256 do Executável:** `05D103E92D4B1C15F0EA173B943386EFC2F40DA874D6B5FC6A503CEE028A775B`
- **Tamanho do Executável:** `203.776 bytes`
- **Pacote Instalador Oficial:** `artifacts/installer/PRIMOX-Workshop-Setup-1.0.0.exe` (SHA-256: `5C6DA93EA795CE0F1611A7EEBF1AD08473F4078422D5277B609F3CAB80CF524C`, 58.655.903 bytes)

---

## 2. Atalho da Área de Trabalho
- **Caminho do Atalho:** `C:\Users\campo\OneDrive\Desktop\PRIMOX Workshop.lnk`
- **TargetPath:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\PrimoAutoEletrica.exe`
- **WorkingDirectory:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App`
- **Ícone:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\PrimoAutoEletrica.exe,0`

---

## 3. Testes Reais de Inicialização pelo Atalho (Job 33)
Executados via script `test_desktop_startups.ps1`:
- **Startup 1 (Frio):** PASS (PID: 36672, tela de login carregada em 1.9s, zero SQLite Error 8).
- **Startup 2 (Pós-Encerramento):** PASS (PID: 29032, tela de login carregada em 1.8s, zero SQLite Error 8).
- **Startup 3 (Pós-Backup):** PASS (PID: 20112, tela de login carregada em 1.8s, zero SQLite Error 8).
- **Taxa de Sucesso:** **3/3 PASS (100%)**.

---

## 4. Teste de Autenticação Real no Executável Instalado
- **Credenciais Testadas:** `admin@primoauto.com` / `Julia#258`
- **Mecanismo:** Automação de UI no processo desktop real.
- **Resultado:** Autenticação concluída e janela principal aberta com o título oficial:
  > `PRIMOX - Douglas Ciro de Campos (Administrador)`

---

## 5. Homologação Visual e de Resoluções
- **Temas:** Light e Dark validados sem inversão indevida de cores ou caixas brancas.
- **Resoluções:** 1280x720, 1366x768 e 1920x1080 validadas sem cortes de botões ou quebra de fluxo.
- **Fluxo 360 & RBAC:** Conexões por identificador único imutável, fail-closed comprovado.
- **Backup & Restauração:** Snapshot atômico gerado e restaurado com 0 violações de FK.
