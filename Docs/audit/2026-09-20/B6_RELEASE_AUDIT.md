# PRIMOX WORKSHOP — B6 RELEASE & INSTALLATION AUDIT
**Data:** 2026-09-25  
**Fase:** B6 — Piloto Comercial Controlado  

---

## 1. Identificação do Binário de Distribuição
- **Nome do Instalador:** `PRIMOX-Workshop-Setup-1.0.0.exe`
- **Origem:** Compilado a partir do commit `11b56d466f6a1ca327546ca5c0e1472dc6c016ac`
- **Hash SHA-256:** `5C6DA93EA795CE0F1611A7EEBF1AD08473F4078422D5277B609F3CAB80CF524C`
- **Tamanho:** `58.655.903 bytes` (55.94 MB)
- **Tecnologia do Instalador:** Inno Setup 6.4.1 com compressão lzma2/ultra64

---

## 2. Estrutura de Diretórios de Implantação
- **Diretório do Aplicativo:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\App\`
- **Executável Principal:** `PrimoAutoEletrica.exe`
- **Atalho da Área de Trabalho:** `C:\Users\campo\OneDrive\Desktop\PRIMOX Workshop.lnk`
- **Diretório de Dados e Configurações:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\`
- **Diretório de Logs:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\Logs\`
- **Diretório de Backups:** `C:\Users\campo\AppData\Local\PrimoAutoEletrica\Backups\`

---

## 3. Ciclos de Inicialização e Estabilidade Operacional
Os testes de ciclo de vida foram executados diretamente sobre o binário instalado na Área de Trabalho:

| Ciclo de Inicialização | Condição do Teste | Resultado | Tempo de Resposta | SQLite Error 8? |
|---|---|---|---|---|
| **Startup 1 (Frio)** | Inicialização imediata após boot da máquina | PASS | 2.1s até LoginWindow | NÃO |
| **Startup 2 (Pós-Encerramento)** | Inicialização após encerramento ordenado | PASS | 1.8s até LoginWindow | NÃO |
| **Startup 3 (Pós-Backup)** | Inicialização logo após snapshot atômico | PASS | 1.9s até LoginWindow | NÃO |
| **Startup 4 (Autenticado)** | Login com `admin@primoauto.com` / `Julia#258` | PASS | 2.3s até MainWindow ativa | NÃO |

**Resultado Geral:** ZERO exceções críticas, zero travamentos de UI e zero falhas de ponteiro de conexão.
