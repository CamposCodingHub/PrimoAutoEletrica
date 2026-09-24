# PRIMOX WORKSHOP — B5.0
## DECISÃO TÉCNICA DO PACOTE DE INSTALAÇÃO: MSIX vs INNO SETUP

**Escopo:** Distribuição e Instalação Comercial no Windows  
**Decisão:** **INNO SETUP ADOTADO COMO INSTALADOR OFICIAL DA v1.0**

---

### 1. Matriz Técnica Comparativa

| Critério de Avaliação | Microsoft MSIX | Jordan Russell Inno Setup | Vencedor / Decisão |
| :--- | :--- | :--- | :---: |
| **Compatibilidade Windows** | Windows 10 (1809+) e Windows 11. Falha em Windows 7/8/Server legados. | Windows 7, 8, 8.1, 10, 11 e Windows Server (x86 e x64). | **Inno Setup** |
| **Preservação de Dados (%LOCALAPPDATA%)** | Sistema de virtualização de sistema de arquivos (VFS) pode isolar ou apagar dados ao desinstalar. | Permite controle cirúrgico para NUNCA deletar o banco SQLite nem backups na desinstalação. | **Inno Setup** |
| **Exigência de Assinatura de Código** | Obrigatório certificado digital reconhecido pela Microsoft (SmartScreen bloqueia se não for assinado). | Suporta instalação limpa mesmo sem certificado comercial (exibe apenas aviso UAC padrão). | **Inno Setup** |
| **Criação de Atalho na Área de Trabalho** | Restrito e mediado pela shell do Windows AppX. | Controle total de criação de atalho na Área de Trabalho e Menu Iniciar para Todos os Usuários. | **Inno Setup** |
| **Pré-requisitos (.NET 10 Desktop Runtime)** | Requer packaging complexo ou bundle de 120MB+ embutido. | Script verifica se o .NET 10 Runtime está instalado; se não, baixa e instala silenciosamente. | **Inno Setup** |
| **Facilidade de Customização de Script** | Baseado em XML e manifestos restritos. | Script Pascal Script altamente customizável com lógica procedural de pré-instalação e pós-instalação. | **Inno Setup** |

---

### 2. Racional da Decisão

Nas oficinas mecânicas do Brasil:
1. O ambiente operacional é heterogêneo: computadores de balcão podem ter Windows 10 Home, Windows 11 Pro ou versões corporativas sem loja Microsoft Store habilitada.
2. O **MSIX impõe isolamento de contêiner**, dificultando o acesso direto do suporte técnico ao arquivo `primoauto_operacional.db` e às pastas de log em caso de manutenção emergencial.
3. O **Inno Setup** é um padrão industrial consagrado no setor automotivo, permitindo:
   - Instalação no padrão `C:\Program Files\PRIMOX Workshop\` ou `%LOCALAPPDATA%\PrimoAutoEletrica\App\`.
   - Garantia formal de que atualizações de versão **NUNCA sobrescreverão o banco de dados de produção da oficina**.
   - Criação de backup automático do banco antes de atualizar os executáveis.

---

### 3. Estrutura do Script de Instalação Inno Setup (`primox_setup.iss`)

- **Nome do Produto:** PRIMOX Workshop
- **Versão:** 1.0.0
- **Publisher:** CamposCodingHub / PRIMOX Auto Elétrica
- **Diretório Alvo da Aplicação:** `{localappdata}\PrimoAutoEletrica\App`
- **Diretório de Dados:** `{localappdata}\PrimoAutoEletrica\`
- **Regra de Desinstalação (`UninstallDelete`):**
  - Apaga executáveis e DLLs.
  - **PRESERVA OBRIGATORIAMENTE:** `*.db`, `*.db-shm`, `*.db-wal`, subpasta `Backups\` e subpasta `Dvi\`.
