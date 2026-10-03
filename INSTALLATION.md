# Guia Completo de Instalação — PRIMOX Workshop Enterprise v2.1

**Produto:** PRIMOX Workshop (Primo Auto Elétrica)  
**Versão:** 2.1.0 Enterprise  
**Plataformas Suportadas:** Windows 10/11 (64-bit) e Linux (via Wine / Proton)  
**Publisher Oficial:** CamposCodingHub  

---

## 1. Requisitos de Sistema

### A. Ambiente Windows
* **Sistema Operacional:** Windows 10 (versão 1903 ou superior) ou Windows 11 (64-bit)
* **Processador:** Intel Core i3 / AMD Ryzen 3 ou superior
* **Memória RAM:** 4 GB mínimo (8 GB recomendado para grandes inventários)
* **Espaço em Disco:** 500 MB livres para a aplicação + espaço para backups
* **Resolução:** 1366 x 768 mínimo (Full HD 1920 x 1080 recomendado)
* **Permissões:** Acesso de Administrador para o instalador oficial

### B. Ambiente Linux
* **Distribuições Testadas:** Ubuntu 20.04+, Debian 11+, Fedora 38+, Arch Linux, Linux Mint
* **Wine:** Wine 8.0 ou superior (64-bit) com suporte a Wine-Mono
* **Winetricks / Fontes:** Fontes TrueType Microsoft (`corefonts`) para renderização nítida
* **Arquitetura:** x86_64

---

## 2. Como Instalar no Windows

Existem duas formas oficiais de instalação no Windows:

### Método 1: Instalador Oficial (.exe — Recomendado)
1. Baixe o instalador oficial `PRIMOX-Workshop-Setup-2.1.0.exe` da pasta `artifacts/installer` ou da página de Releases do GitHub.
2. *(Opcional)* Valide o hash SHA-256 no PowerShell:
   ```powershell
   Get-FileHash .\PRIMOX-Workshop-Setup-2.1.0.exe -Algorithm SHA256
   ```
3. Dê dois cliques no instalador e siga o assistente:
   * Escolha o idioma (Português do Brasil);
   * Aceite os Termos de Direitos Autorais e Não-Comercialização;
   * O caminho padrão de instalação é `C:\Program Files\PRIMOX\Workshop`;
   * Marque a opção de criar atalho na Área de Trabalho e no Menu Iniciar.
4. Conclua a instalação. O sistema iniciará automaticamente.

### Método 2: Versão Portable (.zip)
1. Baixe o pacote `PRIMOX-Workshop-Portable-win-x64-2.1.0.zip`.
2. Extraia o conteúdo em uma pasta de sua escolha (ex.: `C:\PRIMOX`).
3. Execute `PrimoAutoEletrica.exe`.

---

## 3. Como Instalar no Linux

O PRIMOX Workshop conta com instalador automatizado para Linux que configura o Wine, cria atalhos no menu de aplicativos e configura o comando global `primox` no terminal.

### Passo 1: Instalar dependências (Wine)
Certifique-se de que o Wine está instalado em sua distribuição:

* **Ubuntu / Debian / Linux Mint:**
  ```bash
  sudo apt update
  sudo apt install -y wine wine64 winetricks
  ```
* **Fedora / RHEL:**
  ```bash
  sudo dnf install -y wine winetricks
  ```
* **Arch Linux / Manjaro:**
  ```bash
  sudo pacman -S wine wine-mono winetricks
  ```

*(Opcional recomendado)* Instale as fontes padrão para melhor nitidez visual:
```bash
winetricks corefonts
```

### Passo 2: Executar o Instalador Automático
No terminal, dentro da pasta do projeto ou do pacote extraído:

```bash
# Como usuário comum (instala em ~/.local/share/primox-workshop):
./Installer/linux/install.sh
```

Ou, caso deseje instalar para todos os usuários do sistema:
```bash
# Como root/sudo (instala em /opt/primox-workshop):
sudo ./Installer/linux/install.sh
```

### Passo 3: Executar a Aplicação no Linux
Após a instalação, você pode abrir o PRIMOX de 3 formas:
1. **Pelo Menu de Aplicativos:** Busque por **"PRIMOX Workshop"** no menu do seu ambiente gráfico (GNOME, KDE Plasma, XFCE, Cinnamon);
2. **Pelo Terminal:** Digite simplesmente:
   ```bash
   primox
   ```
3. **Pelo Navegador de Arquivos:** Acesse o atalho criado na Área de Trabalho.

---

## 4. Primeira Execução e Configuração Inicial

Na primeira abertura do sistema:
1. **Criação Automática do Banco de Dados:** O motor SQLite de alta performance criará automaticamente o banco de dados em:
   * **Windows:** `%LOCALAPPDATA%\PrimoAutoEletrica\primoauto.db`
   * **Linux:** `~/.wine/drive_c/users/$USER/AppData/Local/PrimoAutoEletrica/primoauto.db`
2. **Login Inicial:** Faça login com as credenciais padrão de primeiro acesso (ou crie o usuário master administrador no assistente de primeiro uso).
3. **Dados da Oficina:** Acesse `Configurações → Empresa` e preencha:
   * Razão Social / Nome Fantasia;
   * CNPJ e Inscrição Estadual/Municipal;
   * Endereço completo e Telefones / WhatsApp;
   * Logotipo da oficina (para impressões de orçamentos e OS).
4. **Cadastro de Usuários:** Crie um usuário individual para cada colaborador da equipe (gerente, recepção, eletricistas, caixa, comprador). **Nunca compartilhe a senha de administrador.**
5. **Configuração de Backup:** Em `Configurações → Backup`, ative o backup automático diário.

---

## 5. Como Atualizar uma Instalação Existente

### Atualização no Windows
1. Feche o PRIMOX caso esteja aberto.
2. Execute o novo instalador `PRIMOX-Workshop-Setup-X.X.X.exe`.
3. Os binários serão atualizados mantendo 100% dos seus dados, clientes, OS e históricos intactos (armazenados em AppData).

### Atualização no Linux
1. No terminal, execute novamente o script de instalação com os novos binários:
   ```bash
   ./Installer/linux/install.sh
   ```
2. O script atualizará a pasta de binários e os lançadores sem alterar a base de dados do Wine.

---

## 6. Solução de Problemas (Troubleshooting)

### A. O programa não abre no Linux
* Execute pelo terminal para visualizar os logs de erro do Wine:
  ```bash
  primox
  ```
* Se o Wine acusar falta do Mono, instale com:
  ```bash
  winetricks dotnet6
  # ou instale o pacote wine-mono da sua distribuição
  ```

### B. Fontes borradas ou caracteres estranhos
* Instale as fontes do Windows via winetricks:
  ```bash
  winetricks corefonts gdiplus
  ```

### C. Como restaurar um backup de segurança
1. Abra o PRIMOX e acesse `Configurações → Backup / Restauração`.
2. Selecione o arquivo `.db` mais recente da pasta de backups.
3. Confirme a restauração e reinicie o programa.
